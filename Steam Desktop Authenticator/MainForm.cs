using System;
using System.Diagnostics;
using System.Windows.Forms;
using SteamAuth;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using System.Drawing;
using System.Linq;

using ZXing.QrCode;
using System.Runtime.InteropServices;
using ZXing.Common;
using ZXing;
using ZXing.Windows.Compatibility;
using System.Threading.Tasks;
using System.Net.Http;

namespace Steam_Desktop_Authenticator
{
    public partial class MainForm : Form
    {
        private SteamGuardAccount currentAccount = null;
        private SteamGuardAccount[] allAccounts;
        private List<string> updatedSessions = new List<string>();
        private Manifest manifest;
        private static SemaphoreSlim confirmationsSemaphore = new SemaphoreSlim(1, 1);

        private long steamTime = 0;
        private long currentSteamChunk = 0;
        private string passKey = null;
        private bool startSilent = false;

        const int VK_RCONTROL = 0xA3;
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(int vKey);

        // Forms
        private TradePopupForm popupFrm = new TradePopupForm();

        public MainForm()
        {
            InitializeComponent();
        }

        public void SetEncryptionKey(string key)
        {
            passKey = key;
        }

        public void StartSilent(bool silent)
        {
            startSilent = silent;
        }

        // Form event handlers

        private void MainForm_Shown(object sender, EventArgs e)
        {
            this.labelVersion.Text = String.Format("v{0}", Application.ProductVersion);
            try
            {
                this.manifest = Manifest.GetManifest();
            }
            catch (ManifestParseException)
            {
                MessageBox.Show(Strings.Get("MainForm_UnableToReadSettings"), Strings.Get("App_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            // Make sure we don't show that welcome dialog again
            this.manifest.FirstRun = false;
            this.manifest.Save();

            // Tick first time manually to sync time
            timerSteamGuard_Tick(new object(), EventArgs.Empty);

            if (manifest.Encrypted)
            {
                if (passKey == null)
                {
                    passKey = manifest.PromptForPassKey();
                    if (passKey == null)
                    {
                        Application.Exit();
                    }
                }

                btnManageEncryption.Text = Strings.Get("MainForm_ManageEncryption");
            }
            else
            {
                btnManageEncryption.Text = Strings.Get("MainForm_SetupEncryption");
            }

            btnManageEncryption.Enabled = manifest.Entries.Count > 0;

            loadSettings();
            loadAccountsList();

            if (startSilent)
            {
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            trayIcon.Icon = this.Icon;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }


        // UI Button handlers

        private void btnSteamLogin_Click(object sender, EventArgs e)
        {
            var loginForm = new LoginForm();
            loginForm.ShowDialog();
            this.loadAccountsList();
        }

        static async Task WaitForLeftAltKeyPress()
        {
            while (true)
            {
                if ((GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0)
                    break;

                await Task.Delay(100);
            }
        }

        private async void btnLoginViaQr_Click(object sender, EventArgs e)
        {
            if (currentAccount == null) return;

            this.btnLoginViaQr.Enabled = false;
            if (this.manifest.FirstQR)
            {
                MessageBox.Show(Strings.Get("MainForm_QrHowToUse"), Strings.Get("MainForm_QrHowToUseTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.manifest.FirstQR = false;
                this.manifest.Save();
            }

            await WaitForLeftAltKeyPress();
            this.btnLoginViaQr.Enabled = true;
            
            var reader = new QRCodeReader();
            GetCursorPos(out Point cursorPos);
            int scanWidth = 500;
            int scanHeight = 500;

            using (Bitmap bitmap = new Bitmap(scanWidth, scanHeight))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(cursorPos.X - scanWidth / 2, cursorPos.Y - scanHeight / 2, 0, 0, bitmap.Size);
                }

                var luminanceSource = new BitmapLuminanceSource(bitmap);
                var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminanceSource));
                var result = reader.decode(binaryBitmap);

                if (result == null)
                    return;
                
                if (!Regex.IsMatch(result.Text, @"^https?://s\.team/q/\d+/\d+"))
                {
                    MessageBox.Show(Strings.Get("MainForm_QrWrongCode"), Strings.Get("MainForm_QrWrongCodeTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (currentAccount.Session.IsRefreshTokenExpired())
                {
                    MessageBox.Show(Strings.Get("Common_SessionExpired"), Strings.Get("MainForm_LoginViaQr"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (currentAccount.Session.IsAccessTokenExpired())
                {
                    try
                    {
                        await currentAccount.Session.RefreshAccessToken();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Strings.Get("MainForm_LoginViaQrErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                bool success;
                try
                {
                    success = await currentAccount.SignInViaQR(result.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Strings.Get("MainForm_LoginViaQrErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!success)
                    MessageBox.Show(Strings.Get("MainForm_QrLoginFailed"), Strings.Get("MainForm_SomethingWentWrongTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTradeConfirmations_Click(object sender, EventArgs e)
        {
            if (currentAccount == null) return;

            string oText = btnTradeConfirmations.Text;
            btnTradeConfirmations.Text = Strings.Get("Common_Loading");
            btnTradeConfirmations.Text = oText;

            ConfirmationFormWeb confirms = new ConfirmationFormWeb(currentAccount);
            confirms.Show();
        }

        private void btnManageEncryption_Click(object sender, EventArgs e)
        {
            if (manifest.Encrypted)
            {
                InputForm currentPassKeyForm = new InputForm(Strings.Get("MainForm_EnterCurrentPasskeyShort"), true);
                currentPassKeyForm.ShowDialog();

                if (currentPassKeyForm.Canceled)
                {
                    return;
                }

                string curPassKey = currentPassKeyForm.txtBox.Text;

                InputForm changePassKeyForm = new InputForm(Strings.Get("MainForm_EnterNewPasskeyOrRemove"));
                changePassKeyForm.ShowDialog();

                if (changePassKeyForm.Canceled && !string.IsNullOrEmpty(changePassKeyForm.txtBox.Text))
                {
                    return;
                }

                InputForm changePassKeyForm2 = new InputForm(Strings.Get("MainForm_ConfirmNewPasskeyOrRemove"));
                changePassKeyForm2.ShowDialog();

                if (changePassKeyForm2.Canceled && !string.IsNullOrEmpty(changePassKeyForm.txtBox.Text))
                {
                    return;
                }

                string newPassKey = changePassKeyForm.txtBox.Text;
                string confirmPassKey = changePassKeyForm2.txtBox.Text;

                if (newPassKey != confirmPassKey)
                {
                    MessageBox.Show(Strings.Get("Common_PasskeysDoNotMatch"));
                    return;
                }

                if (newPassKey.Length == 0)
                {
                    newPassKey = null;
                }

                bool removing = newPassKey == null;
                if (!manifest.ChangeEncryptionKey(curPassKey, newPassKey))
                {
                    MessageBox.Show(Strings.Get(removing ? "MainForm_UnableToRemovePasskey" : "MainForm_UnableToChangePasskey"));
                }
                else
                {
                    MessageBox.Show(Strings.Get(removing ? "MainForm_PasskeyRemoved" : "MainForm_PasskeyChanged"));
                    this.loadAccountsList();
                }
            }
            else
            {
                passKey = manifest.PromptSetupPassKey();
                this.loadAccountsList();
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            CopyLoginToken();
        }


        // Tool strip menu handlers

        private void menuQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void menuRemoveAccountFromManifest_Click(object sender, EventArgs e)
        {
            if (manifest.Encrypted)
            {
                MessageBox.Show(Strings.Get("MainForm_CannotRemoveEncrypted"), Strings.Get("MainForm_MenuRemoveFromManifest"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                DialogResult res = MessageBox.Show(Strings.Get("MainForm_RemoveFromManifestConfirm"), Strings.Get("MainForm_MenuRemoveFromManifest"), MessageBoxButtons.OKCancel);
                if (res == DialogResult.OK)
                {
                    manifest.RemoveAccount(currentAccount, false);
                    MessageBox.Show(Strings.Get("MainForm_RemovedFromManifest"), Strings.Get("MainForm_MenuRemoveFromManifest"));
                    loadAccountsList();
                }
            }
        }

        private void menuLoginAgain_Click(object sender, EventArgs e)
        {
            this.PromptRefreshLogin(currentAccount);
        }

        private void menuImportAccount_Click(object sender, EventArgs e)
        {
            ImportAccountForm currentImport_maFile_Form = new ImportAccountForm();
            currentImport_maFile_Form.ShowDialog();
            loadAccountsList();
        }

        private void menuSettings_Click(object sender, EventArgs e)
        {
            new SettingsForm().ShowDialog();
            manifest = Manifest.GetManifest(true);
            loadSettings();
        }

        private async void menuDeactivateAuthenticator_Click(object sender, EventArgs e)
        {
            if (currentAccount == null) return;

            // Check for a valid refresh token first
            if (currentAccount.Session.IsRefreshTokenExpired())
            {
                MessageBox.Show(Strings.Get("Common_SessionExpired"), Strings.Get("MainForm_MenuDeactivateAuthenticator"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check for a valid access token, refresh it if needed
            if (currentAccount.Session.IsAccessTokenExpired())
            {
                try
                {
                    await currentAccount.Session.RefreshAccessToken();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Strings.Get("MainForm_DeactivateAuthenticatorErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            DialogResult res = MessageBox.Show(Strings.Get("MainForm_DeactivateConfirmPrompt"), Strings.Get("MainForm_DeactivateAuthenticatorTitlePrefix") + currentAccount.AccountName, MessageBoxButtons.YesNoCancel);
            int scheme = 0;
            if (res == DialogResult.Yes)
            {
                scheme = 2;
            }
            else if (res == DialogResult.No)
            {
                scheme = 1;
            }
            else if (res == DialogResult.Cancel)
            {
                scheme = 0;
            }

            if (scheme != 0)
            {
                string confCode = currentAccount.GenerateSteamGuardCode();
                InputForm confirmationDialog = new InputForm(String.Format(Strings.Get("MainForm_RemovingSteamGuardFormat"), currentAccount.AccountName, confCode));
                confirmationDialog.ShowDialog();

                if (confirmationDialog.Canceled)
                {
                    return;
                }

                string enteredCode = confirmationDialog.txtBox.Text.ToUpper();
                if (enteredCode != confCode)
                {
                    MessageBox.Show(Strings.Get("MainForm_ConfCodesDontMatch"));
                    return;
                }

                bool success = await currentAccount.DeactivateAuthenticator(scheme);
                if (success)
                {
                    MessageBox.Show(Strings.Get(scheme == 2 ? "MainForm_SteamGuardRemovedCompletely" : "MainForm_SteamGuardSwitchedEmail"));
                    this.manifest.RemoveAccount(currentAccount);
                    this.loadAccountsList();
                }
                else
                {
                    MessageBox.Show(Strings.Get("MainForm_DeactivateFailed"));
                }
            }
            else
            {
                MessageBox.Show(Strings.Get("MainForm_DeactivateNoAction"));
            }
        }

        // Tray menu handlers
        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            trayRestore_Click(sender, EventArgs.Empty);
        }

        private void trayRestore_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void trayQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trayTradeConfirmations_Click(object sender, EventArgs e)
        {
            btnTradeConfirmations_Click(sender, e);
        }

        private void trayCopySteamGuard_Click(object sender, EventArgs e)
        {
            if (txtLoginToken.Text != "")
            {
                Clipboard.SetText(txtLoginToken.Text);
            }
        }

        private void trayAccountList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (trayAccountList.SelectedIndex < 0) return;

            SteamGuardAccount account = allAccounts.FirstOrDefault(a => GetDisplayName(a) == (string)trayAccountList.SelectedItem);
            if (account == null) return;

            foreach (ListViewItem item in listAccounts.Items)
            {
                if (ReferenceEquals(item.Tag, account))
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }


        // Misc UI handlers
        private void listAccounts_SelectedValueChanged(object sender, EventArgs e)
        {
            if (listAccounts.SelectedItems.Count == 0) return;

            SteamGuardAccount account = (SteamGuardAccount)listAccounts.SelectedItems[0].Tag;
            trayAccountList.Text = GetDisplayName(account);
            currentAccount = account;
            loadAccountInfo();
        }

        private void txtAccSearch_TextChanged(object sender, EventArgs e)
        {
            listAccounts.Items.Clear();
            trayAccountList.Items.Clear();

            foreach (SteamGuardAccount account in allAccounts)
            {
                if (!IsFilter(account)) continue;

                listAccounts.Items.Add(CreateAccountListItem(account));
                trayAccountList.Items.Add(GetDisplayName(account));
            }
        }


        // Timers

        private async void timerSteamGuard_Tick(object sender, EventArgs e)
        {
            lblStatus.Text = Strings.Get("MainForm_AligningTime");
            steamTime = await TimeAligner.GetSteamTimeAsync();
            lblStatus.Text = "";

            currentSteamChunk = steamTime / 30L;
            int secondsUntilChange = (int)(steamTime - (currentSteamChunk * 30L));

            loadAccountInfo();
            if (currentAccount != null)
            {
                pbTimeout.Value = 30 - secondsUntilChange;
            }
        }

        private async void timerTradesPopup_Tick(object sender, EventArgs e)
        {
            if (currentAccount == null || popupFrm.Visible) return;
            if (!confirmationsSemaphore.Wait(0))
            {
                return; //Only one thread may access this critical section at once. Mutex is a bad choice here because it'll cause a pileup of threads.
            }

            List<Confirmation> confs = new List<Confirmation>();
            Dictionary<SteamGuardAccount, List<Confirmation>> autoAcceptConfirmations = new Dictionary<SteamGuardAccount, List<Confirmation>>();

            SteamGuardAccount[] accs =
                manifest.CheckAllAccounts ? allAccounts : new SteamGuardAccount[] { currentAccount };

            try
            {
                lblStatus.Text = Strings.Get("MainForm_CheckingConfirmations");

                foreach (var acc in accs)
                {
                    // Check for a valid refresh token first
                    if (acc.Session.IsRefreshTokenExpired())
                    {
                        MessageBox.Show(String.Format(Strings.Get("MainForm_SessionExpiredForAccountFormat"), acc.AccountName), Strings.Get("ConfirmationFormWeb_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        PromptRefreshLogin(acc);
                        break;
                    }

                    // Check for a valid access token, refresh it if needed
                    if (acc.Session.IsAccessTokenExpired())
                    {
                        try
                        {
                            lblStatus.Text = Strings.Get("MainForm_RefreshingSession");
                            await acc.Session.RefreshAccessToken();
                            lblStatus.Text = Strings.Get("MainForm_CheckingConfirmations");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, Strings.Get("Common_SteamLoginErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                    }

                    try
                    {
                        Confirmation[] tmp = await acc.FetchConfirmationsAsync();
                        foreach (var conf in tmp)
                        {
                            if ((conf.ConfType == Confirmation.EMobileConfirmationType.MarketListing && manifest.AutoConfirmMarketTransactions) ||
                                (conf.ConfType == Confirmation.EMobileConfirmationType.Trade && manifest.AutoConfirmTrades))
                            {
                                if (!autoAcceptConfirmations.ContainsKey(acc))
                                    autoAcceptConfirmations[acc] = new List<Confirmation>();
                                autoAcceptConfirmations[acc].Add(conf);
                            }
                            else
                                confs.Add(conf);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                lblStatus.Text = "";

                if (confs.Count > 0)
                {
                    popupFrm.Confirmations = confs.ToArray();
                    popupFrm.Popup();
                }
                if (autoAcceptConfirmations.Count > 0)
                {
                    foreach (var acc in autoAcceptConfirmations.Keys)
                    {
                        var confirmations = autoAcceptConfirmations[acc].ToArray();
                        await acc.AcceptMultipleConfirmations(confirmations);
                    }
                }
            }
            catch (SteamGuardAccount.WGTokenInvalidException)
            {
                lblStatus.Text = "";
            }

            confirmationsSemaphore.Release();
        }

        // Other methods

        private void CopyLoginToken()
        {
            string text = txtLoginToken.Text;
            if (String.IsNullOrEmpty(text))
                return;
            Clipboard.SetText(text);
        }

        /// <summary>
        /// Display a login form to the user to refresh their OAuth Token
        /// </summary>
        /// <param name="account">The account to refresh</param>
        private void PromptRefreshLogin(SteamGuardAccount account)
        {
            var loginForm = new LoginForm(LoginForm.LoginType.Refresh, account);
            loginForm.ShowDialog();
        }

        /// <summary>
        /// Load UI with the current account info, this is run every second
        /// </summary>
        private void loadAccountInfo()
        {
            if (currentAccount != null && steamTime != 0)
            {
                popupFrm.Account = currentAccount;
                txtLoginToken.Text = currentAccount.GenerateSteamGuardCodeForTime(steamTime);
                groupAccount.Text = String.Format(Strings.Get("MainForm_AccountNameFormat"), currentAccount.AccountName);
            }
        }

        /// <summary>
        /// Decrypts files and populates list UI with accounts
        /// </summary>
        private void loadAccountsList()
        {
            currentAccount = null;

            listAccounts.Items.Clear();
            trayAccountList.Items.Clear();
            trayAccountList.SelectedIndex = -1;

            allAccounts = manifest.GetAllAccounts(passKey);

            if (allAccounts.Length > 0)
            {
                foreach (SteamGuardAccount account in allAccounts)
                {
                    listAccounts.Items.Add(CreateAccountListItem(account));
                    trayAccountList.Items.Add(GetDisplayName(account));
                }

                if (listAccounts.Items.Count > 0)
                {
                    listAccounts.Items[0].Selected = true;
                    listAccounts.Items[0].Focused = true;
                }
                trayAccountList.SelectedIndex = 0;
            }
            menuDeactivateAuthenticator.Enabled = btnTradeConfirmations.Enabled = btnLoginViaQr.Enabled = allAccounts.Length > 0;

            _ = RefreshMissingNicknamesAsync(allAccounts);
        }

        /// <summary>
        /// Builds a table row (login + cached nickname column) for the accounts list, with the
        /// account itself stashed in Tag so selection doesn't need fragile string matching.
        /// </summary>
        private ListViewItem CreateAccountListItem(SteamGuardAccount account)
        {
            var entry = manifest.Entries.FirstOrDefault(e => e.SteamID == account.Session.SteamID);
            var item = new ListViewItem(account.AccountName) { Tag = account };
            item.SubItems.Add(entry?.PersonaName ?? "");
            return item;
        }

        /// <summary>
        /// Returns "login (Nickname)" if we have a cached Steam persona name for this account, otherwise just the login.
        /// Used for the tray dropdown, which is a plain combo box rather than a table.
        /// </summary>
        private string GetDisplayName(SteamGuardAccount account)
        {
            var entry = manifest.Entries.FirstOrDefault(e => e.SteamID == account.Session.SteamID);
            return entry != null && !string.IsNullOrEmpty(entry.PersonaName)
                ? $"{account.AccountName} ({entry.PersonaName})"
                : account.AccountName;
        }

        // Re-fetch a cached nickname if it's older than this, in case the player renamed themselves.
        private static readonly TimeSpan NicknameRefreshInterval = TimeSpan.FromDays(7);

        /// <summary>
        /// Looks up the Steam persona name (nickname) for accounts that don't have one cached yet
        /// or whose cached one is older than <see cref="NicknameRefreshInterval"/>, caches it in
        /// the manifest, and updates the already-rendered list items in place.
        /// </summary>
        private async Task RefreshMissingNicknamesAsync(SteamGuardAccount[] accountsSnapshot)
        {
            bool anySaved = false;

            foreach (var account in accountsSnapshot)
            {
                if (allAccounts != accountsSnapshot) return; // list was reloaded since; abandon this stale batch

                var entry = manifest.Entries.FirstOrDefault(e => e.SteamID == account.Session.SteamID);
                if (entry == null)
                    continue;

                bool isStale = entry.PersonaNameUpdated == null
                    || DateTime.UtcNow - entry.PersonaNameUpdated.Value > NicknameRefreshInterval;
                if (!string.IsNullOrEmpty(entry.PersonaName) && !isStale)
                    continue;

                string oldTrayDisplayName = GetDisplayName(account);
                string personaName = await FetchPersonaNameAsync(account);
                if (string.IsNullOrEmpty(personaName))
                    continue;

                entry.PersonaName = personaName;
                entry.PersonaNameUpdated = DateTime.UtcNow;
                anySaved = true;

                if (allAccounts != accountsSnapshot) return;

                foreach (ListViewItem item in listAccounts.Items)
                {
                    if (ReferenceEquals(item.Tag, account))
                    {
                        item.SubItems[1].Text = personaName;
                        break;
                    }
                }
                ReplaceListItem(trayAccountList.Items, oldTrayDisplayName, GetDisplayName(account));
            }

            if (anySaved)
            {
                manifest.Save();
            }
        }

        private static void ReplaceListItem(System.Collections.IList items, string oldText, string newText)
        {
            int index = items.IndexOf(oldText);
            if (index >= 0)
            {
                items[index] = newText;
            }
        }

        private static readonly HttpClient _profileHttpClient = new HttpClient();

        // Steam account IDs (the 32-bit part used by the miniprofile endpoint) are the
        // 64-bit SteamID minus this fixed offset (the base value of the "individual" ID range).
        private const ulong SteamId64ToAccountIdOffset = 76561197960265728UL;

        // Key-free endpoint - the same one Steam's own site uses for hover-card previews.
        private async Task<string> FetchPersonaNameAsync(SteamGuardAccount account)
        {
            try
            {
                ulong accountId32 = account.Session.SteamID - SteamId64ToAccountIdOffset;
                string url = $"https://steamcommunity.com/miniprofile/{accountId32}/json";

                string json = await _profileHttpClient.GetStringAsync(url);
                var profile = JsonConvert.DeserializeObject<MiniProfileResponse>(json);

                return string.IsNullOrWhiteSpace(profile?.PersonaName) ? null : profile.PersonaName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private class MiniProfileResponse
        {
            [JsonProperty("persona_name")]
            public string PersonaName { get; set; }
        }

        private void listAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
                {
                    if (listAccounts.SelectedIndices.Count == 0) return;
                    int from = listAccounts.SelectedIndices[0];
                    int to = from - (e.KeyCode == Keys.Up ? 1 : -1);
                    manifest.MoveEntry(from, to);
                    loadAccountsList();
                }
                return;
            }

            if (!IsKeyAChar(e.KeyCode) && !IsKeyADigit(e.KeyCode))
            {
                return;
            }

            txtAccSearch.Focus();
            txtAccSearch.Text = e.KeyCode.ToString();
            txtAccSearch.SelectionStart = 1;
        }

        private static bool IsKeyAChar(Keys key)
        {
            return key >= Keys.A && key <= Keys.Z;
        }

        private static bool IsKeyADigit(Keys key)
        {
            return (key >= Keys.D0 && key <= Keys.D9) || (key >= Keys.NumPad0 && key <= Keys.NumPad9);
        }

        private bool IsFilter(SteamGuardAccount account)
        {
            string f = GetDisplayName(account);
            if (txtAccSearch.Text.StartsWith("~"))
            {
                try
                {
                    return Regex.IsMatch(f, txtAccSearch.Text);
                }
                catch (Exception)
                {
                    return true;
                }

            }
            else
            {
                return f.Contains(txtAccSearch.Text.ToLower());
            }
        }

        private void loadSettings()
        {
            timerTradesPopup.Enabled = manifest.PeriodicChecking;
            timerTradesPopup.Interval = manifest.PeriodicCheckingInterval * 1000;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
            {
                CopyLoginToken();
            }
        }

        private void panelButtons_SizeChanged(object sender, EventArgs e)
        {
            int totButtons = panelButtons.Controls.OfType<Button>().Count();

            Point curPos = new Point(0, 0);
            foreach (Button but in panelButtons.Controls.OfType<Button>())
            {
                but.Width = panelButtons.Width / totButtons;
                but.Location = curPos;
                curPos = new Point(curPos.X + but.Width, 0);
            }
        }
    }
}
