using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using CommandLine;

namespace Steam_Desktop_Authenticator
{
    static class Program
    {
        public static Process PriorProcess()
        // Returns a System.Diagnostics.Process pointing to
        // a pre-existing process with the same name as the
        // current one, if any; or null if the current process
        // is unique.
        {
            try
            {
                Process curr = Process.GetCurrentProcess();
                Process[] procs = Process.GetProcessesByName(curr.ProcessName);
                foreach (Process p in procs)
                {
                    if ((p.Id != curr.Id) &&
                        (p.MainModule.FileName == curr.MainModule.FileName))
                        return p;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void ApplyLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return;

            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(language);
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // Unknown language code, fall back to the OS default.
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // run the program only once
            if (PriorProcess() != null)
            {
                MessageBox.Show(Strings.Get("Program_AlreadyRunning"), Strings.Get("App_Title"));
                return;
            }

            // Parse command line arguments
            CommandLineOptions options = new();
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(o => options = o);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Manifest man;

            try
            {
                man = Manifest.GetManifest();
                ApplyLanguage(man.Language);
            }
            catch (ManifestParseException)
            {
                // Manifest file was corrupted, generate a new one.
                try
                {
                    MessageBox.Show(Strings.Get("Common_SettingsCorrupted"), Strings.Get("App_Title"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    man = Manifest.GenerateNewManifest(true);
                    ApplyLanguage(man.Language);
                }
                catch (MaFileEncryptedException)
                {
                    // An maFile was encrypted, we're fucked.
                    MessageBox.Show(Strings.Get("Common_UnrecoverableEncrypted"), Strings.Get("App_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Process.Start(@"https://github.com/Jessecar96/SteamDesktopAuthenticator/wiki/Help!-I'm-locked-out-of-my-account");
                    return;
                }
            }

            if (man.FirstRun)
            {
                if (man.Entries.Count > 0)
                {
                    // Already has accounts, just run
                    MainForm mf = new MainForm();
                    mf.SetEncryptionKey(options.EncryptionKey);
                    mf.StartSilent(options.Silent);
                    Application.Run(mf);
                }
                else
                {
                    // No accounts, run welcome form
                    Application.Run(new WelcomeForm());
                }
            }
            else
            {
                MainForm mf = new MainForm();
                mf.SetEncryptionKey(options.EncryptionKey);
                mf.StartSilent(options.Silent);
                Application.Run(mf);
            }
        }
    }
}
