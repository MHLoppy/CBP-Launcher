using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CBPSetupGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.UpgradeRequired == true)
            {
                UpgradeSettings();
                SaveSettings();
            }

            DefaultChecker();
        }

        private static int Location = 0;
        // 0 = unknown
        // 1 = RoN root folder (where we want it)
        // 2 = Workshop mods folder (where we expect it to be the first time)
        // 3 = local mods folder
        // 4 = Workshop mods folder, but pre-release

        private static bool CBPL = false;

        private static string CBPLExe = "";
        private static string CBPLExeUpdate = "";

        // CBP Setup handles updating CBP Launcher (and its language files); CBPL handles updating CBPS (and its language files)
        private static string CBPLDll = "";
        private static string CBPLDllUpdate = "";

        private static readonly string CBPSFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));
        private static readonly string CBPS = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            await Primary();
        }

        private async Task Primary()
        {
            //step -1: make sure we can actually load the language files
            await VibeCheck();

            // Step 0: don't overlap the streams (check if already running)
            await MasculinityCheck();

            // Step 1: figure out what location exe is running from
            await WhereTheBloodyHellAreYou();

            //Step 2: does CBP launcher exist? (if no, say error, if yes continue)
            await CheckForCBPL ();

            //Step 3: is it up to date? if yes continue, if no, update it and continue (if error updating, say error)
            await CBPLVersionCheck();

            // Step 4: launch CBP launcher
            await StartCBPL();

            //CBPS exits if CBP Launcher is running
            await ProcessCheck("CBP Launcher");
            await Conclusion();

            async Task VibeCheck()
            {
                try
                {
                    PrimaryLog.Text += CBPSetupGUI.Language.Resources.StartupLanguageDetected.ToString();
                }
                catch (Exception ex)
                {
                    string lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;

                    switch (lang)
                    {
                        case "en":
                            MessageBox.Show("Unable to access language files (there should be a dll for CBP Setup to use).");
                            await DelayedClose("Unable to access language files (there should be a dll for CBP Setup to use)." + "\n" + ex, -1);
                            break;
                        case "zh":
                            await DelayedClose("无法访问语言文件（应该有一个dll供CBP Setup使用）。" + "\n" + ex, -1);
                            break;
                        case "fr":
                            await DelayedClose("Impossible d'accéder aux fichiers de langue (il devrait y avoir une dll à utiliser par CBP Setup)." + "\n" + ex, -1);
                            break;
                        case "de":
                            await DelayedClose("Zugriff auf Sprachdateien nicht möglich (es sollte eine dll für CBP Setup zur Verfügung stehen)." + "\n" + ex, -1);
                            break;
                        case "it":
                            await DelayedClose("Impossibile accedere ai file di lingua (dovrebbe esserci una dll da usare per CBP Setup)." + "\n" + ex, -1);
                            break;
                        case "ja":
                            await DelayedClose("言語ファイルにアクセスできません（CBP Setupが使用するDLLがあるはずです）。" + "\n" + ex, -1);
                            break;
                        case "ko":
                            await DelayedClose("언어 파일에 액세스 할 수 없습니다 (CBP Setup가 사용할 dll이 있어야 함). " + "\n" + ex, -1);
                            break;
                        case "pt":
                            await DelayedClose("Impossibilidade de aceder aos ficheiros linguísticos (deve haver um dll para o CBP Setup utilizar)." + "\n" + ex, -1);
                            break;
                        case "ru":
                            await DelayedClose("Невозможно получить доступ к языковым файлам (должна существовать dll для использования CBP Setup)." + "\n" + ex, -1);
                            break;
                        case "es":
                            await DelayedClose("No se puede acceder a los archivos de idioma (debería haber un dll para que el CBP Setup lo utilice)." + "\n" + ex, -1);
                            break;
                        default:
                            await DelayedClose("Unable to access language files (there should be a dll for CBP Setup to use)." + "\n" + ex, -1);
                            break;
                    }
                }
                await SlowDown();
                PrimaryLog.Text += "\n\n" + CBPSetupGUI.Language.Resources.StartupMessage.ToString();
                await SlowDown();
            }

            async Task MasculinityCheck()
            {
                // longwinded way of checking if another copy of the process is already running; mutex would be better but slightly more complex
                if (Process.GetProcessesByName(CBPS).Count() > 1)
                {
                    MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorAlreadyRunning);
                    await DelayedClose(CBPSetupGUI.Language.Resources.ErrorAlreadyRunning, 1056);
                    return;
                }
            }

            async Task WhereTheBloodyHellAreYou()//not sure if this needs the async tag given that it has nothing to do and displays no message
            {
                if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, "riseofnations.exe"))))
                {
                    //RoN root folder
                    Location = 1;
                }

                if (Path.GetFullPath(Path.Combine(CBPSFolder, @"..\", "2287791153")).ToString() == Path.GetFullPath(CBPSFolder).ToString())
                {
                    //workshop mods folder
                    Location = 2;
                }

                if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, @"..\", "mod-status.txt"))))
                {
                    //local mods folder
                    Location = 3;
                }

                if (Path.GetFullPath(Path.Combine(CBPSFolder, @"..\", "2528425253")).ToString() == Path.GetFullPath(CBPSFolder).ToString())
                {
                    //workshop mods folder, but pre-release
                    Location = 4;
                }
            }

            // condenses multiple steps into one; slightly harder to read but easier to make for me /shrug
            // just remember that each of the paths are heavily duplicated (but I don't think it's worth the trouble of making more sophisticated logic to avoid it right now)
            async Task CheckForCBPL()
            {
                // change the path it checks based on where it thinks it is
                // pretty sure this isn't a particularly efficient way of doing this, but it shouldn't really matter
                switch (Location)
                {
                    case 0:

                        MessageBox.Show(CBPSetupGUI.Language.Resources.LocationCase0);
                        await DelayedClose(CBPSetupGUI.Language.Resources.LocationCase0, 3);
                        break;

                    case 1:
                        PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase1;

                        try
                        {
                            CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.exe"));
                            CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));

                            CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBP Launcher.exe"));
                            CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBP Launcher.Language.dll"));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(CBPSetupGUI.Language.Resources.LocationPathError);
                            await DelayedClose(CBPSetupGUI.Language.Resources.LocationPathError + "\n" + ex, 3);
                        }
                        await SlowDown();

                        if (File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "CBP Launcher.exe"))))
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.FoundRootYes;
                            CBPL = true;
                        }
                        else
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.FoundRootNo;
                            CBPL = false;
                        }
                        break;

                    case int _ when (Location == 2 || Location == 4)://parens just for my sake

                        try
                        {
                            CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.exe"));
                            CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.Language.dll"));

                            // because CBP Setup is running from each respective mod folder, the launcher/dll are automatically going to be in the same location both on normal and pre-release versions
                            CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.exe"));
                            CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));
                        }

                        catch (Exception ex)
                        {
                            MessageBox.Show(CBPSetupGUI.Language.Resources.LocationPathError);
                            await DelayedClose(CBPSetupGUI.Language.Resources.LocationPathError + "\n" + ex, 3);
                        }
                        await SlowDown();

                        if (Location == 2)
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase2;
                        }

                        if (Location == 4)
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase4;
                        }

                        await SlowDown();
                        if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.exe"))))
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.FoundRootYes;
                            CBPL = true;
                        }
                        else
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.FoundRootNo;
                            CBPL = false;
                        }
                        break;

                    case 3:
                        PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase3;

                        try
                        {
                            CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", "CBP Launcher.exe"));
                            CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", "CBP Launcher.Language.dll"));

                            CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"workshop\content\287450\2287791153", "CBP Launcher.exe"));
                            CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"workshop\content\287450\2287791153", "CBP Launcher.Language.dll"));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(CBPSetupGUI.Language.Resources.LocationPathError);
                            await DelayedClose(CBPSetupGUI.Language.Resources.LocationPathError + "\n" + ex, 3);
                        }

                        await SlowDown();

                        if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"CBP Launcher.exe"))))
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.FoundRootYes;
                            CBPL = true;
                        }
                        else
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.FoundRootNo;
                            CBPL = false;
                        }
                        break;

                    default:
                        await SlowDown();
                        MessageBox.Show(CBPSetupGUI.Language.Resources.LocationCaseDefault);
                        await DelayedClose(CBPSetupGUI.Language.Resources.LocationCaseDefault, -1);
                        break;
                }
            }

            async Task CBPLVersionCheck()
            {
                if (CBPL == true)
                {
                    try
                    {
                        //https://stackoverflow.com/questions/11350008/how-to-get-exe-file-version-number-from-file-path/23325102#23325102
                        var newVersionShort = FileVersionInfo.GetVersionInfo(CBPLExeUpdate);
                        string newVersionFull = newVersionShort.FileVersion;

                        var oldVersionShort = FileVersionInfo.GetVersionInfo(CBPLExe);
                        string oldVersionFull = oldVersionShort.FileVersion;
                        await SlowDown();

                        if (newVersionFull == oldVersionFull)
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.VersionCheckSame;
                            return;
                        }
                        else
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.VersionCheckDifferent;

                            try
                            {
                                File.Move(CBPLExe, Path.Combine(CBPLExe + "old"));
                                File.Copy(CBPLExeUpdate, CBPLExe);

                                File.Move(CBPLDll, Path.Combine(CBPLDll + "old"));
                                File.Copy(CBPLDllUpdate, CBPLDll);
                            }
                            catch (Exception ex)
                            {
                                await SlowDown();
                                try
                                {
                                    PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.OldVersionRestore;
                                    File.Move(Path.Combine(CBPLExe + "old"), CBPLExe);
                                    File.Move(Path.Combine(CBPLDll + "old"), CBPLDll);
                                }
                                catch (Exception ex2)
                                {
                                    MessageBox.Show(CBPSetupGUI.Language.Resources.OldVersionRestoreError);
                                    PrimaryLog.Text += CBPSetupGUI.Language.Resources.OldVersionRestoreError + "\n" + ex2;
                                }

                                if (ex is UnauthorizedAccessException)
                                {
                                    MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorPermissions);
                                    await DelayedClose(CBPSetupGUI.Language.Resources.ErrorPermissions + "\n" + ex, -1);
                                }
                                if (ex is FileNotFoundException)
                                {
                                    MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorFileNotFound);
                                    await DelayedClose(CBPSetupGUI.Language.Resources.ErrorFileNotFound + "\n" + ex, -1);
                                }
                                if (ex is IOException)
                                {
                                    MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorIO);
                                    await DelayedClose(CBPSetupGUI.Language.Resources.ErrorIO + "\n" + ex, -1);
                                }
                                else
                                {
                                    MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorUnknown);
                                    await DelayedClose(CBPSetupGUI.Language.Resources.ErrorUnknown + "\n" + ex, -1);
                                }
                            }
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.DeletingFiles;

                            try
                            {
                                File.Delete(Path.Combine(CBPLExe + "old"));
                                File.Delete(Path.Combine(CBPLDll + "old"));
                                await SlowDown();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(CBPSetupGUI.Language.Resources.DeletingFilesError);
                                await DelayedClose(CBPSetupGUI.Language.Resources.DeletingFilesError + "\n" + ex, -1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(CBPSetupGUI.Language.Resources.VersionCheckFail);
                        await DelayedClose(CBPSetupGUI.Language.Resources.VersionCheckFail + "\n" + ex, -1);
                    }
                }
                else if (CBPL == false)
                {
                    await SlowDown();
                    try
                    {
                        File.Copy(CBPLExeUpdate, CBPLExe);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(CBPSetupGUI.Language.Resources.CopyToRootError);
                        await DelayedClose(CBPSetupGUI.Language.Resources.CopyToRootError + "\n" + ex, -1);
                        return;
                    }
                }
            }

            async Task StartCBPL()
            {
                await SlowDown();
                PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.StartCBPL;
                await SlowDown();

                try
                {
                    // I'm not actually sure if this whole shebang is necessary just to start it, but I've done it anyway
                    ProcessStartInfo _ = new ProcessStartInfo(CBPLExe)
                    {
                        WorkingDirectory = CBPLExe + @"..\"
                    };
                    Process.Start(CBPLExe);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(CBPSetupGUI.Language.Resources.StartCBPLProblem);
                    PrimaryLog.Text += CBPSetupGUI.Language.Resources.StartCBPLProblem + "\n" + ex;
                }

            }

            async Task<bool> ProcessCheck(string processName)
            {
                await Delay(600);//wait too long and it could give a false negative on fast system (crash/close); too short and you get a false negative on a slow system (still loading)
                return Process.GetProcessesByName(processName).Length > 0;
            }

            async Task Conclusion()
            {
                if (await ProcessCheck("CBP Launcher") == false)
                {
                    await SlowDown();
                    MessageBox.Show(CBPSetupGUI.Language.Resources.StartCBPLFail);
                    await DelayedClose(CBPSetupGUI.Language.Resources.StartCBPLFail, -1);
                    return;
                }
                else
                {
                    await SlowDown();
                    await DelayedClose(CBPSetupGUI.Language.Resources.StartCBPLSuccess, 0);
                    return;
                }
            }

            async Task DelayedClose(string str, int code)
            {
                PrimaryLog.Text += "\n" + str;
                await Delay(5000);
                Application.Current.Shutdown(code);
                return;
            }

            async Task Delay(int ms)
            {
                Task pause = Task.Delay(ms);
                await pause;
            }

            async Task SlowDown()
            {
                if (Properties.Settings.Default.SlowMode == true)
                {
                    await Delay(2250);
                }
            }
        }

        private void SlowModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SlowMode = true;
            SaveSettings();
        }

        private void SlowModeCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SlowMode = false;
            SaveSettings();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void UpgradeSettings()
        {
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.UpgradeRequired = false;
        }

        private void DefaultChecker()
        {
            if (Properties.Settings.Default.SlowMode == true)
            {
                SlowModeCheckBox.IsChecked = true;
            }
            else if (Properties.Settings.Default.SlowMode == false)
            {
                SlowModeCheckBox.IsChecked = false;
            }
        }
    }
}
