using System;
using System.Windows.Forms;

namespace Steam_Desktop_Authenticator
{
    public partial class SettingsForm : Form
    {
        Manifest manifest;
        bool fullyLoaded = false;

        private class LanguageItem
        {
            public string Name;
            public string Code;
            public override string ToString() => Name;
        }

        private static readonly LanguageItem[] AvailableLanguages = new[]
        {
            new LanguageItem { Name = "English", Code = "en" },
            new LanguageItem { Name = "Русский", Code = "ru" },
            new LanguageItem { Name = "Українська", Code = "uk" },
        };

        public SettingsForm()
        {
            InitializeComponent();

            // Get latest manifest
            manifest = Manifest.GetManifest(true);

            chkPeriodicChecking.Checked = manifest.PeriodicChecking;
            numPeriodicInterval.Value = manifest.PeriodicCheckingInterval;
            chkCheckAll.Checked = manifest.CheckAllAccounts;
            chkConfirmMarket.Checked = manifest.AutoConfirmMarketTransactions;
            chkConfirmTrades.Checked = manifest.AutoConfirmTrades;

            cmbLanguage.Items.AddRange(AvailableLanguages);
            int selectedLanguage = Array.FindIndex(AvailableLanguages, l => l.Code == manifest.Language);
            cmbLanguage.SelectedIndex = selectedLanguage >= 0 ? selectedLanguage : 0;

            SetControlsEnabledState(chkPeriodicChecking.Checked);

            fullyLoaded = true;
        }

        private void SetControlsEnabledState(bool enabled)
        {
            numPeriodicInterval.Enabled = chkCheckAll.Enabled = chkConfirmMarket.Enabled = chkConfirmTrades.Enabled = enabled;
        }

        private void ShowWarning(CheckBox affectedBox)
        {
            if (!fullyLoaded) return;

            var result = MessageBox.Show(Strings.Get("SettingsForm_SecurityWarning"), Strings.Get("Common_Warning"), MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                affectedBox.Checked = false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string oldLanguage = manifest.Language;
            string newLanguage = ((LanguageItem)cmbLanguage.SelectedItem).Code;

            manifest.PeriodicChecking = chkPeriodicChecking.Checked;
            manifest.PeriodicCheckingInterval = (int)numPeriodicInterval.Value;
            manifest.CheckAllAccounts = chkCheckAll.Checked;
            manifest.AutoConfirmMarketTransactions = chkConfirmMarket.Checked;
            manifest.AutoConfirmTrades = chkConfirmTrades.Checked;
            manifest.Language = newLanguage;
            manifest.Save();

            if (newLanguage != oldLanguage)
            {
                MessageBox.Show(Strings.Get("SettingsForm_RestartRequired"), Strings.Get("SettingsForm_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            this.Close();
        }

        private void chkPeriodicChecking_CheckedChanged(object sender, EventArgs e)
        {
            SetControlsEnabledState(chkPeriodicChecking.Checked);
        }

        private void chkConfirmMarket_CheckedChanged(object sender, EventArgs e)
        {
            if (chkConfirmMarket.Checked)
                ShowWarning(chkConfirmMarket);
        }

        private void chkConfirmTrades_CheckedChanged(object sender, EventArgs e)
        {
            if (chkConfirmTrades.Checked)
                ShowWarning(chkConfirmTrades);
        }
    }
}
