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
        private static readonly string CBPSExe = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Setup.exe"));

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            PrimaryLog.Text += CBPSetupGUI.Language.Resources.StartupLanguageDetected.ToString();
            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.StartupMessage.ToString();

            // Step 0: don't overlap the streams (check if already running)
            MasculinityCheck();

            // Step 1: figure out what location exe is running from
            WhereTheBloodyHellAreYou();

            //Step 2: does CBP launcher exist? (if no, say error, if yes continue)
            CheckForCBPL();

            //Step 3: is it up to date? if yes continue, if no, update it and continue (if error updating, say error)
            CBPLVersionCheck();

            // Step 4: launch CBP launcher
            StartCBPL();

            //CBPS exits if CBP Launcher is running
            ProcessCheck("CBP Launcher");
            Conclusion();

            void MasculinityCheck()
            {
                // longwinded way of checking if another copy of the process is already running; mutex would be better but slightly more complex
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1)
                {
                    MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorAlreadyRunning);
                    DelayedClose(CBPSetupGUI.Language.Resources.ErrorAlreadyRunning, 1056);
                    return;
                }
            }

            void WhereTheBloodyHellAreYou()
            {

                if (File.Exists
                    (Path.GetFullPath
                    (Path.Combine
                        (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                        , "riseofnations.exe"))))
                {
                    //RoN root folder
                    Location = 1;
                }

                if (Path.GetFullPath
                    (Path.Combine
                        (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                        , @"..\"
                        , "2287791153")).ToString() == Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).ToString())
                {
                    //workshop mods folder
                    Location = 2;
                }

                if (File.Exists
                    (Path.GetFullPath
                    (Path.Combine
                        (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                        , @"..\"
                        , "mod-status.txt"))))
                {
                    //local mods folder
                    Location = 3;
                }

                if (Path.GetFullPath
                    (Path.Combine
                        (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                        , @"..\"
                        , "2528425253")).ToString() == Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).ToString())
                {
                    //workshop mods folder, but pre-release
                    Location = 4;
                }
            }

            // condenses multiple steps into one; slightly harder to read but easier to make for me /shrug
            // just remember that each paths are heavily duplicated (but I don't think it's worth the trouble of making more sophisticated logic to avoid it right now)
            void CheckForCBPL()
            {
                // change the path it checks based on where it thinks it is
                // pretty sure this isn't a particularly efficient way of doing this, but it shouldn't really matter
                switch (Location)
                {
                    case 0:

                        MessageBox.Show(CBPSetupGUI.Language.Resources.LocationCase0);
                        DelayedClose(CBPSetupGUI.Language.Resources.LocationCase0, 3);
                        break;

                    case 1:
                        PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase1;

                        CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.exe"));
                        CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));

                        CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBP Launcher.exe"));
                        CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBP Launcher.Language.dll"));


                        if (File.Exists
                            (Path.GetFullPath
                            (Path.Combine
                                (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                                , "CBP Launcher.exe"))))
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

                        CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.exe"));
                        CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.Language.dll"));

                        // because CBP Setup is running from each respective mod folder, the launcher/dll are automatically going to be in the same location both on normal and pre-release versions
                        CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.exe"));
                        CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));

                        if (Location == 2)
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase2;
                        }

                        if (Location == 4)
                        {
                            PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.LocationCase4;
                        }

                        if (File.Exists
                            (Path.GetFullPath
                            (Path.Combine
                                (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                                , @"..\..\..\.."
                                , @"common\Rise of Nations"
                                , "CBP Launcher.exe"))))
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

                        CBPLExe = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", "CBP Launcher.exe"));
                        CBPLDll = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..", "CBP Launcher.Language.dll"));

                        CBPLExeUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"workshop\content\287450\2287791153", "CBP Launcher.exe"));
                        CBPLDllUpdate = Path.GetFullPath(Path.Combine(CBPSFolder, @"..\..\..\..", @"workshop\content\287450\2287791153", "CBP Launcher.Language.dll"));

                        if (File.Exists
                            (Path.GetFullPath
                            (Path.Combine
                                (Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                                , @"..\.."
                                , @"CBP Launcher.exe"))))
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
                        MessageBox.Show(CBPSetupGUI.Language.Resources.LocationCaseDefault);
                        DelayedClose(CBPSetupGUI.Language.Resources.LocationCaseDefault, -1);
                        break;
                }
            }

            void CBPLVersionCheck()
            {
                if (CBPL == true)
                {
                    //https://stackoverflow.com/questions/11350008/how-to-get-exe-file-version-number-from-file-path/23325102#23325102
                    var newVersionShort = FileVersionInfo.GetVersionInfo(CBPLExe);
                    string newVersionFull = newVersionShort.FileVersion;

                    var oldVersionShort = FileVersionInfo.GetVersionInfo(CBPSExe);
                    string oldVersionFull = oldVersionShort.FileVersion;

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
                                DelayedClose(CBPSetupGUI.Language.Resources.ErrorPermissions + "\n" + ex, -1);
                            }
                            if (ex is FileNotFoundException)
                            {
                                MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorFileNotFound);
                                DelayedClose(CBPSetupGUI.Language.Resources.ErrorFileNotFound + "\n" + ex, -1);
                            }
                            if (ex is IOException)
                            {
                                MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorIO);
                                DelayedClose(CBPSetupGUI.Language.Resources.ErrorIO + "\n" + ex, -1);
                            }
                            else
                            {
                                MessageBox.Show(CBPSetupGUI.Language.Resources.ErrorUnknown);
                                DelayedClose(CBPSetupGUI.Language.Resources.ErrorUnknown + "\n" + ex, -1);
                            }
                        }
                        PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.DeletingFiles;

                        try
                        {
                            File.Delete(Path.Combine(CBPLExe + "old"));
                            File.Delete(Path.Combine(CBPLDll + "old"));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(CBPSetupGUI.Language.Resources.DeletingFilesError);
                            PrimaryLog.Text += CBPSetupGUI.Language.Resources.DeletingFilesError + "\n" + ex;
                            Environment.Exit(-1);
                        }
                    }
                }
                else if (CBPL == false)
                {
                    try
                    {
                        File.Copy(CBPLExeUpdate, CBPLExe);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(CBPSetupGUI.Language.Resources.CopyToRootError);
                        PrimaryLog.Text += CBPSetupGUI.Language.Resources.CopyToRootError + "\n" + ex;
                        Environment.Exit(-1);
                    }
                }
            }

            void StartCBPL()
            {
                PrimaryLog.Text += "\n" + CBPSetupGUI.Language.Resources.StartCBPL;

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

            bool ProcessCheck(string processName)
            {
                Thread.Sleep(5000);
                return Process.GetProcessesByName(processName).Length > 0;
            }

            void Conclusion()
            {
                if (ProcessCheck("CBP Launcher") == false)
                {
                    MessageBox.Show(CBPSetupGUI.Language.Resources.StartCBPLFail);
                    DelayedClose(CBPSetupGUI.Language.Resources.StartCBPLFail, -1);
                    return;
                }
                else
                {
                    DelayedClose(CBPSetupGUI.Language.Resources.StartCBPLSuccess, 0);
                    return;
                }
            }

            void DelayedClose(string str, int code)
            {
                PrimaryLog.Text += "\n" + str;
                Thread.Sleep(5000);
                Environment.Exit(code);
            }
        }
    }
}
