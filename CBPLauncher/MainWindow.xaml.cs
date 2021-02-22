/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
// using System.IO.Compression.FileSystem; added in project References instead (per stackexchange suggestion - I don't actually understand it ::fingerguns::)
using System.Net;           /// this project was made with .NET framework 4.6.1 (at least as of near the start when I'm writing this comment)
using System.Windows;       ///idk *how much* that changes things, but it does influence a few things like what you have to include here compared to using e.g. .NET core 5.0 apparently
using System.Windows.Forms; // this thing makes message boxes messy, since now there's one from .Windows and one from .Windows.Forms @_@

namespace CBPLauncher
{

    enum LauncherStatus
    {
        ready,
        failed,
        installingFirstPatchLocal,
        installingUpdateLocal,
        installingFirstPatchOnline,
        installingUpdateOnline
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml [imagine understanding what's going on enough to succinctly summarise it]
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string gameZip;
        private string gameExe;
        private string localMods;
        private string gameInstallPath;
        private string workshopPath; // yet to implement the part where it finds and uses downloaded Workshop files - right now it downloads a non-Steam copy of the files from google drive

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
                    case LauncherStatus.ready:
                        PlayButton.Content = "Launch Game";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry?";
                        break;
                    case LauncherStatus.installingFirstPatchLocal:  /// primary method: use workshop files;
                        PlayButton.Content = "Installing Patch";    /// means no local-mods CBP detected
                        break;
                    case LauncherStatus.installingUpdateLocal:      /// primary method: use workshop files;
                        PlayButton.Content = "Installing Update";   /// local-mods CBP detected, but out of date compared to workshop version.txt
                        break;
                    case LauncherStatus.installingFirstPatchOnline: /// backup method: use online files;
                        PlayButton.Content = "Installing Patch";    /// means no local-mods CBP detected but can't find workshop files either
                        break;
                    case LauncherStatus.installingUpdateOnline:     /// backup method: use online files; 
                        PlayButton.Content = "Installing Update";   /// local-mods CBP detected, but can't find workshop files and
                        break;                                      /// local files out of date compared to online version.txt
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

            if (Environment.Is64BitOperatingSystem) //I don't *fully* understand what's going on here (ported from stackexchange), but this block seems to be needed to prevent null value return due to 32/64 bit differences???
                regPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            else
                regPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            regPathDebug.Text = "Debug: registry read as " + regPath;

            try
            {
                // core paths
                rootPath = Directory.GetCurrentDirectory();
                gameZip = Path.Combine(rootPath, "Community Balance Patch.zip"); //static file name even with updates, otherwise you have to change this value!
                gameInstallPath = regPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450").GetValue("InstallLocation").ToString();
                gameExe = Path.Combine(gameInstallPath, "riseofnations.exe"); //in EE v1.20 this is the main game exe, with patriots.exe as the launcher (in T&P main game was rise.exe)
                localMods = Path.Combine(gameInstallPath, "mods");
                workshopPath = Path.GetFullPath(Path.Combine(gameInstallPath, @"..\..", @"workshop\content\287450")); //maybe not the best method, but serviceable? Path.GetFullPath used to make final path more human-readable

                /// ===== START OF MOD LIST =====

                // Community Balance Patch
                modnameCBP = "Community Balance Patch"; // this has to be static, which loses the benefit of having the version display in in-game mod manager, but acceptable because it will display in CBP Launcher instead
                workshopIDCBP = "2287791153"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)

                workshopPathCBP = Path.Combine(Path.GetFullPath(workshopPath), workshopIDCBP); /// getfullpath ensures the slash is included between the two
                localPathCBP = Path.Combine(Path.GetFullPath(localMods), modnameCBP);          /// I tried @"\" and "\\" and both made the first part (localMods) get ignored in the combined path
                versionFileCBP = Path.Combine(localPathCBP, "Version.txt"); // moved here in order to move with the data files (useful), and better structure to support other mods in future

                // Example New Mod
                // modname<MOD> = A
                // workshopID<MOD> = B

                // workshopPath<MOD> = X
                // localPath<MOD> = Y // remember to declare (?) these new strings at the top ("private string = ") as well
                // versionFile<MOD> = Path.Combine(localPath<MOD>, "Version.txt"

                /// Ideally in future these values can be dynamically loaded/added/edited etc from
                /// within the program's UI, meaning that these values no longer need to be hardcoded like this.
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
                EEPath.Text = gameInstallPath;
                workshopPathDebug.Text = workshopPath;
                workshopPathCBPDebug.Text = workshopPathCBP;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating paths: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }
        }

        private void CheckForUpdates()
        {
            WebClient webClient = new WebClient();                                                           /// Moved this section from reference to here in order to display
            Version onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/version.txt")); /// latest available version as well as installed version

            VersionTextOnline.Text = "Latest CBP version: "
                 + VersionArray.versionStart[onlineVersion.major]
                 + VersionArray.versionMiddle[onlineVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                 + VersionArray.versionEnd[onlineVersion.subMinor]; ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::

            if (File.Exists(versionFileCBP)) //If there's already a version.txt in the local-mods CBP folder, then...
            {
                Version localVersion = new Version(File.ReadAllText(versionFileCBP)); // recent changes to the version displays creates some code duplication X_X

                VersionTextLocal.Text = "Installed CBP version: "
                                 + VersionArray.versionStart[localVersion.major]
                                 + VersionArray.versionMiddle[localVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                 + VersionArray.versionEnd[localVersion.subMinor]; ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                try
                {
                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready; //if the local version.txt matches the version found in the online file, then no patch required
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    System.Windows.MessageBox.Show($"Error installing patch files: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.installingUpdateOnline;
                }
                else
                {
                    Status = LauncherStatus.installingFirstPatchOnline;
                    _onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/version.txt")); /// maybe this should be ported to e.g. google drive as well? then again it's a 1KB file so I
                                                                                                              /// guess the main concern would be server downtime (either temporary or long term server-taken-offline-forever)
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1hQYZtdsTDihFi33Cc_BisRUHdXvSy5o4"), gameZip, _onlineVersion); //a6c old one https://drive.google.com/uc?export=download&id=1usd0ihBy5HWxsD6UiabV3ohzGxB7SxDD
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                System.Windows.MessageBox.Show($"Error retrieving patch files: {ex}");
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
                    Directory.CreateDirectory(Path.Combine(localMods, "Unloaded Mods")); // will be used to unload CBP
                    File.Delete(gameZip); //extra file to local mods folder, then delete it after the extraction is done
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
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
                        Status = LauncherStatus.ready;

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

                            Version localVersion2 = new Version(File.ReadAllText(versionFileCBP)); //I don't fully understand why I can't refer to localVersion, but ALSO can't declare it??

                            VersionTextLocal.Text = "Installed CBP version: "
                                                    + VersionArray.versionStart[localVersion2.major]
                                                    + VersionArray.versionMiddle[localVersion2.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                                    + VersionArray.versionEnd[localVersion2.subMinor]; ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
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

                Version localVersion = new Version(File.ReadAllText(versionFileCBP));

                VersionTextLocal.Text = "Installed CBP version: "
                                 + VersionArray.versionStart[localVersion.major]
                                 + VersionArray.versionMiddle[localVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                 + VersionArray.versionEnd[localVersion.subMinor]; ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::

                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                File.Delete(gameZip); //without this, the .zip will remain if it successfully downloads but then errors while unzipping
                System.Windows.MessageBox.Show($"Error installing new patch files: {ex}"); 
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe) // if you do this wrong (I don't fully remember what "wrong" was) the game can launch weirdly e.g. errors, bad mod loads etc.
                {
                    WorkingDirectory = gameInstallPath //this change compared to reference app was suggested by VS itself - I'm assuming it's functionally equivalent at worst
                };
                Process.Start(startInfo);
                //DEBUG: Process.Start(gameExe);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
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
        public static String[] versionStart = new string[6] { "not installed", "Pre-Alpha ", "Alpha ", "Beta ", "Release Candidate ", "1." }; // I am a fucking god figuring out how to properly use these arrays based on 10 fragments of 5% knowledge each
        public static String[] versionMiddle = new string[13] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" }; // I don't even know what "static" means in this context, I just know what I need to use it
        public static String[] versionEnd = new string[12] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" }; //e.g. can optionally just skip the subminor by intentionally using [0]
    }
}
