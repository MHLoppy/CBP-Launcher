using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CBPSetupGUI.Language;

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

            Console.WriteLine(CBPSetupGUI.Language.Resources.CBPSTestString.ToString());

            // Step 0: don't overlap the streams (check if already running)
            MasculinityCheck();

            // Step 1: figure out what location exe is running from
            WhereTheBloodyHellAreYou();

            //Step 2: does CBP launcher exist? (if no, say error, if yes continue)
            CheckForCBPL();

            //Step 3: is it up to date? if yes continue, if no, update it and continue (if error updating, say error)
            CBPLVersionCheck();

            // Step 4: launch CBP launcher, then close this
            StartCBPL();

            //CBPS exits if CBP Launcher is running
            ProcessCheck("CBP Launcher");
            Conclusion();
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

        private static string CBPSFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));
        private static string CBPSExe = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, "CBP Setup.exe"));

        private static void MasculinityCheck()
        {
            // longwinded way of checking if another copy of the process is already running; mutex would be better but slightly more complex
            if (Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                // there probably won't be enough time to see this (even if you're literally recording the screen)
                Console.WriteLine("It looks like CBP Setup is already running in another thread. (window will close in 5 seconds)");
                Thread.Sleep(5000);
                Environment.Exit(1056);
            }
        }

        private static void WhereTheBloodyHellAreYou()
        {

            if (File.Exists
                (System.IO.Path.GetFullPath
                (System.IO.Path.Combine
                    (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                    , "riseofnations.exe"))))
            {
                //RoN root folder
                Location = 1;
            }

            if (System.IO.Path.GetFullPath
                (System.IO.Path.Combine
                    (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                    , @"..\"
                    , "2287791153")).ToString() == System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).ToString())
            {
                //workshop mods folder
                Location = 2;
            }

            if (File.Exists
                (System.IO.Path.GetFullPath
                (System.IO.Path.Combine
                    (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                    , @"..\"
                    , "mod-status.txt"))))
            {
                //local mods folder
                Location = 3;
            }

            if (System.IO.Path.GetFullPath
                (System.IO.Path.Combine
                    (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                    , @"..\"
                    , "2528425253")).ToString() == System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).ToString())
            {
                //workshop mods folder, but pre-release
                Location = 4;
            }
        }

        // condenses multiple steps into one; slightly harder to read but easier to make for me /shrug
        // just remember that each paths are heavily duplicated (but I don't think it's worth the trouble of making more sophisticated logic to avoid it right now)
        private static void CheckForCBPL()
        {
            // change the path it checks based on where it thinks it is
            // pretty sure this isn't a particularly efficient way of doing this, but it shouldn't really matter
            switch (Location)
            {
                case 0:
                    Console.WriteLine("Unable to ascertain current location. (window will close in 5 seconds)");
                    Thread.Sleep(5000);
                    Environment.Exit(3);
                    break;

                case 1:
                    Console.WriteLine("Looks like the root RoN folder.");

                    CBPLExe = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, "CBP Launcher.exe"));
                    CBPLDll = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));

                    CBPLExeUpdate = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBP Launcher.exe"));
                    CBPLDllUpdate = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..", @"workshop\content\287450\2287791153", "CBP Launcher.Language.dll"));


                    if (File.Exists
                        (System.IO.Path.GetFullPath
                        (System.IO.Path.Combine
                            (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                            , "CBP Launcher.exe"))))
                    {
                        Console.WriteLine("Found CBP Launcher in RoN's root folder.");
                        CBPL = true;
                    }
                    else
                    {
                        Console.WriteLine("Can't find CBP Launcher in RoN's root folder.");
                        CBPL = false;
                    }
                    break;

                case int n when (Location == 2 || Location == 4)://parens just for my sake

                    CBPLExe = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.exe"));
                    CBPLDll = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..\..\..", @"common\Rise of Nations", "CBP Launcher.Language.dll"));

                    // because CBP Setup is running from each respective mod folder, the launcher/dll are automatically going to be in the same location both on normal and pre-release versions
                    CBPLExeUpdate = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, "CBP Launcher.exe"));
                    CBPLDllUpdate = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, "CBP Launcher.Language.dll"));

                    if (Location == 2)
                    {
                        Console.WriteLine("Looks like the Workshop mods folder for normal CBP.");
                    }

                    if (Location == 4)
                    {
                        Console.WriteLine("Looks like the Workshop mods folder for pre-release CBP.");
                    }

                    if (File.Exists
                        (System.IO.Path.GetFullPath
                        (System.IO.Path.Combine
                            (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                            , @"..\..\..\.."
                            , @"common\Rise of Nations"
                            , "CBP Launcher.exe"))))
                    {
                        Console.WriteLine("Found CBP Launcher in RoN's root folder.");
                        CBPL = true;
                    }
                    else
                    {
                        Console.WriteLine("Can't find CBP Launcher in RoN's root folder.");
                        CBPL = false;
                    }
                    break;

                case 3:
                    Console.WriteLine("Looks like the local mods folder (it probably shouldn't be here except for testing).");

                    CBPLExe = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..", "CBP Launcher.exe"));
                    CBPLDll = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..", "CBP Launcher.Language.dll"));

                    CBPLExeUpdate = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..\..\..", @"workshop\content\287450\2287791153", "CBP Launcher.exe"));
                    CBPLDllUpdate = System.IO.Path.GetFullPath(System.IO.Path.Combine(CBPSFolder, @"..\..\..\..", @"workshop\content\287450\2287791153", "CBP Launcher.Language.dll"));

                    if (File.Exists
                        (System.IO.Path.GetFullPath
                        (System.IO.Path.Combine
                            (System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
                            , @"..\.."
                            , @"CBP Launcher.exe"))))
                    {
                        Console.WriteLine("Found CBP Launcher in RoN's root folder.");
                        CBPL = true;
                    }
                    else
                    {
                        Console.WriteLine("Can't find CBP Launcher in RoN's root folder.");
                        CBPL = false;
                    }
                    break;

                default:
                    Console.WriteLine("Location result unexpected. (window will close in 5 seconds)");
                    Thread.Sleep(5000);
                    Environment.Exit(-1);
                    break;
            }
        }

        private static void CBPLVersionCheck()
        {
            if (CBPL == true)
            {
                Console.WriteLine("CBPLExe: " + CBPLExe);

                //https://stackoverflow.com/questions/11350008/how-to-get-exe-file-version-number-from-file-path/23325102#23325102
                var newVersionShort = FileVersionInfo.GetVersionInfo(CBPLExe);
                string newVersionFull = newVersionShort.FileVersion;

                var oldVersionShort = FileVersionInfo.GetVersionInfo(CBPSExe);
                string oldVersionFull = oldVersionShort.FileVersion;

                if (newVersionFull == oldVersionFull)
                {
                    Console.WriteLine("CBP Launcher in the RoN folder is the same as the downloaded Workshop version.");
                    return;
                }
                else
                {
                    Console.WriteLine("CBP Launcher in the RoN folder is not the same as the downloaded Workshop version. Replacing former with latter...");

                    try
                    {
                        File.Move(CBPLExe, System.IO.Path.Combine(CBPLExe + "old"));
                        File.Copy(CBPLExeUpdate, CBPLExe);

                        File.Move(CBPLDll, System.IO.Path.Combine(CBPLDll + "old"));
                        File.Copy(CBPLDllUpdate, CBPLDll);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Console.WriteLine("Trying to restore old versions...");
                            File.Move(System.IO.Path.Combine(CBPLExe + "old"), CBPLExe);
                            File.Move(System.IO.Path.Combine(CBPLDll + "old"), CBPLDll);
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine("Error restoring old versions: " + ex2);
                        }

                        if (ex is UnauthorizedAccessException)
                        {
                            Console.WriteLine("Permissions error replacing CBP Launcher with new version. Try running CBP Setup as admin instead?\n" + ex);
                            Console.ReadLine();
                            Environment.Exit(-1);
                        }
                        if (ex is FileNotFoundException)
                        {
                            Console.WriteLine("Error replacing CBP Launcher with new version - file not found.\n" + ex);
                            Console.ReadLine();
                            Environment.Exit(-1);
                        }
                        if (ex is IOException)
                        {
                            Console.WriteLine("Error replacing CBP Launcher with new version - maybe old version was not deleted or wrong path?\n" + ex);
                            Console.ReadLine();
                            Environment.Exit(-1);
                        }
                        else
                        {
                            Console.WriteLine("Unknown error replacing CBP Launcher with new version.\n" + ex);
                            Console.ReadLine();
                            Environment.Exit(-1);
                        }
                    }
                    Console.WriteLine("Looks like the file update worked fine, deleting old files...");

                    try
                    {
                        File.Delete(System.IO.Path.Combine(CBPLExe + "old"));
                        File.Delete(System.IO.Path.Combine(CBPLDll + "old"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error deleting the old files.\n" + ex);
                        Console.ReadLine();
                        Environment.Exit(-1);
                    }
                }
            }
            else if (CBPL == false)
            {
                try
                {
                    Console.WriteLine("CBPLExe: " + CBPLExe);
                    File.Copy(CBPLExeUpdate, CBPLExe);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CBPLExe: " + CBPLExe);
                    Console.WriteLine("Error copying CBP Launcher into RoN root folder\n" + ex);
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
        }

        private static void StartCBPL()
        {
            Console.WriteLine("Time to start CBP Launcher!");
            Console.WriteLine(CBPSetupGUI.Language.Resources.CBPSTestString.ToString());

            // I'm not actually sure if this whole shebang is necessary just to start it, but I've done it anyway
            ProcessStartInfo startInfo = new ProcessStartInfo(CBPLExe)
            {
                WorkingDirectory = CBPLExe + @"..\"
            };
            Process.Start(CBPLExe);
            Thread.Sleep(5000);
        }

        public static bool ProcessCheck(string processName)
        {
            return Process.GetProcessesByName(processName).Length > 0;
        }

        private static void Conclusion()
        {
            if (ProcessCheck("CBP Launcher") == false)
            {
                Console.WriteLine("It looks like CBP Launcher didn't start :(");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("CBP Launcher detected as running; job complete. Window will close in 5 seconds.");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }
        }
    }
}
