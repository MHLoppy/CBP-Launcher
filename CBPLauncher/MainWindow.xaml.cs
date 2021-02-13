using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
// using System.IO.Compression.FileSystem; added in project References instead (per stackexchange suggestion - I don't actually understand it ::fingerguns::)
using System.Net;           /// this project was made with .NET framework 4.6.1 (at least as of near the start when I'm writing this comment)
using System.Windows;       ///idk *how much* that changes things, but it does influence a few things like what you have to include here compared to using e.g. .NET core apparently
namespace CBPLauncher
{

    enum LauncherStatus
    {
        ready,
        failed,
        downloadingPatch,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml [imagine understanding what's going on enough to succinctly summarise it]
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        private string localMods;
        private string gameInstallPath;
        private string workshopPath; // yet to implement the part where it finds and uses downloaded Workshop files - right now it downloads a non-Steam copy of the files from google drive
        private string workshopPathCBP;

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
                        PlayButton.Content = "Launch RoN:EE";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry?";
                        break;
                    case LauncherStatus.downloadingPatch:
                        PlayButton.Content = "Downloading Patch"; //means no local-mods CBP detected
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update"; //local-mods CBP detected, but out of date compared to online version.txt
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
                MessageBox.Show($"Error during initialization: {ex}");
            }

            RegistryKey regPath; //this part (and related below) is to find the install location for RoN:EE (Steam)


            if (Environment.Is64BitOperatingSystem) //I don't *fully* understand what's going on here (ported from stackexchange), but this block seems to be needed to prevent null value return due to 32/64 bit differences???
                regPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            else
                regPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

            regPathDebug.Text = "Debug: registry read as " + regPath;

            try
            {
                rootPath = Directory.GetCurrentDirectory();
                gameZip = Path.Combine(rootPath, "Community Balance Patch (Alpha 6c).zip"); //at some point, this should probably have a generic name that doesn't change between versions e.g. "CBP.zip"
                gameInstallPath = regPath.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 287450").GetValue("InstallLocation").ToString();
                EEPath.Text = gameInstallPath; //to display the detected install path for RoN:EE
                gameExe = Path.Combine(gameInstallPath, "riseofnations.exe"); //in EE this is the main game exe, with patriots.exe as the launcher (in T&P it was just rise.exe)

                localMods = Path.Combine(gameInstallPath, "mods");
                workshopPath = Path.GetFullPath(Path.Combine(gameInstallPath, @"..\..", @"workshop\content\287450")); ///maybe not the best method, but serviceable?
                workshopPathCBP = Path.Combine(workshopPath, @"2287791153");                                          ///Path.GetFullPath used to make final path more human-readable
                workshopPathDebug.Text = workshopPath; //debug readout
                workshopPathCBPDebug.Text = workshopPathCBP; //debug readout

                versionFile = Path.Combine(localMods, "Version.txt"); //putting this file in the mods folder seems like a good spot
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating paths: {ex}");
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

            if (File.Exists(versionFile)) //If there's already a version.txt in the local mods folder, then...
            {
                Version localVersion = new Version(File.ReadAllText(versionFile)); // recent changes to the version displays creates some code duplication X_X

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
                    MessageBox.Show($"Error installing patch files: {ex}");
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
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingPatch;
                    _onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/version.txt")); ///maybe this should be ported to e.g. google drive as well? then again it's a 1KB file so I
                                                                                                              /// guess the main concern would be server downtime (either temporary or long term server-taken-offline-forever)
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1usd0ihBy5HWxsD6UiabV3ohzGxB7SxDD"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error retrieving patch files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                var onlineVersion = ((Version)e.UserState); //I literally don't know when to use var vs other stuff, but it works here so I guess it's fine???
                                                                                          ///To make the online version be converted (not just the local version), need the near-duplicate code below
                                                                                          ///Vs reference, it separates the conversion to string until after displaying the version number
                                                                                          ///that way it displays e.g. "Alpha 6c" but actually writes e.g. "6.0.3" to version.txt so that future compares to that file will work
                string onlineVersionString = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, localMods);
                File.Delete(gameZip); //extra file to local mods folder, then delete it after the extraction is done

                File.WriteAllText(versionFile, onlineVersionString);

                Version localVersion = new Version(File.ReadAllText(versionFile));

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
                MessageBox.Show($"Error installing new patch files: {ex}"); 
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
            return false; //detecting if they're different: false = not different
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
