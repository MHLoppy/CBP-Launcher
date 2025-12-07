using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static CBPSetupGUI.App;//for SetLanguageDictionary, used for the English override
using LangRes = CBPSetupGUI.Language.Resources;

namespace CBPSetupGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.UpgradeRequired == true)
            {
                ReplacementSettingsReset();
                UpgradeSettings();
                SaveSettings();
            }

            DefaultChecker();
        }

        private static bool CBPL = false;

        // CBP Setup handles updating CBP Launcher (and its language files); CBPL handles updating CBPS (and its language files)
        private static string CBPLExe = "";
        private static string CBPLExeUpdate = "";

        private static readonly string CBPSFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));
        private static readonly string CBPSName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location); //"CBP Setup"
        
        private static string CBPVersionFile = "";
        private static int CBPVersion = 0;
        private static bool CBPPR = false;//used for debugging

        //private static string netFrameworkVersion => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            var window = GetWindow(this);
            window.KeyDown += HandleKeyPressF9;

            await Primary();
        }

        private async Task Primary()
        {
            //step -2: check .NET framework version
            //MessageBox.Show(netFrameworkVersion);
            
            await CheckForLanguageFiles();
            await CheckIfAlreadyRunning();

            // Step 1: figure out what location exe is running from
            int runningLocation = await FindRunningLocation();

            //Step 2: does CBP launcher exist? (if no, say error, if yes continue)
            await CheckForCBPL(runningLocation);
            await AutoConsentQuestion();

            //Step 3: is it up to date? if yes continue, if no, update it and continue (if error updating, say error)
            await CBPLVersionCheck();

            // Step 4: launch CBP launcher
            await StartCBPL();

            //CBPS exits if CBP Launcher is running
            await Conclusion();
        }

        async Task CheckForLanguageFiles()
        {
            try
            {
                if (LangFallback == true)
                {
                    PrimaryLog.Text += LangRes.UsingFallbackLanguage + "\n";
                    await SlowDown();
                }

                PrimaryLog.Text += LangRes.StartupLanguageDetected + " " + LangRes.FontSizeNotice;
            }
            catch (Exception ex)
            {
                string lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;

                switch (lang)
                {
                    //hardcoded error string for when language files are inaccessible
                    case "en":
                        MessageBox.Show("Unable to access language files (there should be a dll for CBP Setup to use).");
                        await DelayedClose("Unable to access language files (there should be a dll for CBP Setup to use)." + "\n" + ex, -1);
                        break;
                    case "zh":
                        MessageBox.Show("无法访问语言文件（应该有一个dll供CBP Setup使用）。");
                        await DelayedClose("无法访问语言文件（应该有一个dll供CBP Setup使用）。" + "\n" + ex, -1);
                        break;
                    case "fr":
                        MessageBox.Show("Impossible d'accéder aux fichiers de langue (il devrait y avoir une dll à utiliser par CBP Setup).");
                        await DelayedClose("Impossible d'accéder aux fichiers de langue (il devrait y avoir une dll à utiliser par CBP Setup)." + "\n" + ex, -1);
                        break;
                    case "de":
                        MessageBox.Show("Zugriff auf Sprachdateien nicht möglich (es sollte eine dll für CBP Setup zur Verfügung stehen).");
                        await DelayedClose("Zugriff auf Sprachdateien nicht möglich (es sollte eine dll für CBP Setup zur Verfügung stehen)." + "\n" + ex, -1);
                        break;
                    case "it":
                        MessageBox.Show("Impossibile accedere ai file di lingua (dovrebbe esserci una dll da usare per CBP Setup).");
                        await DelayedClose("Impossibile accedere ai file di lingua (dovrebbe esserci una dll da usare per CBP Setup)." + "\n" + ex, -1);
                        break;
                    case "ja":
                        MessageBox.Show("言語ファイルにアクセスできません（CBP Setupが使用するDLLがあるはずです）。");
                        await DelayedClose("言語ファイルにアクセスできません（CBP Setupが使用するDLLがあるはずです）。" + "\n" + ex, -1);
                        break;
                    case "ko":
                        MessageBox.Show("언어 파일에 액세스 할 수 없습니다 (CBP Setup가 사용할 dll이 있어야 함). ");
                        await DelayedClose("언어 파일에 액세스 할 수 없습니다 (CBP Setup가 사용할 dll이 있어야 함). " + "\n" + ex, -1);
                        break;
                    case "pt":
                        MessageBox.Show("Impossibilidade de aceder aos ficheiros linguísticos (deve haver um dll para o CBP Setup utilizar).");
                        await DelayedClose("Impossibilidade de aceder aos ficheiros linguísticos (deve haver um dll para o CBP Setup utilizar)." + "\n" + ex, -1);
                        break;
                    case "ru":
                        MessageBox.Show("Невозможно получить доступ к языковым файлам (должна существовать dll для использования CBP Setup).");
                        await DelayedClose("Невозможно получить доступ к языковым файлам (должна существовать dll для использования CBP Setup)." + "\n" + ex, -1);
                        break;
                    case "es":
                        MessageBox.Show("No se puede acceder a los archivos de idioma (debería haber un dll para que el CBP Setup lo utilice).");
                        await DelayedClose("No se puede acceder a los archivos de idioma (debería haber un dll para que el CBP Setup lo utilice)." + "\n" + ex, -1);
                        break;
                    default:
                        MessageBox.Show("Unable to access language files (there should be a dll for CBP Setup to use).");
                        await DelayedClose("Unable to access language files (there should be a dll for CBP Setup to use)." + "\n" + ex, -1);
                        break;
                }
            }
            await SlowDown();
            PrimaryLog.Text += "\n" + LangRes.StartupMessage + "\n";
            await SlowDown();

            // that extra delay at the start of this bit is mostly for non-English since their language string is slightly longer
            if (Properties.Settings.Default.FirstTimeRun == true)
            {
                await SlowDown();
                PrimaryLog.Text += "\n" + LangRes.FirstTimeRun + "\n";
                await SlowDown();
            }
        }

        async Task CheckIfAlreadyRunning()
        {
            // longwinded way of checking if another copy of the process is already running; mutex would be better but slightly more complex
            if (await ProcessCheck(CBPSName, 1) == true)
            {
                MessageBox.Show(LangRes.ErrorAlreadyRunning);
                await DelayedClose(LangRes.ErrorAlreadyRunning + "\n" + LangRes.WindowWillClose, 1056);
                return;
            }
            if (await ProcessCheck("CBPLauncher", 1) == true)
            {
                MessageBox.Show(LangRes.CBPLCurrentlyRunning);
                await DelayedClose(LangRes.CBPLCurrentlyRunning + "\n" + LangRes.WindowWillClose, 1056);
                return;
            }
        }

        async Task<int> FindRunningLocation()//there's not really anything to run async here
        {
            // RoN root folder
            if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, "riseofnations.exe"))))
            {
                return 1;
            }

            // workshop mods folder
            else if (Path.GetFullPath(Path.Combine(CBPSFolder, @"..\", "2287791153")) == CBPSFolder)
            {
                return 2;
            }

            // local mods folder
            else if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, @"..\", "mod-status.txt"))))
            {
                return 3;
            }

            // workshop mods folder, but pre-release
            else if (Path.GetFullPath(Path.Combine(CBPSFolder, @"..\", "2528425253")) == CBPSFolder)
            {
                return 4;
            }

            // Unknown location
            else return 0;
        }

        // condenses multiple steps into one; slightly harder to read but easier to make for me /shrug
        // just remember that each of the paths are heavily duplicated (but I don't think it's worth the trouble of making more sophisticated logic to avoid it right now)
        async Task CheckForCBPL(int location)
        {
            // change the path it checks based on where it thinks it is
            // pretty sure this isn't a particularly efficient way of doing this, but it shouldn't really matter
            switch (location)
            {
                case 0: // 0 = unknown

                    MessageBox.Show(LangRes.LocationCase0);
                    await DelayedClose(LangRes.LocationCase0 + "\n" + LangRes.WindowWillClose, 3);
                    break;

                case 1: // 1 = RoN root folder

                    PrimaryLog.Text += "\n" + LangRes.LocationCase1;

                    try
                    {
                        CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, "CBPLauncher.exe"));
                        CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBPLauncher.exe"));

                        // Oct 2021: to be quite honest I'm not really sure why we're checking for CBP version anyway, given that it's CBP Launcher's job (not setup's) to update CBP
                        // we already read cbpl's version elsewhere, why do we care about CBP version (other than to see if CBP is installed at all)?

                        // check for the version file in the mod-is-loaded location, otherwise check for it in the mod-is-unloaded location
                        if (File.Exists(Path.Combine(CBPSFolder, @"mods\Community Balance Patch\version.txt")))
                        {
                            // check if using PR by reading local mods version.txt file and take the last 2 digits; if TryParse fails, its result is false
                            // this check should only be required for location 1
                            CBPVersionFile = File.ReadAllText(Path.GetFullPath(Path.Combine(CBPSFolder, @"mods\Community Balance Patch\version.txt")));
                            string CBPVersionEnd = CBPVersionFile.ToString().Substring(CBPVersionFile.Length - 2);

                            //10 because all the pre-release versions start at x.y.z.11
                            if (int.TryParse(CBPVersionEnd, out CBPVersion) && CBPVersion > 10)
                            {
                                CBPPR = true;//not currently utilised much beyond a sanity check
                                CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2528425253", "CBPLauncher.exe"));
                            }
                        }
                        else if (File.Exists(Path.Combine(CBPSFolder, @"mods\Unloaded Mods\Community Balance Patch\version.txt")))
                        {
                            // even if CBP is unloaded, we should still update CBP Launcher, so should continue
                            CBPVersionFile = File.ReadAllText(Path.GetFullPath(Path.Combine(CBPSFolder, @"mods\Unloaded Mods\Community Balance Patch\version.txt")));
                            string CBPVersionEnd = CBPVersionFile.ToString().Substring(CBPVersionFile.Length - 2);

                            //10 because all the pre-release versions start at x.y.z.11
                            if (int.TryParse(CBPVersionEnd, out CBPVersion) && CBPVersion > 10)
                            {
                                CBPPR = true;//not currently utilised much beyond a sanity check
                                CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2528425253", "CBPLauncher.exe"));
                            }
                        }
                        else
                        {
                            MessageBox.Show(LangRes.CBPVersionFileNotFound);
                            await DelayedClose(LangRes.CBPVersionFileNotFound + "\n" + LangRes.WindowWillClose, -1);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(LangRes.LocationPathError);
                        await DelayedClose(LangRes.LocationPathError + "\n" + ex + "\n" + LangRes.WindowWillClose, 3);
                    }
                    await SlowDown();

                    if (File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "CBPLauncher.exe"))))
                    {
                        PrimaryLog.Text += "\n" + LangRes.FoundRootYes;
                        CBPL = true;
                    }
                    else
                    {
                        PrimaryLog.Text += "\n" + LangRes.FoundRootNo;
                        CBPL = false;
                    }
                    break;

                case int _ when (location == 2 || location == 4):
                    // 2 = Workshop mods folder (where we expect it to be the first time); 4 is pre-release
                    try
                    {
                        CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBPLauncher.exe"));
                        ///CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.Language.dll"));

                        // because CBP Setup is running from each respective mod folder, the launcher/dll are automatically going to be in the same *relative* location both on normal and pre-release versions
                        CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, "CBPLauncher.exe"));
                        ///CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));

                        CBPVersionFile = File.ReadAllText(Path.GetFullPath(Path.Combine(CBPSFolder, @"Community Balance Patch\version.txt")));
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show(LangRes.LocationPathError);
                        await DelayedClose(LangRes.LocationPathError + "\n" + ex + "\n" + LangRes.WindowWillClose, 3);
                    }
                    await SlowDown();

                    if (location == 2)
                    {
                        PrimaryLog.Text += "\n" + LangRes.LocationCase2;
                    }

                    if (location == 4)
                    {
                        PrimaryLog.Text += "\n" + LangRes.LocationCase4;
                    }

                    await SlowDown();
                    if (File.Exists(Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBPLauncher.exe"))))
                    {
                        PrimaryLog.Text += "\n" + LangRes.FoundRootYes;
                        CBPL = true;
                    }
                    else
                    {
                        PrimaryLog.Text += "\n" + LangRes.FoundRootNo;
                        CBPL = false;
                    }
                    break;

                case 3: // 3 = local mods folder
                default:
                    await SlowDown();
                    MessageBox.Show(LangRes.LocationCaseDefault);
                    await DelayedClose(LangRes.LocationCaseDefault + "\n" + LangRes.WindowWillClose, -1);
                    break;
            }
        }

        async Task AutoConsentQuestion()
        {
            if (Properties.Settings.Default.NeedAskAutoConsent == true)
            {
                if (MessageBox.Show(LangRes.DoYouWantAutoConsent, LangRes.DoYouWantACTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    Properties.Settings.Default.AutoConsent = true;

                Properties.Settings.Default.NeedAskAutoConsent = false;
                SaveSettings();
            }

            await SlowDown();
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
                        PrimaryLog.Text += "\n" + LangRes.VersionCheckSame;
                        return;
                    }
                    else
                    {
                        //ask user if it's okay to update [can autoconsent]
                        if (Properties.Settings.Default.AutoConsent == false)
                        {
                            if (MessageBox.Show(LangRes.VersionCheckDifferent, LangRes.ConsentNeeded, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                PrimaryLog.Text += "\n" + LangRes.VersionCheckDifferent + LangRes.ConsentYes;
                                await UpdateCBPL();
                                return;
                            }
                            else
                            {
                                await DelayedClose(LangRes.VersionCheckDifferent + LangRes.ConsentNo + "\n" + LangRes.WindowWillClose, 0);
                                return;
                            }
                        }
                        else
                        {
                            PrimaryLog.Text += "\n" + LangRes.VersionCheckDifferent + LangRes.ConsentIsCool;
                            await UpdateCBPL();
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LangRes.VersionCheckFail);
                    await DelayedClose(LangRes.VersionCheckFail + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                    return;
                }
            }
            else if (CBPL == false)//reasonably one could expect a bool to never return anything except a true or not-true-therefore-false value, but this is actually a sermon on the world not always being reasonable T_T
            {
                await SlowDown();

                // ask user if it's okay to copy CBPL to RoN folder [can autoconsent]
                if (Properties.Settings.Default.AutoConsent == false)
                {
                    if (MessageBox.Show(LangRes.CopyToRootConsent, LangRes.ConsentNeeded, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        PrimaryLog.Text += "\n" + LangRes.CopyToRootConsent + LangRes.ConsentYes;
                        await CopyToRoot();
                        return;
                    }
                    else
                    {
                        await DelayedClose(LangRes.CopyToRootConsent + LangRes.ConsentNo + "\n" + LangRes.WindowWillClose, 0);
                        return;
                    }
                }
                else
                {
                    PrimaryLog.Text += "\n" + LangRes.CopyToRootConsent + LangRes.ConsentIsCool;
                    await CopyToRoot();
                    return;
                }
            }
        }

        async Task StartCBPL()
        {
            await SlowDown();
            PrimaryLog.Text += "\n" + LangRes.StartCBPL;
            await SlowDown();
            FirstTimeSlow();

            if (await ProcessCheck("CBPLauncher", 0) == false)
            {
                //ask user if it's okay to run CBP Launcher [can autoconsent]
                if (Properties.Settings.Default.AutoConsent == false)
                {
                    if (MessageBox.Show(LangRes.StartCBPLConsent, LangRes.ConsentNeeded, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        PrimaryLog.Text += "\n" + LangRes.StartCBPLConsent + LangRes.ConsentYes;
                        await StartCBPLProcess();
                        return;
                    }
                    else
                    {
                        await DelayedClose(LangRes.StartCBPLConsent + LangRes.ConsentNo + "\n" + LangRes.WindowWillClose, 0);
                        return;
                    }
                }
                else
                {
                    PrimaryLog.Text += "\n" + LangRes.StartCBPLConsent + LangRes.ConsentIsCool;
                    await StartCBPLProcess();
                    return;
                }
            }
            else
            {
                await DelayedClose(LangRes.StartCBPLAlreadyRunning + "\n" + LangRes.WindowWillClose, -1);
                return;
            }

        }

        async Task Conclusion()
        {
            await Delay(600); //wait too long and it could give a false negative on fast system (crash/close); too short and you get a false negative on a slow system (still loading)
            if (await ProcessCheck("CBPLauncher", 0) == false)
            {
                // second try, reduce false negatives for slower systems (or just random OS hitches)
                await Delay(3200);
                if (await ProcessCheck("CBPLauncher", 0) == false)
                {
                    MessageBox.Show(LangRes.StartCBPLFail);
                    await DelayedClose(LangRes.StartCBPLFail + "\n" + LangRes.WindowWillClose, -1);
                    return;
                }
                //no need for the slowdown since the delay is already long
                await DelayedClose(LangRes.StartCBPLSuccess + "\n" + LangRes.WindowWillClose, 0);
                return;
            }
            else
            {
                await SlowDown();
                await DelayedClose(LangRes.StartCBPLSuccess + "\n" + LangRes.WindowWillClose, 0);
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
            if (Properties.Settings.Default.SlowMode == true || Properties.Settings.Default.FirstTimeRun == true)
            {
                await Delay(2250);
            }
        }

        async Task<bool> ProcessCheck(string processName, int qty) //not really anything to run async here
        {
            return Process.GetProcessesByName(processName).Length > qty;
        }

        async Task UpdateCBPL()
        {
            try
            {
                // instead of deleting the old files, rename them (so that if the copy fails we haven't lost the originals)
                File.Move(CBPLExe, Path.Combine(CBPLExe + "old"));
                File.Copy(CBPLExeUpdate, CBPLExe);
            }
            catch (Exception ex)
            {
                await SlowDown();
                try
                {
                    PrimaryLog.Text += "\n" + LangRes.OldVersionRestore;
                    File.Move(Path.Combine(CBPLExe + "old"), CBPLExe);
                    ///File.Move(Path.Combine(CBPLDll + "old"), CBPLDll);
                }
                catch (Exception ex2)
                {
                    MessageBox.Show(LangRes.OldVersionRestoreError);
                    PrimaryLog.Text += LangRes.OldVersionRestoreError + "\n" + ex2;
                }

                if (ex is UnauthorizedAccessException)
                {
                    MessageBox.Show(LangRes.ErrorPermissions);
                    await DelayedClose(LangRes.ErrorPermissions + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
                else if (ex is FileNotFoundException)
                {
                    MessageBox.Show(LangRes.ErrorFileNotFound);
                    await DelayedClose(LangRes.ErrorFileNotFound + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
                else if (ex is IOException)
                {
                    MessageBox.Show(LangRes.ErrorIO);
                    await DelayedClose(LangRes.ErrorIO + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
                else
                {
                    MessageBox.Show(LangRes.ErrorUnknown);
                    await DelayedClose(LangRes.ErrorUnknown + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
            }
            PrimaryLog.Text += "\n" + LangRes.DeletingFiles;

            try
            {
                //if copy is successful, don't need the old versions anymore
                File.Delete(Path.Combine(CBPLExe + "old"));
                await SlowDown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LangRes.DeletingFilesError);
                await DelayedClose(LangRes.DeletingFilesError + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
            }
        }

        async Task StartCBPLProcess()
        {
            try
            {
                // I'm not actually sure if this whole shebang is necessary just to start it, but I've done it anyway
                ProcessStartInfo PSI = new ProcessStartInfo(CBPLExe)
                {
                    WorkingDirectory = Path.GetDirectoryName(CBPLExe)
                };
                Process.Start(PSI);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LangRes.StartCBPLProblem);
                await DelayedClose(LangRes.StartCBPLProblem + "\n" + ex, -1);
            }
        }

        async Task CopyToRoot()
        {
            try
            {
                File.Copy(CBPLExeUpdate, CBPLExe);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LangRes.CopyToRootError);
                await DelayedClose(LangRes.CopyToRootError + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                return;
            }
        }

        private static void OverrideLanguageToEnglish()
        {
            if (Properties.Settings.Default.EnglishOverride == true)
            {
                CultureInfo culture = CultureInfo.CreateSpecificCulture("en");
                CultureInfo.DefaultThreadCurrentCulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                SetLanguageDictionary();
            }
        }

        private static void FirstTimeSlow()
        {
            if (Properties.Settings.Default.FirstTimeRun == true)
            {
                Properties.Settings.Default.FirstTimeRun = false;
                Properties.Settings.Default.NeedAskAutoConsent = true;
                SaveSettings();
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
        private void EnglishCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.EnglishOverride = true;
            SaveSettings();
            OverrideLanguageToEnglish();
        }

        private void EnglishCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.EnglishOverride = false;
            SaveSettings();
        }

        private void AutoConsentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoConsent = true;
            SaveSettings();
            OverrideLanguageToEnglish();
        }

        private void AutoConsentCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoConsent = false;
            SaveSettings();
        }

        private static void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private static void UpgradeSettings()
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
            else
            {
                SlowModeCheckBox.IsChecked = false;
            }
            if (Properties.Settings.Default.EnglishOverride == true)
            {
                EnglishCheckBox.IsChecked = true;
            }
            else
            {
                EnglishCheckBox.IsChecked = false;
            }
            if (Properties.Settings.Default.AutoConsent == true)
            {
                AutoConsentCheckBox.IsChecked = true;
            }
            else
            {
                AutoConsentCheckBox.IsChecked = false;
            }
        }

        // toggle between the font size controls and the reset button being visible in the bottom right of main window
        private void HandleKeyPressF9(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9 && Properties.Settings.Default.FontSizeVisible == true)
            {
                Properties.Settings.Default.FontSizeVisible = false;
                FontSizeGrid.Visibility = Visibility.Hidden;
                FontSizeAltGrid.Visibility = Visibility.Visible;
                SaveSettings();
            }
            else if (e.Key == Key.F9 && Properties.Settings.Default.FontSizeVisible == false)
            {
                Properties.Settings.Default.FontSizeVisible = true;
                FontSizeGrid.Visibility = Visibility.Visible;
                FontSizeAltGrid.Visibility = Visibility.Hidden;
                SaveSettings();
            }
        }

        // not sure if automatically increasing the window size is a good idea or not

        private void FontSizeIncreasePress(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.FontSize += 2;
            ///if (Properties.Settings.Default.Height <= 1500 && Properties.Settings.Default.Width <= 2250)
            ///{
            ///    Properties.Settings.Default.Height += 30;
            ///    Properties.Settings.Default.Width += 44;
            ///}
            SaveSettings();
        }

        private void FontSizeDecreasePress(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.FontSize -= 2;
            ///if (Properties.Settings.Default.Height >= 120 && Properties.Settings.Default.Width >= 200)
            ///{
            ///    Properties.Settings.Default.Height -= 30;
            ///    Properties.Settings.Default.Width -= 44;
            ///}
            SaveSettings();
        }

        private void ResetPressed(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.Reset();
            ReplacementSettingsReset();

            // for some reason height doesn't reset properly by default?
            //Properties.Settings.Default.Height = 420;
            //Properties.Settings.Default.Width = 600;
            //update: might be because the reset didn't work as expected since I wasn't distributing the myApp.exe.config (app.config) file :(

            // neither does the grid height????
            SaveSettings();

            MessageBox.Show(LangRes.SettingResetSuccess);
        }

        private void ReplacementSettingsReset()
        {
            Properties.Settings.Default.UpgradeRequired = true;
            Properties.Settings.Default.SlowMode = false;
            Properties.Settings.Default.EnglishOverride = false;
            Properties.Settings.Default.FirstTimeRun = true;
            Properties.Settings.Default.AutoConsent = false;
            Properties.Settings.Default.FontSizeVisible = false;
            Properties.Settings.Default.FontSize = 12;
            Properties.Settings.Default.Height = 420;
            Properties.Settings.Default.Width = 640;
            Properties.Settings.Default.NeedAskAutoConsent = false;

            SaveSettings();
        }
    }
}
