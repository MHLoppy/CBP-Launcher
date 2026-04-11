using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LangRes = CBPSetup.Language.Resources; // ease-of-use alias (apparently no performance overhead / penalty to do this)

namespace CBPSetup
{
    class Program
    {
        static long swInit;
        static long swUpgrade;
        static long swDefault;
        static long swLang;
        static long swRunning;
        static long swLocation;
        static long swFound;
        static long swTxt;
        static long swUpdated;
        static long swLaunch;
        static long swConclusion;

        private enum RunningLocation
        {
            Unknown                = 0,
            RonRoot                = 1,
            WorkshopMods           = 2,
            LocalMods              = 3,
            WorkshopModsPreRelease = 4
        }

        public static string TextLog = "";
        public static string[] Args;
        private static bool cbpLauncherIsRunning = false;

        private static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            Args = args;
            sw.Stop();
            swInit = sw.ElapsedMilliseconds;

            foreach (string arg in args)
            {
                TextLog += arg;
            }
            foreach (string arg in Args)
            {
                TextLog += arg;
            }

            sw.Restart();
            if (Properties.Settings.Default.UpgradeRequired == true)
            {
                ReplacementSettingsReset();
                UpgradeSettings();
                SaveSettings();
            }
            sw.Stop();
            swUpgrade = sw.ElapsedMilliseconds;

            sw.Restart();
            sw.Stop();
            swDefault = sw.ElapsedMilliseconds;

            Primary();
        }

        // CBP Setup handles updating CBP Launcher (and its language files); CBPL handles updating CBPS (and its language files)
        private static string CbpLauncherLocalExePath = "";
        private static string CbpLauncherWorkshopExePath = "";

        //private static bool CBPPR = false;//used for debugging
        //private static string netFrameworkVersion => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        static private void Primary()
        {
            //step -2: check .NET framework version
            //MessageBox.Show(netFrameworkVersion);

            var sw = Stopwatch.StartNew();

            sw.Stop();
            swLang = sw.ElapsedMilliseconds;
            sw.Restart();
            CheckIfAlreadyRunning();
            sw.Stop();
            swRunning = sw.ElapsedMilliseconds;
            sw.Restart();

            // Step 1: figure out what location exe is running from
            RunningLocation runningLocation = FindRunningLocation();
            sw.Stop();
            swLocation = sw.ElapsedMilliseconds;
            sw.Restart();

            //Step 2: does CBP launcher exist? (if no, say error, if yes continue)
            bool found = CbpLauncherFound(runningLocation);
            sw.Stop();
            swFound = sw.ElapsedMilliseconds;
            sw.Restart();
            //await AutoConsentQuestion();

            //Step 3: is it up to date? if yes continue, if no, update it and continue (if error updating, say error)
            CopyTxtFiles();
            sw.Stop();
            swTxt = sw.ElapsedMilliseconds;
            sw.Restart();
            KeepCbpLauncherUpdated(found);
            sw.Stop();
            swUpdated = sw.ElapsedMilliseconds;
            sw.Restart();

            // Step 4: launch CBP launcher
            StartCbpLauncher();
            sw.Stop();
            swLaunch = sw.ElapsedMilliseconds;
            sw.Restart();

            //CBPS exits if CBP Launcher is running
            Conclusion();
            sw.Stop();
            swConclusion = sw.ElapsedMilliseconds;
        }

        private static void CheckIfAlreadyRunning()
        {
            // longwinded way of checking if another copy of the process is already running; mutex would be better but slightly more complex
            string thisProcessName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location); //"CBP Setup"
            if (HasMoreThanNumProcesses(thisProcessName, 1) == true)
            {
                ControlledClose(LangRes.ErrorAlreadyRunning + "\n" + LangRes.WindowWillClose, 1056);
                return;
            }

            // safeguard against there being *multiple* CBP Launcher instances running
            cbpLauncherIsRunning = HasMoreThanNumProcesses("CBPLauncher", 1);
            if (cbpLauncherIsRunning)
            {
                ControlledClose(LangRes.CBPLCurrentlyRunning + "\n" + LangRes.WindowWillClose, 1056);
                return;
            }
        }

        private static RunningLocation FindRunningLocation()
        {
            string thisProcessLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (File.Exists(Path.Combine(thisProcessLocation, "riseofnations.exe")))
            {
                return RunningLocation.RonRoot;
            }

            var parentDirectoryName = new DirectoryInfo(thisProcessLocation).Parent?.Name; // null guard seems good practice, though normally unnecessary here
            if (parentDirectoryName == "2287791153")
            {
                return RunningLocation.WorkshopMods;
            }
            if (parentDirectoryName == "2528425253")
            {
                return RunningLocation.WorkshopModsPreRelease;
            }

            if (File.Exists(Path.Combine(thisProcessLocation, @"..\", "mod-status.txt"))) // (this location is currently unsupported)
            {
                return RunningLocation.LocalMods;
            }

            return RunningLocation.Unknown;
        }

        // condenses multiple steps into one; slightly harder to read but easier to make for me /shrug
        // just remember that each of the paths are heavily duplicated (but I don't think it's worth the trouble of making more sophisticated logic to avoid it right now)
        private static bool CbpLauncherFound(RunningLocation location)
        {
            string thisProcessLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            switch (location)
            {
                case RunningLocation.Unknown:

                    ControlledClose(LangRes.LocationCase0 + "\n" + LangRes.WindowWillClose, 3);
                    break;

                case RunningLocation.RonRoot:

                    TextLog += "\n" + LangRes.LocationCase1;

                    try
                    {
                        CbpLauncherLocalExePath = Path.Combine(thisProcessLocation, "CBPLauncher.exe");
                        CbpLauncherWorkshopExePath = Path.Combine(thisProcessLocation, @"..\..", @"workshop\content\287450\2287791153", "CBPLauncher.exe");
                    }
                    catch (Exception ex)
                    {
                        ControlledClose(LangRes.LocationPathError + "\n" + ex + "\n" + LangRes.WindowWillClose, 3);
                    }

                    if (File.Exists(Path.Combine(thisProcessLocation, "CBPLauncher.exe")))
                    {
                        TextLog += "\n" + LangRes.FoundRootYes;
                        return true;
                    }
                    else
                    {
                        TextLog += "\n" + LangRes.FoundRootNo;
                        return false;
                    }

                case RunningLocation _ when (location == RunningLocation.WorkshopMods || location == RunningLocation.WorkshopModsPreRelease):
                    try
                    {
                        // because CBP Setup is running from each respective mod folder, the launcher/dll are automatically going to be in the same *relative* location both on normal and pre-release versions
                        CbpLauncherLocalExePath = Path.Combine(thisProcessLocation, @"..\..\..\..", @"common\Rise of Nations", "CBPLauncher.exe");
                        CbpLauncherWorkshopExePath = Path.Combine(thisProcessLocation, "CBPLauncher.exe");
                    }

                    catch (Exception ex)
                    {
                        ControlledClose(LangRes.LocationPathError + "\n" + ex + "\n" + LangRes.WindowWillClose, 3);
                    }

                    if (location == RunningLocation.WorkshopMods)
                    {
                        TextLog += "\n" + LangRes.LocationCase2;
                    }
                    else if (location == RunningLocation.WorkshopModsPreRelease)
                    {
                        TextLog += "\n" + LangRes.LocationCase4;
                    }

                    if (File.Exists(Path.Combine(thisProcessLocation, @"..\..\..\..", @"common\Rise of Nations", "CBPLauncher.exe")))
                    {
                        TextLog += "\n" + LangRes.FoundRootYes;
                        return true;
                    }
                    else
                    {
                        TextLog += "\n" + LangRes.FoundRootNo;
                        return false;
                    }

                case RunningLocation.LocalMods:
                default:
                    ControlledClose(LangRes.LocationCaseDefault + "\n" + LangRes.WindowWillClose, -1);
                    break;
            }
            return false; // putting this in cases 0/3/default should be enough, but it's *not*
        }

        private static void CopyTxtFiles()
        {
            try
            {
                string workshopCbpRootFolder = Path.GetDirectoryName(CbpLauncherWorkshopExePath);
                //string workshopCbpLatestFolder = Path.Combine(workshopCbpRootFolder, "Community Balance Patch");
                string workshopAnnouncementsTxt = Path.Combine(workshopCbpRootFolder, "announcements.txt");
                string workshopOldAnnouncementsTxt = Path.Combine(workshopCbpRootFolder, "old_announcements.txt");
                string workshopPatchnotesTxt = Path.Combine(workshopCbpRootFolder, "patchnotes.txt");

                string localRonRootFolder = Path.GetDirectoryName(CbpLauncherLocalExePath);
                string localCbpFolder = Path.Combine(localRonRootFolder, "CBP");
                string localAnnouncementsTxt = Path.Combine(localCbpFolder, "announcements.txt");
                string localOldAnnouncementsTxt = Path.Combine(localCbpFolder, "old_announcements.txt");
                string localPatchnotesTxt = Path.Combine(localCbpFolder, "patchnotes.txt");

                // create the CBP folder before copying into it (does nothing if the folder already exists)
                Directory.CreateDirectory(localCbpFolder);
                File.Copy(workshopAnnouncementsTxt, localAnnouncementsTxt, true);
                File.Copy(workshopPatchnotesTxt, localPatchnotesTxt, true);
                File.Copy(workshopOldAnnouncementsTxt, localOldAnnouncementsTxt, true);
            }
            catch (Exception ex)
            {
                ControlledClose(LangRes.ErrorUnknown + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
            }
        }

        private static void KeepCbpLauncherUpdated(bool launcherFound)
        {
            if (launcherFound)
            {
                try
                {
                    //https://stackoverflow.com/questions/11350008/how-to-get-exe-file-version-number-from-file-path/23325102#23325102
                    var newVersionShort = FileVersionInfo.GetVersionInfo(CbpLauncherWorkshopExePath);
                    string newVersionFull = newVersionShort.FileVersion;

                    var oldVersionShort = FileVersionInfo.GetVersionInfo(CbpLauncherLocalExePath);
                    string oldVersionFull = oldVersionShort.FileVersion;

                    if (newVersionFull == oldVersionFull)
                    {
                        TextLog += "\n" + LangRes.VersionCheckSame;
                        return;
                    }
                    else
                    {
                        TextLog += "\n" + LangRes.VersionCheckDifferent + LangRes.ConsentIsCool;
                        UpdateCbpLauncher();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ControlledClose(LangRes.VersionCheckFail + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                    return;
                }
            }
            else
            {
                TextLog += "\n" + LangRes.CopyToRootConsent + LangRes.ConsentIsCool;
                CopyToRoot();
                return;
            }
        }

        private static void StartCbpLauncher()
        {
            TextLog += "\n" + LangRes.StartCBPL;
            FirstTimeSlow();

            if (cbpLauncherIsRunning == false)
            {
                TextLog += "\n" + LangRes.StartCBPLConsent + LangRes.ConsentIsCool;
                StartCBPLProcess();
                return;
            }
            else
            {
                ControlledClose(LangRes.StartCBPLAlreadyRunning + "\n" + LangRes.WindowWillClose, -1);
                return;
            }
        }

        private static void Conclusion()
        {
            int maxAttempts = 30;
            int delayMs = 60;
            bool launcherRunning = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                if (HasMoreThanNumProcesses("CBPLauncher", 0) == true)
                {
                    launcherRunning = true;
                    break;
                }
                Delay(delayMs);
                delayMs += 20;
            }

            if (launcherRunning)
            {
                ControlledClose(LangRes.StartCBPLSuccess + "\n" + LangRes.WindowWillClose, 0);
            }
            else
            {
                ControlledClose(LangRes.StartCBPLFail + "\n" + LangRes.WindowWillClose, -1);
            }
        }

        private static void ControlledClose(string str, int code)
        {
            TextLog += "\n" + str;

            string message = $"\ninit: {swInit}"
                            + $"\nupgrade: {swUpgrade}"
                            + $"\ndefault: {swDefault}"
                            + $"\nlanguage: {swLang}"
                            + $"\nrunning: {swRunning}"
                            + $"\nlocation: {swLocation}"
                            + $"\nfound: {swFound}"
                            + $"\ntxt: {swTxt}"
                            + $"\nupdated: {swUpdated}"
                            + $"\nlaunch: {swLaunch}"
                            + $"\nconclusion: {swConclusion}";
            //MessageBox.Show(message);//STOPWATCH

            TextLog += message;

            string logPath = "CBPSetup_log.txt";
            RunningLocation runLoc = FindRunningLocation();
            if (runLoc == RunningLocation.RonRoot)
            {
                var here = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var hereParent = new DirectoryInfo(here).Parent?.Name;
                logPath = Path.Combine(hereParent, "CBP", "logs", logPath);
            }
            File.WriteAllText(logPath, TextLog);

            Environment.Exit(code);
        }

        private static void Delay(int ms)
        {
            Thread.Sleep(ms);
        }

        private static bool HasMoreThanNumProcesses(string processName, int qty)
        {
            return Process.GetProcessesByName(processName).Length > qty;
        }

        private static void UpdateCbpLauncher()
        {
            try
            {
                // instead of deleting the old files, rename them (so that if the copy fails we haven't lost the originals)
                File.Move(CbpLauncherLocalExePath, Path.Combine(CbpLauncherLocalExePath, "old"));
                File.Copy(CbpLauncherWorkshopExePath, CbpLauncherLocalExePath);
            }
            catch (Exception ex)
            {
                try
                {
                    TextLog += "\n" + LangRes.OldVersionRestore;
                    File.Move(Path.Combine(CbpLauncherLocalExePath, "old"), CbpLauncherLocalExePath);
                    ///File.Move(Path.Combine(CBPLDll + "old"), CBPLDll);
                }
                catch (Exception ex2)
                {
                    TextLog += LangRes.OldVersionRestoreError + "\n" + ex2;
                }

                if (ex is UnauthorizedAccessException)
                {
                    ControlledClose(LangRes.ErrorPermissions + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
                else if (ex is FileNotFoundException)
                {
                    ControlledClose(LangRes.ErrorFileNotFound + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
                else if (ex is IOException)
                {
                    ControlledClose(LangRes.ErrorIO + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
                else
                {
                    ControlledClose(LangRes.ErrorUnknown + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                }
            }
            TextLog += "\n" + LangRes.DeletingFiles;

            try
            {
                // if copy is successful, don't need the old versions anymore
                File.Delete(Path.Combine(CbpLauncherLocalExePath, "old"));
            }
            catch (Exception ex)
            {
                ControlledClose(LangRes.DeletingFilesError + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
            }
        }

        private static void StartCBPLProcess()
        {
            try
            {
                string combinedArgs = string.Join(" ", Args);
                string escapedArgs = combinedArgs.Replace("\"", "\\\"");
                string processedArgs = "\"" + escapedArgs + "\"";

                TextLog += $"\nArgs: {processedArgs}";
                TextLog += $"\nLauncher path: {CbpLauncherLocalExePath}";

                ProcessStartInfo PSI = new ProcessStartInfo(CbpLauncherLocalExePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(CbpLauncherLocalExePath),
                    Arguments = processedArgs
                };
                Process.Start(PSI);
            }
            catch (Exception ex)
            {
                ControlledClose(LangRes.StartCBPLProblem + "\n" + ex, -1);
            }
        }

        private static void CopyToRoot()
        {
            try
            {
                File.Copy(CbpLauncherWorkshopExePath, CbpLauncherLocalExePath);
            }
            catch (Exception ex)
            {
                ControlledClose(LangRes.CopyToRootError + "\n" + ex + "\n" + LangRes.WindowWillClose, -1);
                return;
            }
        }

        private static void FirstTimeSlow()
        {
            if (Properties.Settings.Default.FirstTimeRun == true)
            {
                Properties.Settings.Default.FirstTimeRun = false;
                SaveSettings();
            }
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

        private static void ReplacementSettingsReset()
        {
            Properties.Settings.Default.UpgradeRequired = true;
            Properties.Settings.Default.SlowMode = false;
            Properties.Settings.Default.EnglishOverride = false;
            Properties.Settings.Default.FirstTimeRun = true;
            Properties.Settings.Default.AutoConsent = true;
            Properties.Settings.Default.FontSizeVisible = false;
            Properties.Settings.Default.FontSize = 14;
            Properties.Settings.Default.Height = 420;
            Properties.Settings.Default.Width = 640;
            Properties.Settings.Default.NeedAskAutoConsent = false;

            SaveSettings();
        }
    }
}
