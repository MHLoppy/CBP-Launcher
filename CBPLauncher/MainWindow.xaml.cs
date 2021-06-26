/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

// sometimes comments will refer to a "reference [program]" which refers to https://github.com/tom-weiland/csharp-game-launcher

using Microsoft.Win32;          /// this project was made with .NET framework 4.6.1 (at least as of near the start when I'm writing this comment)
using System;                   /// idk *how much* that changes things, but it does influence a few things like what you have to include here compared to using e.g. .NET core 5.0 apparently
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;    // System.IO.Compression.FileSystem added in project References instead (per stackexchange suggestion - I don't actually fully understand it ::fingerguns::)
using System.Net;           
using System.Windows;
using System.Windows.Forms;     // this thing makes message boxes messy, since now there's one from .Windows and one from .Windows.Forms @_@
using System.Windows.Media;     // used for selecting brushes (used for coloring in e.g. textboxes)
using Microsoft.VisualBasic;    // used for the current (temporary?) popup user text input for manual path

namespace CBPLauncher
{

    enum LauncherStatus
    {
        readyCBPEnabled,
        readyCBPDisabled,
        loadFailed,
        unloadFailed,
        installFailed,
        installingFirstTimeLocal,
        installingUpdateLocal,
        installingFirstTimeOnline,
        installingUpdateOnline,
        connectionProblem,
        installProblem
    }

    public partial class MainWindow : Window
    {
        private string rootPath;
        private string gameZip;
        private string gameExe;
        private string localMods;
        private string RoNPath;
        private string RoNPathManual; //used for manual install only
        private string workshopPath; // yet to implement the part where it finds and uses downloaded Workshop files - right now it downloads a non-Steam copy of the files from google drive
        private string unloadedModsPath;

        /// ===== START OF MOD LIST =====

        //Community Balance Patch
        private string modnameCBP;
        private string workshopIDCBP;
        private string workshopPathCBP;
        private string localPathCBP;
        private string versionFileCBP;

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
                    case LauncherStatus.installingFirstTimeLocal:                       /// primary method: use workshop files;
                        StatusReadout.Text = "Installing patch from local files...";    /// means no local-mods CBP detected
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.installingUpdateLocal:                          /// primary method: use workshop files;
                        StatusReadout.Text = "Installing update from local files...";   /// local-mods CBP detected, but out of date compared to workshop version.txt
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.installingFirstTimeOnline:                      /// backup method: use online files;
                        StatusReadout.Text = "Installing patch from online files...";   /// means no local-mods CBP detected but can't find workshop files either
                        StatusReadout.Foreground = Brushes.White;
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.installingUpdateOnline:                         /// backup method: use online files; 
                        StatusReadout.Text = "Installing update from online files...";  /// local-mods CBP detected, but can't find workshop files and
                        StatusReadout.Foreground = Brushes.White;                       /// local files out of date compared to online version.txt
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.connectionProblem:
                        StatusReadout.Text = "Error: connectivity issue";
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
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during initialization: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            RegistryKey regPath; //this part (and related below) is to find the install location for RoN:EE (Steam)
 //!!!!!!           // apparently this is not a good method for the registry part? use using instead? https://stackoverflow.com/questions/1675864/read-a-registry-key
            if (Environment.Is64BitOperatingSystem) //I don't *fully* understand what's going on here (ported from stackexchange), but this block seems to be needed to prevent null value return due to 32/64 bit differences???
            {
                regPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            else
            {
                regPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            }

            regPathDebug.Text = "Debug: registry read as " + regPath;

            try
            {
                // core paths
                rootPath = Directory.GetCurrentDirectory();
                gameZip = Path.Combine(rootPath, "Community Balance Patch.zip"); //static file name even with updates, otherwise you have to change this value!

                using (RegistryKey ronReg = regPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450"))
                {
                    if (ronReg == null) // some RoN:EE installs (for some UNGODLY REASON WHICH I DON'T UNDERSTAND) don't have their location in the registry, so we have to work around that
                    {
                        // try a default 64-bit install path, since that should honestly work for most of the users with cursed registries
                        RoNPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\steamapps\common\Rise of Nations";

                        if (File.Exists(Path.Combine(RoNPath, "riseofnations.exe")))
                        {
                            // success: automated secondary 1
                            return;
                        }

                        // old way of doing it, but used as as backup because I don't know if the environment call ever fails or not
                        /*RoNPath = @"C:\Program Files (x86)\Steam\steamapps\common\Rise of Nations";

                        if (File.Exists(Path.Combine(RoNPath, "riseofnations.exe")))
                        {
                            // success: automated secondary 2
                            return;
                        }*/

                        if (File.Exists(Path.Combine(RoNPath, "riseofnations.exe")))
                        {
                            System.Windows.MessageBox.Show($"Rise of Nations detected in " + RoNPath);
                        }
                        else
                        {
                            System.Windows.MessageBox.Show($"None of the automated methods were able to find your Rise of Nations install. Try entering the path manually instead.");
                            RoNPathManual = Interaction.InputBox(@"Enter the path for your Rise of Nations install (e.g. D:\SteamLibrary\SteamApps\common\Rise of Nations", "Manual path entry");

                            // check that the user has input a seemingly valid location
                            if (File.Exists(Path.Combine(RoNPathManual, "riseofnations.exe")))
                            {
                                RoNPath = RoNPathManual;
                            }
                            else
                            {
                                System.Windows.MessageBox.Show($"Can't find a RoN install at that location! Launcher will now close.");
                                Environment.Exit(0);
                            }
                        }
                    }

                    else
                    {
                        // success: automated primary
                        RoNPath = regPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450").GetValue("InstallLocation").ToString();
                    }
                }

                gameExe = Path.Combine(RoNPath, "riseofnations.exe"); //in EE v1.20 this is the main game exe, with patriots.exe as the launcher (in T&P main game was rise.exe)
                localMods = Path.Combine(RoNPath, "mods");
                workshopPath = Path.GetFullPath(Path.Combine(RoNPath, @"..\..", @"workshop\content\287450")); //maybe not the best method, but serviceable? Path.GetFullPath used to make final path more human-readable

                /// ===== START OF MOD LIST =====

                // Community Balance Patch
                modnameCBP = "Community Balance Patch"; // this has to be static, which loses the benefit of having the version display in in-game mod manager, but acceptable because it will display in CBP Launcher instead
                workshopIDCBP = "2287791153"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)

                workshopPathCBP = Path.Combine(Path.GetFullPath(workshopPath), workshopIDCBP); /// getfullpath ensures the slash is included between the two
                localPathCBP = Path.Combine(Path.GetFullPath(localMods), modnameCBP);          /// I tried @"\" and "\\" and both made the first part (localMods) get ignored in the combined path
                versionFileCBP = Path.Combine(localPathCBP, "Version.txt"); // moved here in order to move with the data files (useful), and better structure to support other mods in future

                /// TODO
                /// use File.Exists and/or Directory.Exists to confirm that CBP files have actually downloaded from Workshop
                /// (at the moment it just assumes they exist and eventually errors later on if they don't)

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

                // detected paths shown in the UI
                EEPath.Text = RoNPath;
                workshopPathDebug.Text = workshopPath;
                workshopPathCBPDebug.Text = workshopPathCBP;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating paths: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }
            try
            {
                Directory.CreateDirectory(Path.Combine(localMods, "Unloaded Mods")); // will be used to unload CBP
                unloadedModsPath = Path.Combine(localMods, "Unloaded Mods");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating Unloaded Mods directory {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            CBPDefaultChecker();

        }

        private void CheckForUpdates()
        {
            try // without the try you can accidentally create online-only DRM whoops
            {
                VersionTextOnline.Text = "Checking online version...";
                VersionTextLocal.Text = "Checking local version...";

                WebClient webClient = new WebClient();                                                           /// Moved this section from reference to here in order to display
                Version onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/version.txt")); /// latest available version as well as installed version

                VersionTextOnline.Text = "Latest CBP version: "
                     + VersionArray.versionStart[onlineVersion.major]
                     + VersionArray.versionMiddle[onlineVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                 + VersionArray.versionEnd[onlineVersion.subMinor]; ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::

                if (File.Exists(versionFileCBP)) //If there's already a version.txt in the local-mods CBP folder, then...
                {
                    Version localVersion = new Version(File.ReadAllText(versionFileCBP)); // this doesn't use UpdateLocalVersionNumber() because of the compare done below it - will break if replaced without modification

                    VersionTextLocal.Text = "Installed CBP version: "
                                            + VersionArray.versionStart[localVersion.major]
                                            + VersionArray.versionMiddle[localVersion.minor]
                                            + VersionArray.versionEnd[localVersion.subMinor];
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
                        System.Windows.MessageBox.Show($"Error installing patch files: {ex}");
                    }
                }
                else
                {
                    InstallGameFiles(false, Version.zero);
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.connectionProblem;
                System.Windows.MessageBox.Show($"Error checking for updates. Maybe no internet connection could be established? {ex}");
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            if (Properties.Settings.Default.CBPUnloaded == false)

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
                       _onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/version.txt")); /// maybe this should be ported to e.g. google drive as well? then again it's a 1KB file so I
                                                                                                              /// guess the main concern would be server downtime (either temporary or long term server-taken-offline-forever)
                    }

                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                    webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1hQYZtdsTDihFi33Cc_BisRUHdXvSy5o4"), gameZip, _onlineVersion); //a6c old one https://drive.google.com/uc?export=download&id=1usd0ihBy5HWxsD6UiabV3ohzGxB7SxDD
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.installFailed;
                    System.Windows.MessageBox.Show($"Error retrieving patch files: {ex}");
                }
            if (Properties.Settings.Default.CBPUnloaded == true)

                try
                {
                    Directory.Move(Path.Combine(unloadedModsPath, "Community Balance Patch"), Path.Combine(localPathCBP)); //this will still currently fail if the folder already exists though

                    UpdateLocalVersionNumber();

                    Properties.Settings.Default.CBPLoaded = true;
                    Properties.Settings.Default.CBPUnloaded = false;
                    SaveSettings();

                    Status = LauncherStatus.readyCBPEnabled;
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.loadFailed;
                    System.Windows.MessageBox.Show($"Error loading CBP: {ex}");
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
                                     + Environment.NewLine + Environment.NewLine + "Full error: " + Environment.NewLine + $"{ex}"
                                     + Environment.NewLine + Environment.NewLine + "Ignore error and continue?";
                    string caption = "Error installing new patch files";
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result;

                    result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);

                    // if they say yes, then also ask if they want to write a new version.txt file where the mod is supposed to be installed:
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        Status = LauncherStatus.installProblem;

                        string message2 = $"If you're very confident that CBP is actually installed and the problem is just the version.txt file, you can write a new file to resolve this issue."
                                          + Environment.NewLine + Environment.NewLine + "Would you like to write a new version.txt file?"
                                          + Environment.NewLine + "(AVOID DOING THIS IF YOU'RE NOT SURE!!)";
                        string caption2 = "Write new version.txt file?";
                        MessageBoxButtons buttons2 = MessageBoxButtons.YesNo;
                        DialogResult result2;

                        result2 = System.Windows.Forms.MessageBox.Show(message2, caption2, buttons2);
                        if (result2 == System.Windows.Forms.DialogResult.Yes)
                        {
                            File.WriteAllText(versionFileCBP, onlineVersionString);

                            UpdateLocalVersionNumber();

                            Status = LauncherStatus.readyCBPEnabled;
                            Properties.Settings.Default.CBPLoaded = true;
                            SaveSettings();

                            return; /// I had expected that this return would actually go down to the indicated location before the caught exception, but it doesn't?
                        }           /// It means I have to do the few lines above this when I didn't really want to because it's super redundant code
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
                System.Windows.MessageBox.Show($"Error installing new patch files: {ex}"); 
            }
        }

        private void UnloadCBP()
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                try
                {
                    System.IO.Directory.Move(localPathCBP, Path.Combine(unloadedModsPath, "Community Balance Patch"));
                    Properties.Settings.Default.CBPUnloaded = true;
                    Properties.Settings.Default.CBPLoaded = false;
                    SaveSettings();

                    VersionTextLocal.Text = "Installed CBP version: not loaded";

                    Status = LauncherStatus.readyCBPDisabled;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error unloading mod: {ex}");
                    Status = LauncherStatus.unloadFailed;
                }
            }
            else
            {
                System.Windows.MessageBox.Show($"CBP is already unloaded.");
            }
        }

        private void UpdateLocalVersionNumber()
        {
            Version localVersion = new Version(File.ReadAllText(versionFileCBP)); // moved to separate thing to reduce code duplication

            VersionTextLocal.Text = "Installed CBP version: "
                                    + VersionArray.versionStart[localVersion.major]
                                    + VersionArray.versionMiddle[localVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                    + VersionArray.versionEnd[localVersion.subMinor]; ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
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
                    WorkingDirectory = RoNPath //this change compared to reference app was suggested by VS itself - I'm assuming it's functionally equivalent at worst
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

            System.Windows.MessageBox.Show($"Settings reset. Default settings will be loaded the next time the program is loaded.");
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
        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void CBPDefaultChecker()
        {
            if ( Properties.Settings.Default.DefaultCBP == true)
            { 
                CBPDefaultCheckbox.IsChecked = true; 
            }
            else if ( Properties.Settings.Default.DefaultCBP == false)
            {
                CBPDefaultCheckbox.IsChecked = false;
            }
        }

    }

    struct Version 
    {
        internal static Version zero = new Version(0, 0, 0);

        public short major;    ///in reference these are private, but I want to refer to them in the version displayed to the user (which I'm converting to X.Y.Z numerical to e.g. "Alpha 6c")
        public short minor;    ///I feel obliged to point out that I have little/no frame of reference to know if this is "bad" to do so maybe this is a code sin and I'm just too naive to know
        public short subMinor; 

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.'); //version.txt uses an X.Y.Z version pattern e.g. 6.0.3, so break that up  on the "." to parse each value
            if (_versionStrings.Length !=3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return; //if the version detected doesn't seem to follow the format expected, set detected version to 0.0.0
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
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
                    if (subMinor != _otherVersion.subMinor) //presumably there's a more efficient / elegant way to lay this out e.g. run one check thrice, cycling major->minor->subminor
                    {
                        return true;
                    }
                }
            }
            return false; //detecting if they're different, so false = not different
        }

        public override string ToString()
        { 
            return $"{major}.{minor}.{subMinor}"; //because this is used for comparison, you can't put the conversion into e.g. "Alpha 6c" here or it will fail the version check above because of the format change
        }
    }

    public class VersionArray //this seems like an inelegant way to implement the string array? but I wasn't sure where else to put it (and have it work)
    {
        //cheeky bit of extra changes to convert the numerical/int based X.Y.Z into the versioning I already used before this launcher
        public static string[] versionStart = new string[6] { "not installed", "Pre-Alpha ", "Alpha ", "Beta ", "Release Candidate ", "1." }; // I am a fucking god figuring out how to properly use these arrays based on 10 fragments of 5% knowledge each
        public static string[] versionMiddle = new string[13] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" }; // I don't even know what "static" means in this context, I just know what I need to use it
        public static string[] versionEnd = new string[12] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" }; //e.g. can optionally just skip the subminor by intentionally using [0]
        //and add a hotfix number as well, where 0 in the array will be blank (not a hotfix update)
    }
}
