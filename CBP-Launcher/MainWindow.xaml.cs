﻿// sometimes comments will refer to a "reference [program]" which refers to https://github.com/tom-weiland/csharp-game-launcher

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Media;     // used for selecting brushes (used for coloring in e.g. textboxes)
using Microsoft.VisualBasic;    // used for the current (temporary?) popup user text input for manual path; I doubt it's efficient but it doesn't seem to be *too* resource intensive pending a replacement
using CBPLauncher.Logic;
using static CBPLauncher.Logic.BasicIOLogic;
using System.Windows.Input;
using static CBPLauncher.Logic.MainCode;

namespace CBPLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            //await MainCode.CreateAsync();
        }
    }



    /*enum LauncherStatus
    {
        gettingReady,
        readyCBPEnabled,
        readyCBPDisabled,
        loadFailed,
        unloadFailed,
        installFailed,
        installingFirstTimeLocal,
        installingUpdateLocal,
        installingFirstTimeOnline,
        installingUpdateOnline,
        connectionProblemLoaded,
        connectionProblemUnloaded,
        installProblem
    }

    public partial class MainWindow : Window
    {
        private string rootPath;
        private string gameZip;
        private string gameExe;
        private string localMods;
        private string RoNPathFinal = Properties.Settings.Default.RoNPathSetting; // is it possible there's a narrow af edge case where the path ends up wrong after a launcher version upgrade?
        private string RoNPathCheck;
        private string workshopPath;
        private string unloadedModsPath;

        //a7 temp
        private string helpXML = "help.xml";
        private string interfaceXML = "interface.xml";
        private string setupwinXML = "setupwin.xml";

        private string helpXMLOrig = "";
        private string interfaceXMLOrig = "";
        private string setupwinXMLOrig = "";

        private string patriotsOrig = "";

        /// ===== START OF MOD LIST =====

        //Community Balance Patch
        private string modnameCBP;
        private string workshopIDCBP;
        private string workshopPathCBP;
        private string localPathCBP;
        private string versionFileCBP;
        private string archiveCBP;

        /// ===== END OF MOD LIST =====

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.gettingReady:
                        StatusReadout.Text = "Initializing...";
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.Content = "Launch Game";
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.readyCBPEnabled:
                        StatusReadout.Text = "Ready: CBP enabled";
                        StatusReadout.Foreground = Brushes.LimeGreen;
                        PlayButton.Content = "Launch Game";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.readyCBPDisabled:
                        StatusReadout.Text = "Ready: CBP disabled";
                        StatusReadout.Foreground = Brushes.Orange;
                        PlayButton.Content = "Launch Game";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.loadFailed:
                        StatusReadout.Text = "Error: unable to load CBP";
                        StatusReadout.Foreground = Brushes.Red;
                        PlayButton.Content = "Retry Unload?";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.unloadFailed:
                        StatusReadout.Text = "Error: unable to unload CBP";
                        StatusReadout.Foreground = Brushes.Red;
                        PlayButton.Content = "Retry Unload?";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.installFailed:
                        StatusReadout.Text = "Error: update failed";
                        StatusReadout.Foreground = Brushes.Red;
                        PlayButton.Content = "Retry Update?";
                        PlayButton.IsEnabled = true;
                        break;
                    // I tried renaming the *Local to *Workshop and VS2019 literally did the opposite of that (by renaming what I just changed) instead of doing what it said it would wtf
                    case LauncherStatus.installingFirstTimeLocal:                       /// primary method: use workshop files;
                        StatusReadout.Text = "Installing CBP from local files...";      /// means no local-mods CBP detected
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.installingUpdateLocal:                          /// primary method: use workshop files;
                        StatusReadout.Text = "Installing update from local files...";   /// local-mods CBP detected, but out of date compared to workshop version.txt
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.installingFirstTimeOnline:                      /// backup method: use online files;
                        StatusReadout.Text = "Installing CBP from online files...";     /// means no local-mods CBP detected but can't find workshop files either
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.installingUpdateOnline:                         /// backup method: use online files; 
                        StatusReadout.Text = "Installing update from online files...";  /// local-mods CBP detected, but can't find workshop files and
                        StatusReadout.Foreground = Brushes.White;                       /// local files out of date compared to online version.txt
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.connectionProblemLoaded:
                        StatusReadout.Text = "Error: connectivity issue (CBP loaded)";
                        StatusReadout.Foreground = Brushes.OrangeRed;
                        PlayButton.Content = "Launch game";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.connectionProblemUnloaded:
                        StatusReadout.Text = "Error: connectivity issue (CBP not loaded)";
                        StatusReadout.Foreground = Brushes.OrangeRed;
                        PlayButton.Content = "Launch game";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.installProblem:
                        StatusReadout.Text = "Potential installation error";
                        StatusReadout.Foreground = Brushes.OrangeRed;
                        PlayButton.Content = "Launch game";
                        PlayButton.IsEnabled = true;
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                if (Properties.Settings.Default.UpgradeRequired == true)
                {
                    UpgradeSettings();
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during initialization: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            regPathDebug.Text = "Debug: registry read as " + RegPath;

            /// TODO
            /// use File.Exists and/or Directory.Exists to confirm that CBP files have actually downloaded from Workshop
            /// (at the moment it just assumes they exist and eventually errors later on if they don't)

            try
            {
                AssignZipPath();

                // this starts a cycle through each of the automatic find-path attempts - if all fail, it just prompts user to input the path into a popup box instead
                FindPathAuto1();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating paths (part 1): {ex}");
                Environment.Exit(0);
            }

            try
            {
                AssignPaths();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error assigning paths (path 2):" + ex);
                Environment.Exit(0);
            }

            // show detected paths in the UI
            try
            {
                EEPath.Text = RoNPathFinal;
                workshopPathDebug.Text = workshopPath;
                workshopPathCBPDebug.Text = workshopPathCBP;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying paths in UI {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            // create directories
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

            CBPDefaultChecker();
        }

        private void AssignZipPath()
        {
            // core paths
            rootPath = Directory.GetCurrentDirectory();
            gameZip = Path.Combine(rootPath, "Community Balance Patch.zip"); //static file name even with updates, otherwise you have to change this value!
        }

        private void FindPathAuto1()
        {
            if (Properties.Settings.Default.RoNPathSetting == "no path")
            {
                //I'm unsure if registry in non-English will actually have this exact path - do the names change per language?
                using (RegistryKey ronReg = RegPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450"))
                {
                    // some RoN:EE installs (for some UNGODLY REASON WHICH I DON'T UNDERSTAND) don't have their location in the registry, so we have to work around that
                    if (ronReg != null)
                    {
                        // success: automated primary
                        RoNPathCheck = RegPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450").GetValue("InstallLocation").ToString();
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
                //a7 temp
                helpXMLOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "Data", helpXML));
                interfaceXMLOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "Data", interfaceXML));
                setupwinXMLOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "Data", setupwinXML));
                patriotsOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "patriots.exe"));
            }
        }

        private void FindPathAuto2()
        {
            // try a default 64-bit install path, since that should probably work for most of the users with cursed registries
            RoNPathCheck = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\SteamApps\common\Rise of Nations";

            if (File.Exists(Path.Combine(RoNPathCheck, "riseofnations.exe")))
            {
                // success: automated secondary 1
                RoNPathFound();
            }
            else
            {
                FindPathAuto3();
            }
        }

        private void FindPathAuto3()
        {
            // old way of doing it, but used as as backup because I don't know if the environment call method ever fails or not
            RoNPathCheck = @"C:\Program Files (x86)\Steam\SteamApps\common\Rise of Nations";

            if (File.Exists(Path.Combine(RoNPathCheck, "riseofnations.exe")))
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

            RoNPathCheck = Interaction.InputBox($"Please provide the file path to the folder where Rise of Nations: Extended Edition is installed."
                                               + "\n\n" + @"e.g. D:\Steamgames\common\Rise of Nations", "Unable to detect RoN install");

            // check that the user has input a seemingly valid location
            if (File.Exists(Path.Combine(RoNPathCheck, "riseofnations.exe")))
            {
                // success: manual path
                RoNPathFound();
            }
            else
            {
                // tell user invalid path, ask if they want to try again
                string message = $"Rise of Nations install not detected in that location. "
                               + "The path needs to be the folder that riseofnations.exe is located in (but not including the executable itself in that path)."
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
            // create / find paths for RoN, Steam Workshop, and relevant mods
            gameExe = Path.Combine(RoNPathFinal, "riseofnations.exe"); //in EE v1.20 this is the main game exe, with patriots.exe as the launcher (in T&P main game was rise.exe)
            localMods = Path.Combine(RoNPathFinal, "mods");
            workshopPath = Path.GetFullPath(Path.Combine(RoNPathFinal, @"..\..", @"workshop\content\287450")); //maybe not the best method, but serviceable? Path.GetFullPath used to make final path more human-readable

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
        }

        private void CheckForUpdates()
        {
            try // without the try you can accidentally create online-only DRM whoops
            {
                VersionTextLatest.Text = "Checking latest version...";
                VersionTextInstalled.Text = "Checking installed version...";

                WebClient webClient = new WebClient();                                                               /// Moved this section from reference to here in order to display
                Version onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/version.txt")); /// latest available version as well as installed version

                if (Properties.Settings.Default.UsePrerelease == true)//pr
                {
                    onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/versionpr.txt")); /// latest available version as well as installed version
                }

                VersionTextLatest.Text = "Latest CBP version: "
                     + VersionArray.versionStart[onlineVersion.major]
                     + VersionArray.versionMiddle[onlineVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                     + VersionArray.versionEnd[onlineVersion.subMinor]  ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                     + VersionArray.versionHotfix[onlineVersion.hotfix];

                if (File.Exists(versionFileCBP)) //If there's already a version.txt in the local-mods CBP folder, then...
                {
                    Version localVersion = new Version(File.ReadAllText(versionFileCBP)); // this doesn't use UpdateLocalVersionNumber() because of the compare done below it - will break if replaced without modification

                    VersionTextInstalled.Text = "Installed CBP version: "
                                            + VersionArray.versionStart[localVersion.major]
                                            + VersionArray.versionMiddle[localVersion.minor]
                                            + VersionArray.versionEnd[localVersion.subMinor]
                                            + VersionArray.versionHotfix[localVersion.hotfix];
                    try
                    {
                        if (onlineVersion.IsDifferentThan(localVersion))
                        {
                            InstallGameFiles(true, onlineVersion);
                        }
                        else
                        {
                            Status = LauncherStatus.readyCBPEnabled; //if the local version.txt matches the version found in the online file, then no patch required
                            Properties.Settings.Default.CBPLoaded = true;
                            Properties.Settings.Default.CBPUnloaded = false;
                            SaveSettings();
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        MessageBox.Show($"Error installing patch files: {ex}");
                    }
                }

                // compatibility with a6c (maybe making it compatible was a mistake)
                else if (Directory.Exists(Path.Combine(localMods, "Community Balance Patch (Alpha 6c)")))
                {
                    InstallGameFiles(true, Version.zero);
                }

                else
                {
                    InstallGameFiles(false, Version.zero);
                }
            }
            catch (Exception ex)
            {
                if (Properties.Settings.Default.CBPLoaded == true)
                {
                    Status = LauncherStatus.connectionProblemLoaded;
                }
                else
                {
                    Status = LauncherStatus.connectionProblemUnloaded;
                }

                UpdateLocalVersionNumber();
                VersionTextLatest.Text = "Unable to check latest version";
                MessageBox.Show($"Error checking for updates. Maybe no connection could be established? {ex}");
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                if (Properties.Settings.Default.NoWorkshopFiles == false)
                {
                    // try using workshop files
                    try
                    {
                        // extra steps depending on whether this an update to existing install or first time install
                        if (_isUpdate)
                        {
                            Status = LauncherStatus.installingUpdateLocal;

                            // if archive setting is enabled, archive the old version; it looks for an unversioned CBP folder and has a separate check for a6c specifically
                            if (Properties.Settings.Default.CBPArchive == true)
                            {
                                // standard (non-a6c) archiving
                                if (Directory.Exists(Path.Combine(localPathCBP)))
                                {
                                    ArchiveNormal();
                                }
                                
                                // compatibility with archiving a6c
                                else if (Directory.Exists(Path.Combine(localMods, "Community Balance Patch (Alpha 6c)")))
                                {
                                    ArchiveA6c();
                                }

                                else
                                {
                                    MessageBox.Show($"Archive setting is on, but there doesn't seem to be any compatible CBP install to archive.");
                                }
                            }
                            else
                            {
                                //just delete the old files instead
                            }
                        }
                        else
                        {
                            Status = LauncherStatus.installingFirstTimeLocal;
                        }

                        // perhaps this is a chance to use async, but the benefits are minor given the limited IO, and my half-hour attempt wasn't adequate to get it working
                        DirectoryCopy(Path.Combine(workshopPathCBP, "Community Balance Patch"), Path.Combine(localPathCBP), true);

                        //temporary a7-structure copy feature for e.g. help.xml; should be redone (or at least re-examined) with CBPL re-do
                        // part pre-A: strings so that both A and C can access them
                        // part A: backup old files you're about to override
                        // part B: copy contents of <workshopIDfolder>/<Secondary> to RoN/data
                        // part C: (not here, search for PART C) when mod is unloaded, restore the old files

                        if (Properties.Settings.Default.OldFilesRenamed == false)
                        {
                            try
                            {
                                //PART A
                                File.Move(helpXMLOrig, helpXMLOrig + " (old)");
                                File.Move(interfaceXMLOrig, interfaceXMLOrig + " (old)");
                                File.Move(setupwinXMLOrig, setupwinXMLOrig + " (old)");
                                File.Move(patriotsOrig, patriotsOrig + " (original)");

                                Properties.Settings.Default.OldFilesRenamed = true;

                                try
                                {
                                    //PART B
                                    File.Copy(Path.Combine(localPathCBP, "Secondary", helpXML), helpXMLOrig);
                                    File.Copy(Path.Combine(localPathCBP, "Secondary", interfaceXML), interfaceXMLOrig);
                                    File.Copy(Path.Combine(localPathCBP, "Secondary", setupwinXML), setupwinXMLOrig);
                                    File.Copy(Path.Combine(workshopPathCBP, "CBP Setup GUI.exe"), patriotsOrig);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("error with part B1 of the temp a7 logic:\n" + ex);
                                }


                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("error with the temp a7 logic:\n" + ex);
                            }
                            //end of temp
                        }

                        try
                        {
                            UpdateLocalVersionNumber();

                            Properties.Settings.Default.CBPLoaded = true;
                            Properties.Settings.Default.CBPUnloaded = false;
                            SaveSettings();

                            Status = LauncherStatus.readyCBPEnabled;
                        }
                        catch (Exception ex)
                        {
                            Status = LauncherStatus.loadFailed;
                            MessageBox.Show($"Error loading CBP: {ex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        MessageBox.Show($"Error installing CBP from Workshop files: {ex}");
                    }
                }

                else if (Properties.Settings.Default.NoWorkshopFiles == true) // as of v0.3 release this option isn't even exposed to the user yet, but it'll be useful later
                {
                    // try using online files
                    try
                    {
                        WebClient webClient = new WebClient();
                        if (_isUpdate)
                        {
                            Status = LauncherStatus.installingUpdateOnline;
                        }
                        else
                        {
                            Status = LauncherStatus.installingFirstTimeOnline;
                            _onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/version.txt")); /// maybe this should be ported to e.g. google drive as well? then again it's a 1KB file so I
                                                                                                                          /// guess the main concern would be server downtime (either temporary or long term server-taken-offline-forever)
                        }

                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                        webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1hQYZtdsTDihFi33Cc_BisRUHdXvSy5o4"), gameZip, _onlineVersion); //a6c old one https://drive.google.com/uc?export=download&id=1usd0ihBy5HWxsD6UiabV3ohzGxB7SxDD
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        MessageBox.Show($"Error retrieving patch files: {ex}");
                    }
                }

            }

            if (Properties.Settings.Default.CBPUnloaded == true)
            {
                try
                {
                    Directory.Move(Path.Combine(unloadedModsPath, "Community Balance Patch"), Path.Combine(localPathCBP)); //this will still currently fail if the folder already exists though

                    try
                    {
                        //PART A
                        File.Move(helpXMLOrig, helpXMLOrig + " (old)");
                        File.Move(interfaceXMLOrig, interfaceXMLOrig + " (old)");
                        File.Move(setupwinXMLOrig, setupwinXMLOrig + " (old)");
                        File.Move(patriotsOrig, patriotsOrig + " (original)");

                        Properties.Settings.Default.OldFilesRenamed = true;

                        try
                        {
                            //PART B
                            File.Copy(Path.Combine(localPathCBP, "Secondary", helpXML), helpXMLOrig);
                            File.Copy(Path.Combine(localPathCBP, "Secondary", interfaceXML), interfaceXMLOrig);
                            File.Copy(Path.Combine(localPathCBP, "Secondary", setupwinXML), setupwinXMLOrig);
                            File.Copy(Path.Combine(workshopPathCBP, "CBP Setup GUI.exe"), patriotsOrig);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("error with part B1 of the temp a7 logic:\n" + ex);
                        }


                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("error with the temp a7 logic:\n" + ex);
                    }

                    UpdateLocalVersionNumber();

                    Properties.Settings.Default.CBPLoaded = true;
                    Properties.Settings.Default.CBPUnloaded = false;
                    SaveSettings();

                    Status = LauncherStatus.readyCBPEnabled;
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.loadFailed;
                    MessageBox.Show($"Error loading CBP: {ex}");
                }
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                var onlineVersion = ((Version)e.UserState); //I literally don't know when to use var vs other stuff, but it works here so I guess it's fine???

                /// To make the online version be converted (not just the local version), need the near-duplicate code below (on this indent level).
                /// Vs reference, it separates the conversion to string until after displaying the version number,
                /// that way it displays e.g. "Alpha 6c" but actually writes e.g. "6.0.3" to version.txt so that future compares to that file will work
                string onlineVersionString = ((Version)e.UserState).ToString();

                try
                {
                    ZipFile.ExtractToDirectory(gameZip, localMods);
                    File.Delete(gameZip); //extra file to local mods folder, then delete it after the extraction is done
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.installFailed;
                    File.Delete(gameZip); //without this, the .zip will remain if it successfully downloads but then errors while unzipping

                    // show a message asking user if they want to ignore the error (and unlock the launch button)
                    string message = $"If you've already installed CBP this error might be okay to ignore. It may occur if you have the CBP files but no version.txt file to read from, causing the launcher to incorrectly think CBP is not installed. "
                                     + "It's also *probably* okay to ignore this if you want to just play non-CBP for now."
                                     + "\n\nFull error: \n" + $"{ex}"
                                     + "\n\nIgnore error and continue?";
                    string title = "Error installing new patch files";

                    // if they say yes, then also ask if they want to write a new version.txt file where the mod is supposed to be installed:
                    if (MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Status = LauncherStatus.installProblem;

                        string message2 = $"If you're very confident that CBP is actually installed and the problem is just the version.txt file, you can write a new file to resolve this issue."
                                          + "\n\nWould you like to write a new version.txt file?"
                                          + "\n(AVOID DOING THIS IF YOU'RE NOT SURE!!)";

                        string title2 = "Write new version.txt file?";

                        if (MessageBox.Show(message2, title2, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            // currently do nothing; explicitly preferred over using if-not-yes-then-return in case I change this later
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        Environment.Exit(0); /// if they say no, then application is kill;
                    }                        /// Env.Exit used instead of App.Exit because it prevents more code from running
                }                            /// App.Exit was writing the new version file even if you said no on the prompt - maybe could be resolved, but this is okay I think

                File.WriteAllText(versionFileCBP, onlineVersionString); // I thought this is where return would go, but it doesn't, so I evidently don't know what I'm doing

                UpdateLocalVersionNumber();

                Status = LauncherStatus.readyCBPEnabled;
                Properties.Settings.Default.CBPLoaded = true;
                SaveSettings();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.installFailed;
                File.Delete(gameZip); //without this, the .zip will remain if it successfully downloads but then errors while unzipping
                MessageBox.Show($"Error installing new patch files: {ex}"); 
            }
        }

        private void UnloadCBP()
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                try
                {
                    if (Process.GetProcessesByName("patriots").Length > 0)
                    {
                        MessageBox.Show($"CBP Setup GUI (patriots.exe) has to be closed before CBP can be unloaded.");
                    }
                    else
                    {
                        Directory.Move(localPathCBP, Path.Combine(unloadedModsPath, "Community Balance Patch"));
                        Properties.Settings.Default.CBPUnloaded = true;
                        Properties.Settings.Default.CBPLoaded = false;
                        SaveSettings();

                        VersionTextInstalled.Text = "Installed CBP version: not loaded";

                        //PART C
                        File.Delete(helpXMLOrig);
                        File.Delete(interfaceXMLOrig);
                        File.Delete(setupwinXMLOrig);
                        File.Delete(patriotsOrig);
                        File.Move(helpXMLOrig + " (old)", helpXMLOrig);
                        File.Move(interfaceXMLOrig + " (old)", interfaceXMLOrig);
                        File.Move(setupwinXMLOrig + " (old)", setupwinXMLOrig);
                        File.Move(patriotsOrig + " (original)", patriotsOrig);

                        Properties.Settings.Default.OldFilesRenamed = false;
                        //end of c

                        Status = LauncherStatus.readyCBPDisabled;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error unloading mod: {ex}");
                    Status = LauncherStatus.unloadFailed;
                }

                if (Properties.Settings.Default.UnloadWorkshopToo == true)
                {
                    try
                    {
                        //unload workshop
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error unloading Workshop mod: {ex}");
                        Status = LauncherStatus.unloadFailed;
                    }
                }

            }
            else
            {
                MessageBox.Show($"CBP is already unloaded.");
            }
        }

        private void UpdateLocalVersionNumber()
        {
            if (File.Exists(versionFileCBP))
            {
                Version localVersion = new Version(File.ReadAllText(versionFileCBP)); // moved to separate thing to reduce code duplication

                VersionTextInstalled.Text = "Installed CBP version: "
                                        + VersionArray.versionStart[localVersion.major]
                                        + VersionArray.versionMiddle[localVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                    + VersionArray.versionEnd[localVersion.subMinor]  ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                                    + VersionArray.versionHotfix[localVersion.hotfix];
            }
            else
            {
                VersionTextInstalled.Text = "CBP not installed";
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // allow user to switch between CBP and unmodded, and if unmodded then CBP updating logic unneeded
            if (Properties.Settings.Default.DefaultCBP == true)
            {
                CheckForUpdates();
            };
            if (Properties.Settings.Default.DefaultCBP == false)
            {
                if (Properties.Settings.Default.CBPUnloaded == false && Properties.Settings.Default.CBPLoaded == true)
                {
                    UnloadCBP();
                }
                else
                {
                    Status = LauncherStatus.readyCBPDisabled;
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.readyCBPEnabled || Status == LauncherStatus.readyCBPDisabled) // make sure all "launch" button options are included here
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe) // if you do this wrong (I don't fully remember what "wrong" was) the game can launch weirdly e.g. errors, bad mod loads etc.
                {
                    WorkingDirectory = RoNPathFinal //this change compared to reference app was suggested by VS itself - I'm assuming it's functionally equivalent at worst
                };
                Process.Start(startInfo);
                //DEBUG: Process.Start(gameExe);

                Close();
            }
            else if (Status == LauncherStatus.installFailed)
            {
                CheckForUpdates(); // because CheckForUpdates currently includes the logic for *all* installing/loading, it's used for both installFailed and loadFailed right now
            }
            else if (Status == LauncherStatus.loadFailed)
            {
                CheckForUpdates();
            }
            else if (Status == LauncherStatus.unloadFailed)
            {
                UnloadCBP();
            }
        }

        private void ManualLoadCBP_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private void ManualUnloadCBP_Click(object sender, RoutedEventArgs e)
        {
            UnloadCBP();
        }
        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();

            MessageBox.Show($"Settings reset. Default settings will be loaded the next time the program is loaded.");
        }

        private void CBPDefaultCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DefaultCBP = true;

            SaveSettings();

            //debug: System.Windows.MessageBox.Show($"New value: {Properties.Settings.Default.DefaultCBP}"); //p.s. this will *also* be activated if the program loads with check enabled
        }

        private void CBPDefaultCheckbox_UnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DefaultCBP = false;

            SaveSettings();

            //debug: System.Windows.MessageBox.Show($"new value: {Properties.Settings.Default.DefaultCBP}");
        }

        private void UsePrereleaseCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UsePrerelease = true;

            SaveSettings();

            //debug: System.Windows.MessageBox.Show($"New value: {Properties.Settings.Default.DefaultCBP}"); //p.s. this will *also* be activated if the program loads with check enabled
        }

        private void UsePrereleaseCheckbox_UnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UsePrerelease = false;

            SaveSettings();

            //debug: System.Windows.MessageBox.Show($"new value: {Properties.Settings.Default.DefaultCBP}");
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

        private void CBPDefaultChecker()
        {
            if (Properties.Settings.Default.DefaultCBP == true)
            { 
                CBPDefaultCheckbox.IsChecked = true; 
            }
            else if (Properties.Settings.Default.DefaultCBP == false)
            {
                CBPDefaultCheckbox.IsChecked = false;
            }

            if (Properties.Settings.Default.UsePrerelease == true)
            {
                UsePrereleaseCheckbox.IsChecked = true;
            }
            else if (Properties.Settings.Default.UsePrerelease == false)
            {
                UsePrereleaseCheckbox.IsChecked = false;
            }
        }

        private void RoNPathFound()
        {
            if (RoNPathFinal == $"no path")
            {
                MessageBox.Show($"Rise of Nations detected in " + RoNPathCheck);
            }
            RoNPathFinal = RoNPathCheck;

            Properties.Settings.Default.RoNPathSetting = RoNPathFinal;
            SaveSettings();

            //a7 temp
            helpXMLOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "Data", helpXML));
            interfaceXMLOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "Data", interfaceXML));
            setupwinXMLOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "Data", setupwinXML));
            patriotsOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "patriots.exe"));
        }

        private void ArchiveNormal()
        {
            try
            {
                //rename it after moving it, then check version and use that to rename the folder in the archived location
                Directory.Move(Path.Combine(localPathCBP), Path.Combine(archiveCBP, "Community Balance Patch"));

                Version archiveVersion = new Version(File.ReadAllText(Path.Combine(archiveCBP, "Community Balance Patch", "version.txt")));

                string archiveVersionNew = VersionArray.versionStart[archiveVersion.major]
                                         + VersionArray.versionMiddle[archiveVersion.minor]
                                         + VersionArray.versionEnd[archiveVersion.subMinor]
                                         + VersionArray.versionHotfix[archiveVersion.hotfix];

                Directory.Move(Path.Combine(archiveCBP, "Community Balance Patch"), Path.Combine(archiveCBP, "Community Balance Patch " + "(" + archiveVersionNew + ")"));
                MessageBox.Show(archiveVersionNew + " has been archived.");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                MessageBox.Show($"Error archiving previous CBP version: {ex}");
            }
        }

        // can't use same version check because it uses a 3-digit identifier, not 4-digit, but since we know its name it's not too bad
        private void ArchiveA6c()
        {
            try
            {
                //rename it after moving it
                Directory.Move(Path.Combine(localMods, "Community Balance Patch (Alpha 6c)"), Path.Combine(archiveCBP, "Community Balance Patch (Alpha 6c)"));
                MessageBox.Show("Alpha 6c has been archived.");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                MessageBox.Show($"Error archiving previous CBP version (compatbility for a6c): {ex}");
            }
        }
    }

    struct Version 
    {
        internal static Version zero = new Version(0, 0, 0, 0);

        // introduced a fourth tier of version numbering as well, since the naming convention doesn't work very well for subminor being used for the purpose of a hotfix
        public short major;    ///in reference these are private, but I want to refer to them in the version displayed to the user (which I'm converting to X.Y.Z numerical to e.g. "Alpha 6c")
        public short minor;    ///I feel obliged to point out that I have little/no frame of reference to know if this is "bad" to do so maybe this is a code sin and I'm just too naive to know
        public short subMinor;
        public short hotfix;

        internal Version(short _major, short _minor, short _subMinor, short _hotFix)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
            hotfix = _hotFix;
        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.'); //version.txt uses an X.Y.Z version pattern e.g. 6.0.3, so break that up  on the "." to parse each value
            if (_versionStrings.Length !=4)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                hotfix = 0;
                return; //if the version detected doesn't seem to follow the format expected, set detected version to 0.0.0
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
            hotfix = short.Parse(_versionStrings[3]);
        }

        internal bool IsDifferentThan(Version _otherVersion) //check if version (from local version.txt file) matches online with online version.txt
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor) //presumably there's a more efficient / elegant way to lay this out e.g. run one check thrice, cycling major->minor->subminor->hotfix
                    {
                        return true;
                    }
                    else
                    {
                        if (hotfix != _otherVersion.hotfix)
                        {
                            return true;
                        }
                    }
                }
            }
            return false; //detecting if they're different, so false = not different
        }

        public override string ToString()
        { 
            return $"{major}.{minor}.{subMinor}.{hotfix}"; //because this is used for comparison, you can't put the conversion into e.g. "Alpha 6c" here or it will fail the version check above because of the format change
        }
    }

    public class VersionArray //this seems like an inelegant way to implement the string array? but I wasn't sure where else to put it (and have it work)
    {
        //cheeky bit of extra changes to convert the numerical/int based X.Y.Z into the versioning I already used before this launcher
        public static string[] versionStart = new string[11] { "not installed", "Pre-Alpha ", "Alpha ", "Beta ", "Release Candidate ", "1.", "2.", "3.", "4.", "5.", "6." }; // I am a fucking god figuring out how to properly use these arrays based on 10 fragments of 5% knowledge each
        public static string[] versionMiddle = new string[16] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" }; // I don't even know what "static" means in this context, I just know what I need to use it
        public static string[] versionEnd = new string[17] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p" }; //e.g. can optionally just skip the subminor by intentionally using [0]
        public static string[] versionHotfix = new string[20] { "", " (hotfix 1)", " (hotfix 2)", " (hotfix 3)", " (hotfix 4)", " (hotfix 5)", " (hotfix 6)", " (hotfix 7)", " (hotfix 8)", " (hotfix 9)" // 0-9 respectively
                                                              , " (special)" // 10
                                                              , " (PR1)", " (PR2)", " (PR3)", " (PR4)", " (PR5),", "(PR6)", "(PR7)", "(PR8)", "(PR9)" }; // 11-19 respectively
    }*/
}
