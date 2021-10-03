using Microsoft.VisualBasic;//frustratingly, using VB seems to be the easiest not-actually-bad way of having a messagebox with input
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static CBPLauncher.Logic.RegistryLogic;
using static CBPLauncher.Logic.SettingsLogic;

namespace CBPLauncher.Logic
{
    public class GetPathsLogic
    {
        /*///
        /// private strings
        /// 

        // core strings
        private string rootPath;
        private static string gameZip;
        private static string gameExe;
        private static string localMods;
        private static string gamePathFinal = Properties.Settings.Default.RoNPathSetting; // is it possible there's a narrow af edge case where the path ends up wrong after a launcher version upgrade?
        private string gamePathCheck;
        private string workshopPath;
        private static string unloadedModsPath;

        // new strings for alpha 7+
        private static readonly string helpXML = "help.xml";
        private static readonly string interfaceXML = "interface.xml";
        private static readonly string setupwinXML = "setupwin.xml";

        private static string helpXMLOrig = "";
        private static string interfaceXMLOrig = "";
        private static string setupwinXMLOrig = "";

        private static string patriotsOrig = "";

        // CBP strings (ideally move to plugin/external format and use CBP Launcher as a mod manager (with CBP as a bundled plugin) in future)
        private static string modnameCBP;
        private static string workshopIDCBP;
        private static string workshopPathCBP;
        private static string localPathCBP;
        private static string versionFileCBP;
        private static string archiveCBP;

        ///
        /// public strings
        ///
        /// ``public string Name => name;`` is the same as ``public string Name { get { return name; } }`` - just different syntax

        // core strings
        public string RootPath => rootPath;
        public static string GameZip => gameZip;
        public static string GameExe => gameExe;
        public static string LocalMods => localMods;
        public static string GamePathFinal => gamePathFinal;
        public string GamePathCheck => gamePathCheck;
        public string WorkshopPath => workshopPath;
        public static string UnloadedModsPath => unloadedModsPath;

        // new strings for alpha 7+
        public static string HelpXML => helpXML;
        public static string InterfaceXML => interfaceXML;
        public static string SetupwinXML => setupwinXML;

        public static string HelpXMLOrig => helpXMLOrig;
        public static string InterfaceXMLOrig => interfaceXMLOrig;
        public static string SetupwinXMLOrig => setupwinXMLOrig;

        public static string PatriotsOrig => patriotsOrig;

        // CBP strings (ideally move to plugin/external format and use CBP Launcher as a mod manager (with CBP as a bundled plugin) in future)
        public static string ModnameCBP => modnameCBP;
        public static string WorkshopIDCBP => workshopIDCBP;
        public static string WorkshopPathCBP => workshopPathCBP;
        public static string LocalPathCBP => localPathCBP;
        public static string VersionFileCBP => versionFileCBP;
        public static string ArchiveCBP => archiveCBP;

        public GetPathsLogic()
        {
            // stuff that happens automatically goes here

            FindRoNPath();
        }



        ///
        /// publicly accessible stuff so that outside can interact with model
        ///

        ///
        /// additional private functions (e.g. those used by public functions)
        ///

        public void FindRoNPath()
        {
            try
            {
                AssignZipPath();

                // this starts a cycle through each of the automatic find-path attempts - if all fail, it just prompts user to input the path into a popup box instead
                FindPathAuto1();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating paths" + ex);
                Environment.Exit(0);
            }

            try
            {
                AssignPaths();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error assigning paths" + ex);
                Environment.Exit(0);
            }

            try
            {
                CreateFoldersCBP();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directories {ex}");
                Environment.Exit(0);
            }
            //these are for debugging, but they ABSOLUTELY BREAK VISUAL STUDIO IN SOME CIRCUMSTANCES (SUCH AS ACCESSING THE HEADER OF APP.XML)
			//USE WITH CARE!??!?!?
			//MessageBox.Show("rootPath: " + rootPath);
            //MessageBox.Show("gamePathFinal: " + gamePathFinal);
        }

        private void AssignZipPath()
        {
            rootPath = Directory.GetCurrentDirectory();
            gameZip = Path.Combine(rootPath, "Community Balance Patch.zip"); //static file name even with updates, otherwise you have to change this value!
        }

        private void FindPathAuto1()
        {
            if (Properties.Settings.Default.RoNPathSetting == "no path") //RoNPathSetting is specifically set to this by default (not null), so this will always trigger on first-time-run
            {
                //I'm unsure if registry in non-English will actually have this exact path - do the names change per language?
                using (RegistryKey ronReg = RegPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450"))
                {
                    // some RoN:EE installs (for some UNGODLY REASON WHICH I DON'T UNDERSTAND) don't have their location in the registry (at all), so we have to work around that with the other attempts
                    if (ronReg != null)
                    {
                        // success: automated primary
                        gamePathCheck = RegPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450").GetValue("InstallLocation").ToString();
                        RoNPathFound();
                    }
                    else
                    {
                        FindPathAuto2();
                    }
                }
            }
            else
            {
                //alpha 7+
                helpXMLOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "Data", helpXML));
                interfaceXMLOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "Data", interfaceXML));
                setupwinXMLOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "Data", setupwinXML));
                patriotsOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "patriots.exe"));
            }
        }

        private void FindPathAuto2()
        {
            // try a default 64-bit install path, since that should probably work for most of the users with cursed registries
            gamePathCheck = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\steamapps\common\Rise of Nations";

            if (File.Exists(Path.Combine(gamePathCheck, "riseofnations.exe")))
            {
                // success: automated secondary 1
                RoNPathFound();
            }
            else
            {
                FindPath3Auto();
            }
        }

        private void FindPath3Auto()
        {
            gamePathCheck = @"C:\Program Files (x86)\Steam\steamapps\common\Rise of Nations";

            if (File.Exists(Path.Combine(gamePathCheck, "riseofnations.exe")))
            {
                // success: automated secondary 2
                RoNPathFound();
            }

            // automated methods unable to locate RoN install path - ask user for path
            else
            {
                FindPathManual();
            }
        }

        private void FindPathManual()
        {
            //people hate gotos (less so in C# but still) but this seems like a very reasonable substitute for a while-not-true loop that I haven't figured out how to implement here
            AskManualPath:

            gamePathCheck = Interaction.InputBox($"Please provide the file path to the folder where Rise of Nations: Extended Edition is installed."
                                                + "\n\n" + @"e.g. D:\Steamgames\common\Rise of Nations", "Unable to detect RoN install");

            // check that the user has input a seemingly valid location
            if (File.Exists(Path.Combine(gamePathCheck, "riseofnations.exe")))
            {
                // success: manual path
                RoNPathFound();
            }
            else
            {
                // tell user invalid path, ask if they want to try again
                string message = $"Rise of Nations install not detected in that location. "
                               + "The path needs to be the folder that riseofnations.exe is located in, not including the executable itself."
                               + "\n\n Would you like to try entering a path again?";

                if (MessageBox.Show(message, "Invalid Path", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    goto AskManualPath;
                }
                else
                {
                    MessageBox.Show("Launcher will now close.");
                    Environment.Exit(0);
                }
            }
        }

        private void AssignPaths()
        {
            gameExe = Path.Combine(gamePathFinal, "riseofnations.exe"); //in EE v1.20 this is the main game exe, with patriots.exe as the launcher (in T&P main game was rise.exe)
            localMods = Path.Combine(gamePathFinal, "mods");
            workshopPath = Path.GetFullPath(Path.Combine(gamePathFinal, @"..\..", @"workshop\content\287450")); //maybe not the best method, but serviceable? Path.GetFullPath is used to make final path more human-readable

            /// ===== START OF MOD LIST =====

            // Community Balance Patch
            modnameCBP = "Community Balance Patch"; // this has to be static, which loses the benefit of having the version display in in-game mod manager, but acceptable because it will display in CBP Launcher instead

            // for testing purposes, access pre-elease (of a7)
            if (Properties.Settings.Default.UsePrerelease == true)
            {
                workshopIDCBP = "2528425253"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)
            }
            else
            {
                workshopIDCBP = "2287791153"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)
            }
            //workshopIDCBP = "2287791153"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)

            workshopPathCBP = Path.Combine(Path.GetFullPath(workshopPath), workshopIDCBP); /// getfullpath ensures the slash is included between the two
            localPathCBP = Path.Combine(Path.GetFullPath(localMods), modnameCBP);          /// I tried @"\" and "\\" and both made the first part (localMods) get ignored in the combined path
            versionFileCBP = Path.Combine(localPathCBP, "Version.txt"); // moved here in order to move with the data files (useful), and better structure to support other mods in future

            /// TODO
            /// use File.Exists and/or Directory.Exists to confirm that CBP files have actually downloaded from Workshop
            /// (at the moment it just assumes they exist and eventually errors later on if they don't)
            /// 
            /// I think it might also be *technically possible but extremely unlikely in the wild* that someone might have completely different paths to RoN and RoN's workshop mods


            // Example New Mod
            // modname<MOD> = A
            // workshopID<MOD> = B

            // workshopPath<MOD> = X
            // localPath<MOD> = Y // remember to declare (?) these new strings at the top ("private string = ") as well
            // versionFile<MOD> = Path.Combine(localPath<MOD>, "Version.txt"

            /// Ideally in future these values can be dynamically loaded/added/edited etc from
            /// within the program's UI (using e.g. external files), meaning that these values no longer need to be hardcoded like this.
            /// I guess it might involve a Steam Workshop api call/request(?) for a string the user inputs (e.g. "Community Balance Patch")
            /// and then it comes back with the ID and description so that the user can decide if that's the one they want.
            /// If it is, then its details are populated into new strings within CBP Launcher.
            /// No idea how to actually do that yet though :HnZdead:
            /// And even if I did, I suspect it might be more work than it's worth - very few people seem to care enough to put in the effort on this
            /// so it's probably not viable for *me* to put in the effort to make the program go from an 8/10 to a 10/10 - 
            /// it sure would be nice to avoid half-duplicating these here though.
            /// 
            /// Also this list should probably be moved to a different file if implemented so it doesn't clog this thing up once it supports more mods

            /// ===== END OF MOD LIST =====
        }

        private void CreateFoldersCBP()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(localMods, "Unloaded Mods")); // will be used to unload CBP
                unloadedModsPath = Path.Combine(localMods, "Unloaded Mods");

                Directory.CreateDirectory(Path.Combine(unloadedModsPath, "CBP Archive")); // will be used for archiving old CBP versions
                archiveCBP = Path.Combine(unloadedModsPath, "CBP Archive");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directories {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }
        }

        private void RoNPathFound()
        {
            if (gamePathFinal == $"no path")
            {
                MessageBox.Show($"Rise of Nations detected in " + gamePathCheck);
            }
            gamePathFinal = gamePathCheck;

            Properties.Settings.Default.RoNPathSetting = gamePathFinal;
            SaveSettings();

            //alpha 7+
            helpXMLOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "Data", helpXML));
            interfaceXMLOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "Data", interfaceXML));
            setupwinXMLOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "Data", setupwinXML));
            patriotsOrig = Path.GetFullPath(Path.Combine(gamePathFinal, "patriots.exe"));
        }*/
    }
}
