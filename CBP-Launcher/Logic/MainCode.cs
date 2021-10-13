﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CBPLauncher.Core;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using static CBPLauncher.Logic.BasicIO;

namespace CBPLauncher.Logic
{
    enum LauncherStatus
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

    public class MainCode : ObservableObject
    {
        private string rootPath;
        private string gameZip;
        private string gameExe;
        private string localMods;
        private string RoNPathFinal = Properties.Settings.Default.RoNPathSetting; // is it possible there's a narrow af edge case where the path ends up wrong after a launcher version upgrade?
        private string RoNPathCheck;
        private string workshopPath;
        private string unloadedModsPath;
        private string RoNDataPath;

        private bool BullshitButtonPress = false;

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
        private string versionFileCBPLocal;
        private string versionFileCBPWorkshop;
        private string archiveCBP;
        private bool abortWorkshopCopyCBP = false;
        private string folderCBProot;
        private string folderCBPmodded;//unused; using the "normal" local mods location instead
        private string folderCBPoriginal;
        private List<string> CBPFileListAll = new List<string>();//the empty list seems to sometimes have a null error (in app.xaml of all places) in VS2019..... except it doesn't seem to matter at all ? ? ? ? ?
        private List<string> CBPFileListModded = new List<string>();
        private List<string> CBPFileListOriginal = new List<string>();
        private bool updateSetupLater = false;
        private bool prereleaseFilesDetected = false;
        private string primaryDataCBP;
        private string secondaryDataCBP;
        private string primaryNonDataCBP;
        private string secondarynonDataCBP;
        
        //private string patchNotesCBP; //moved out to its own VM instead

        /// ===== END OF MOD LIST =====

        // new MVVM-like strings etc
        public RegistryKey RegPath;

        private string regPathDebug;
        public string RegPathDebug
        {
            get => regPathDebug;
            set
            {
                regPathDebug = value;
                OnPropertyChanged();
            }
        }

        private bool launchEnabled = false;
        public bool LaunchEnabled
        {
            get => launchEnabled;
            set
            {
                launchEnabled = value;
                OnPropertyChanged();
            }
        }

        private string launchButtonText = "...";
        public string LaunchButtonText
        {
            get => launchButtonText;
            set
            {
                launchButtonText = value;
                OnPropertyChanged();
            }
        }

        private string launchStatusText = "Initializing...";
        public string LaunchStatusText
        {
            get => launchStatusText;
            set
            {
                launchStatusText = value;
                OnPropertyChanged();
            }
        }

        private Brush launchStatusColor = Brushes.LightGray;
        public Brush LaunchStatusColor
        {
            get => launchStatusColor;
            set
            {
                launchStatusColor = value;
                OnPropertyChanged();
            }
        }

        private bool logoRoNEE = true;
        public bool LogoRoNEE
        {
            get => logoRoNEE;
            set
            {
                logoRoNEE = value;
                OnPropertyChanged();
            }
        }

        private bool logoCBP = false;
        public bool LogoCBP
        {
            get => logoCBP;
            set
            {
                logoCBP = value;
                OnPropertyChanged();
            }
        }

        private string eEPath;
        public string EEPath
        {
            get => eEPath;
            set
            {
                eEPath = value;
                OnPropertyChanged();
            }
        }

        private string workshopPathDebug;
        public string WorkshopPathDebug
        {
            get => workshopPathDebug;
            set
            {
                workshopPathDebug = value;
                OnPropertyChanged();
            }
        }

        private string workshopPathCBPDebug;
        public string WorkshopPathCBPDebug
        {
            get => workshopPathCBPDebug;
            set
            {
                workshopPathCBPDebug = value;
                OnPropertyChanged();
            }
        }

        private string versionTextLatest = "Checking version...";
        public string VersionTextLatest
        {
            get => versionTextLatest;
            set
            {
                versionTextLatest = value;
                OnPropertyChanged();
            }
        }

        private string versionTextInstalled = "Checking version...";
        public string VersionTextInstalled
        {
            get => versionTextInstalled;
            set
            {
                versionTextInstalled = value;
                OnPropertyChanged();
            }
        }


        //this seems wrong but I can't remember if I know a better way
        private bool cBPDefaultCheckbox = Properties.Settings.Default.DefaultCBP;
        public bool CBPDefaultCheckbox
        {
            get => cBPDefaultCheckbox;
            set
            {
                cBPDefaultCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool usePrereleaseCheckbox = Properties.Settings.Default.UsePrerelease;
        public bool UsePrereleaseCheckbox
        {
            get => usePrereleaseCheckbox;
            set
            {
                usePrereleaseCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool useDefaultLauncherCheckbox = Properties.Settings.Default.UseDefaultLauncher;
        public bool UseDefaultLauncherCheckbox
        {
            get => useDefaultLauncherCheckbox;
            set
            {
                useDefaultLauncherCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool usePrimaryFilesCheckbox = Properties.Settings.Default.UsePrimaryFileList;
        public bool UsePrimaryFilesCheckbox
        {
            get => usePrimaryFilesCheckbox;
            set
            {
                usePrimaryFilesCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool useSecondaryFilesCheckbox = Properties.Settings.Default.UseSecondaryFileList;
        public bool UseSecondaryFilesCheckbox
        {
            get => useSecondaryFilesCheckbox;
            set
            {
                useSecondaryFilesCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool detectBullshitCheckbox = Properties.Settings.Default.DetectBullshit;
        public bool DetectBullshitCheckbox
        {
            get => detectBullshitCheckbox;
            set
            {
                detectBullshitCheckbox = value;
                OnPropertyChanged();
            }
        }

        private string launcherVersion = "CBP Launcher vX.Y.Z";
        public string LauncherVersion
        {
            get => launcherVersion;
            set
            {
                launcherVersion = value;
                OnPropertyChanged();//this may not be needed, not sure
            }
        }

        //RelayCommand definition things
        public RelayCommand CBPDefaultCommand { get; set; }
        public RelayCommand UsePrereleaseCommand { get; set; }
        public RelayCommand UseDefaultLauncherCommand { get; set; }
        public RelayCommand ResetSettingsCommand { get; set; }
        public RelayCommand UsePrimaryFilesCommand { get; set; }
        public RelayCommand UseSecondaryFilesCommand { get; set; }
        public RelayCommand DetectBullshitCommand { get; set; }
        public RelayCommand DetectBullshitNowCommand { get; set; }
        

        public RelayCommand PlayButtonCommand { get; set; }
        public RelayCommand LoadCBPCommand { get; set; }
        public RelayCommand UnloadCBPCommand { get; set; }

        public RelayCommand WorkshopCommand { get; set; }
        public RelayCommand GithubCommand { get; set; }
        public RelayCommand DiscordCommand { get; set; }


        public RelayCommand SkinSpartanV1Command { get; set; }
        public RelayCommand SkinSpartanV1MiniCommand { get; set; }
        public RelayCommand SpV1TabPatchNotesCommand { get; set; }
        public RelayCommand SpV1TabModManagerCommand { get; set; }
        public RelayCommand SpV1TabOptionsCommand { get; set; }
        public RelayCommand SpV1TabLogCommand { get; set; }


        public RelayCommand SkinClassicPlusCommand { get; set; }
        public RelayCommand SkinClassicPlusMiniCommand { get; set; }
        public RelayCommand CPTabPatchNotesCommand { get; set; }
        public RelayCommand CPTabModManagerCommand { get; set; }
        public RelayCommand CPTabOptionsCommand { get; set; }
        public RelayCommand CPTabLogCommand { get; set; }

        public RelayCommand MinimiseCommand { get; set; }
        public RelayCommand ExitCommand { get; set; }

        //test commands
        public RelayCommand ChangeSkinCommand { get; set; }

        private object _currentSkin;
        public object CurrentSkin
        {
            get { return _currentSkin; }
            set
            {
                _currentSkin = value;
                OnPropertyChanged();
            }
        }

        private object _currentTab;
        public object CurrentTab
        {
            get { return _currentTab; }
            set
            {
                _currentTab = value;
                OnPropertyChanged();
            }
        }

        // Skin "viewmodel"s
        

        public SpartanV1VM SpartanV1 { get; set; }
        public SpartanV1MiniVM SpartanV1Mini { get; set; }
        public SpartanV1PatchNotesVM SpartanV1PatchNotes { get; set; }
        public SpartanV1ModManagerVM SpartanV1ModManager { get; set; }
        public SpartanV1OptionsVM SpartanV1Options { get; set; }
        public SpartanV1LogVM SpartanV1Log { get; set; }
        public SpartanV1DummyTabVM SpartanV1DummyTab { get; set; }

        public ClassicPlusVM ClassicPlus { get; set; }
        public ClassicPlusMiniVM ClassicPlusMini { get; set; }
        public ClassicPlusPatchNotesVM ClassicPlusPatchNotes { get; set; }
        public ClassicPlusModManagerVM ClassicPlusModManager { get; set; }
        public ClassicPlusOptionsVM ClassicPlusOptions { get; set; }
        public ClassicPlusLogVM ClassicPlusLog { get; set; }
        public DummyTabVM ClassicPlusDummyTab { get; set; }

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
                        LaunchStatusText = "Initializing...";
                        LaunchStatusColor = Brushes.White;
                        LaunchButtonText = "Launch Game";
                        LaunchEnabled = false;
                        break;
                    case LauncherStatus.readyCBPEnabled:
                        LaunchStatusText = "Ready: CBP enabled";
                        LaunchStatusColor = Brushes.LimeGreen;
                        LaunchButtonText = "Launch Game";
                        LaunchEnabled = true;
                        LogoCBP = true;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.readyCBPDisabled:
                        LaunchStatusText = "Ready: CBP disabled";
                        LaunchStatusColor = Brushes.Orange;
                        LaunchButtonText = "Launch Game";
                        LaunchEnabled = true;
                        LogoCBP = false; ;
                        LogoRoNEE = true;
                        break;
                    case LauncherStatus.loadFailed:
                        LaunchStatusText = "Error: unable to load CBP";
                        LaunchStatusColor = Brushes.Red;
                        LaunchButtonText = "Retry Unload?";
                        LaunchEnabled = true;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.unloadFailed:
                        LaunchStatusText = "Error: unable to unload CBP";
                        LaunchStatusColor = Brushes.Red;
                        LaunchButtonText = "Retry Unload?";
                        LaunchEnabled = true;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.installFailed:
                        LaunchStatusText = "Error: update failed";
                        LaunchStatusColor = Brushes.Red;
                        LaunchButtonText = "Retry Update?";
                        LaunchEnabled = true;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    // I tried renaming the *Local to *Workshop and VS2019 literally did the opposite of that (by renaming what I just changed) instead of doing what it said it would wtf
                    case LauncherStatus.installingFirstTimeLocal:                       /// primary method: use workshop files;
                        LaunchStatusText = "Installing CBP from local files...";      /// means no local-mods CBP detected
                        LaunchStatusColor = Brushes.Yellow;
                        LaunchEnabled = false;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.installingUpdateLocal:                          /// primary method: use workshop files;
                        LaunchStatusText = "Installing update from local files...";   /// local-mods CBP detected, but out of date compared to workshop version.txt
                        LaunchStatusColor = Brushes.Yellow;
                        LaunchEnabled = false;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.installingFirstTimeOnline:                      /// backup method: use online files;
                        LaunchStatusText = "Installing CBP from online files...";     /// means no local-mods CBP detected but can't find workshop files either
                        LaunchStatusColor = Brushes.Yellow;
                        LaunchEnabled = false;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.installingUpdateOnline:                         /// backup method: use online files; 
                        LaunchStatusText = "Installing update from online files...";  /// local-mods CBP detected, but can't find workshop files and
                        LaunchStatusColor = Brushes.Yellow;                       /// local files out of date compared to online version.txt
                        LaunchEnabled = false;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.connectionProblemLoaded:
                        LaunchStatusText = "Error: connectivity issue (CBP loaded)";
                        LaunchStatusColor = Brushes.OrangeRed;
                        LaunchButtonText = "Launch game";
                        LaunchEnabled = true;
                        LogoCBP = true;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.connectionProblemUnloaded:
                        LaunchStatusText = "Error: connectivity issue (CBP not loaded)";
                        LaunchStatusColor = Brushes.OrangeRed;
                        LaunchButtonText = "Launch game";
                        LaunchEnabled = true;
                        LogoCBP = false;
                        LogoRoNEE = true;
                        break;
                    case LauncherStatus.installProblem:
                        LaunchStatusText = "Potential installation error";
                        LaunchStatusColor = Brushes.OrangeRed;
                        LaunchButtonText = "Launch game";
                        LaunchEnabled = true;
                        LogoCBP = false;
                        LogoRoNEE = false;
                        break;
                    default:
                        break;
                }
            }
        }

        public MainCode()
        {
            if (IsInDesignMode() == false)
            {
                // moved into separate function
                AutoRunWrapper();
            }
            else
            {
                //designtime baybeeee

                //to stop strange A N G E R Y VS2019 error messages which don't actually matter
                CBPFileListAll.Add("uwu");

                //(turns out that didn't stop the messages  n w n
            }
        }

        private DependencyObject dummy = new DependencyObject();

        private bool IsInDesignMode()
        {
            return DesignerProperties.GetIsInDesignMode(dummy);
        }

        private async Task AutoRunWrapper()
        {
            await AutoRun();
            await CreateCommands();
        }

        private async Task AutoRun()
        {
            try
            {
                if (Properties.Settings.Default.UpgradeRequired == true)
                {
                    MessageBox.Show("settings debug");
                    UpgradeSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during initialization: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            try
            {
                ReadRegistry();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading registry: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            RegPathDebug = "Debug: registry read as " + RegPath.ToString();

            /// TODO
            /// use File.Exists and/or Directory.Exists to confirm that CBP files have actually downloaded from Workshop
            /// (at the moment it just assumes they exist and eventually errors later on if they don't)

            try
            {
                AssignZipPath();

                // this starts a cycle through each of the automatic find-path attempts - if all fail, it just prompts user to input the path into a popup box instead
                await FindPathAuto1();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating paths (part 1): {ex}");
                Environment.Exit(0);
            }

            try
            {
                await AssignPaths();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error assigning paths (path 2):" + ex);
                Environment.Exit(0);
            }

            // show detected paths in the UI
            try
            {
                EEPath = RoNPathFinal;
                WorkshopPathDebug = workshopPath;
                WorkshopPathCBPDebug = workshopPathCBP;
                GetLauncherVersion();
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

                //new for late alpha 7; doing some path assigning too because this is the first time directories are created *after* the normal path assignment function
                Directory.CreateDirectory(Path.Combine(EEPath, "CBP")); //used for CBP file storage going forward
                folderCBProot = Path.Combine(RoNPathFinal, "CBP");

                //Directory.CreateDirectory(Path.Combine(folderCBProot, "CBP files")); //modded (CBP) files //decided to just use the existing CBP local mod directory
                Directory.CreateDirectory(Path.Combine(folderCBProot, "Original files")); //copies of the user's original files (which are *not necessarily* RoN:EE's original files)
                Directory.CreateDirectory(Path.Combine(folderCBProot, "CBP files"));
                folderCBPmodded = Path.Combine(folderCBProot, "CBP files");
                folderCBPoriginal = Path.Combine(folderCBProot, "Original files");

                //Directory.CreateDirectory(Path.Combine(folderCBPoriginal, "conquest"));
                //Directory.CreateDirectory(Path.Combine(folderCBPoriginal, "conquest", "Napoleon"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directories {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            try
            {
                await AskDefaultLauncher();

                await AskDefaultCBP();

                // allow user to switch between CBP and unmodded, and if unmodded then CBP updating logic unneeded
                if (Properties.Settings.Default.DefaultCBP == true)
                {
                    await CheckForUpdates();
                };
                if (Properties.Settings.Default.DefaultCBP == false)
                {
                    if (Properties.Settings.Default.CBPUnloaded == false && Properties.Settings.Default.CBPLoaded == true)
                    {
                        await UnloadCBP();
                    }
                    else
                    {
                        Status = LauncherStatus.readyCBPDisabled;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error with primary (old content_rendered) step: {ex}");
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            //CBPDefaultChecker();
        }

        private async Task CreateCommands()
        {
            //RelayCommands (they don't all need to have objects passed to them, but it probably just hands them an unproblematic null in that case, so idk I just kept it)
            CBPDefaultCommand = new RelayCommand(o =>
            {
                CBPDefaultCheckbox_Inversion();
            });

            UsePrereleaseCommand = new RelayCommand(o =>
            {
                UsePrereleaseCheckbox_Inversion();
            });

            UseDefaultLauncherCommand = new RelayCommand(async o =>
            {
                UseDefaultLauncher_Inversion();
                await ReplaceRestoreDefaultLauncher();
            });

            UsePrimaryFilesCommand = new RelayCommand(async o =>
            {
                UsePrimaryFiles_Inversion();
                if (Properties.Settings.Default.CBPLoaded)
                {
                    await GenerateLists();
                    await LoadDirectFiles();
                }
            });

            UseSecondaryFilesCommand = new RelayCommand(async o =>
            {
                UseSecondaryFiles_Inversion();
                if (Properties.Settings.Default.CBPLoaded)//otherwise it can't find the files!
                {
                    await GenerateLists();
                    await LoadDirectFiles();
                }
            });

            DetectBullshitCommand = new RelayCommand(o =>
            {
                DetectBullshit_Inversion();
            });

            /*DetectBullshitNowCommand = new RelayCommand(o =>
            {
                WarnLocalModDataFiles();
                BullshitButtonPress = true;
            });*/

            ResetSettingsCommand = new RelayCommand(o =>
            {
                ResetSettings();
            });

            PlayButtonCommand = new RelayCommand(async o =>
            {
                await PlayButton_Click();
            });

            LoadCBPCommand = new RelayCommand(async o =>
            {
                await CheckForUpdates();
                await ForceUpdatePatchnotes();//otherwise patch notes might not get updated
            });

            UnloadCBPCommand = new RelayCommand(async o =>
            {
                await UnloadCBP();
            });

            WorkshopCommand = new RelayCommand(o =>
            {
                Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=2287791153");
            });

            GithubCommand = new RelayCommand(o =>
            {
                Process.Start("https://github.com/MHLoppy/CBP-Launcher");
            });

            DiscordCommand = new RelayCommand(o =>
            {
                Process.Start("https://discord.gg/wh7YWJgjwf");
            });

            SkinSpartanV1Command = new RelayCommand(o =>
            {
                CurrentSkin = SpartanV1;
            });

            SkinSpartanV1MiniCommand = new RelayCommand(o =>
            {
                CurrentSkin = SpartanV1Mini;
            });

            SkinClassicPlusMiniCommand = new RelayCommand(o =>
            {
                CurrentSkin = ClassicPlusMini;
            });

            SkinClassicPlusCommand = new RelayCommand(o =>
            {
                CurrentSkin = ClassicPlus;
            });

            MinimiseCommand = new RelayCommand(o =>
            {
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
            });

            ExitCommand = new RelayCommand(o =>
            {
                Application.Current.Shutdown();
            });

            // combined with datatemplates in app.xaml, this means the view (skin) is switched when the vm (dummy code in this case) is switched
            // same for each of the tabs
            SpartanV1 = new SpartanV1VM();
            SpartanV1Mini = new SpartanV1MiniVM();
            SpartanV1PatchNotes = new SpartanV1PatchNotesVM();
            SpartanV1ModManager = new SpartanV1ModManagerVM();
            SpartanV1Options = new SpartanV1OptionsVM();
            SpartanV1Log = new SpartanV1LogVM();
            SpartanV1DummyTab = new SpartanV1DummyTabVM();

            ClassicPlus = new ClassicPlusVM();
            ClassicPlusMini = new ClassicPlusMiniVM();
            ClassicPlusPatchNotes = new ClassicPlusPatchNotesVM();
            ClassicPlusModManager = new ClassicPlusModManagerVM();
            ClassicPlusOptions = new ClassicPlusOptionsVM();
            ClassicPlusLog = new ClassicPlusLogVM();
            ClassicPlusDummyTab = new DummyTabVM();

            if (Properties.Settings.Default.SkinSpV1 == true)
            {
                CurrentSkin = SpartanV1;
                CurrentTab = SpartanV1PatchNotes;
            }
            else
            {
                CurrentSkin = ClassicPlus;
                CurrentTab = ClassicPlusPatchNotes;
            }

            ChangeSkinCommand = new RelayCommand(o =>//convert this to a multi-choice command (e.g. dropdown selection)
            {
                if (CurrentSkin == SpartanV1)
                {
                    CurrentSkin = ClassicPlus;
                    CurrentTab = ClassicPlusOptions;

                    // janky but functional for now
                    Properties.Settings.Default.SkinSpV1 = false;
                    SaveSettings();

                    //don't allow resize
                    /*Application.Current.MainWindow.ResizeMode = ResizeMode.NoResize;

                    MessageBox.Show(Application.Current.MainWindow.ResizeMode.ToString());*/
                }
                else
                {
                    CurrentSkin = SpartanV1;
                    CurrentTab = SpartanV1Options;

                    // janky but functional for now
                    Properties.Settings.Default.SkinSpV1 = true;
                    SaveSettings();

                    //allow resize
                    /*Application.Current.MainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                    MessageBox.Show(Application.Current.MainWindow.ResizeMode.ToString());*/
                }
            });

            SpV1TabPatchNotesCommand = new RelayCommand(o =>
            {
                CurrentTab = SpartanV1PatchNotes;
            });

            SpV1TabModManagerCommand = new RelayCommand(o =>
            {
                CurrentTab = SpartanV1ModManager;
            });

            SpV1TabOptionsCommand = new RelayCommand(o =>
            {
                CurrentTab = SpartanV1Options;
            });

            SpV1TabLogCommand = new RelayCommand(o =>
            {
                CurrentTab = SpartanV1Log;
            });

            CPTabPatchNotesCommand = new RelayCommand(o =>
            {
                CurrentTab = ClassicPlusPatchNotes;
            });

            CPTabModManagerCommand = new RelayCommand(o =>
            {
                CurrentTab = ClassicPlusModManager;
            });

            CPTabOptionsCommand = new RelayCommand(o =>
            {
                CurrentTab = ClassicPlusOptions;
            });

            CPTabLogCommand = new RelayCommand(o =>
            {
                CurrentTab = ClassicPlusLog;
            });
        }

        private void AssignZipPath()
        {
            // core paths
            rootPath = Directory.GetCurrentDirectory();
            gameZip = Path.Combine(rootPath, "Community Balance Patch.zip"); //static file name even with updates, otherwise you have to change this value!
        }

        private async Task FindPathAuto1()
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
                        await RoNPathFound();
                    }

                    else
                    {
                        await FindPathAuto2();
                    }
                }
            }
            else
            {
                //a7 temp
                RoNDataPath = Path.Combine(RoNPathFinal, "Data");

                helpXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, helpXML));
                interfaceXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, interfaceXML));
                setupwinXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, setupwinXML));
                patriotsOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "patriots.exe"));
            }
        }

        private async Task FindPathAuto2()
        {
            // try a default 64-bit install path, since that should probably work for most of the users with cursed registries
            RoNPathCheck = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\SteamApps\common\Rise of Nations";

            if (File.Exists(Path.Combine(RoNPathCheck, "riseofnations.exe")))
            {
                // success: automated secondary 1
                await RoNPathFound();
            }
            else
            {
                await FindPathAuto3();
            }
        }

        private async Task FindPathAuto3()
        {
            // old way of doing it, but used as as backup because I don't know if the environment call method ever fails or not
            RoNPathCheck = @"C:\Program Files (x86)\Steam\SteamApps\common\Rise of Nations";

            if (File.Exists(Path.Combine(RoNPathCheck, "riseofnations.exe")))
            {
                // success: automated secondary 2
                await RoNPathFound();
            }

            // automated methods unable to locate RoN install path - ask user for path
            else
            {
                await FindPathManual();
            }
        }

        private async Task FindPathManual()
        {
            //people hate gotos (less so in C# but still) but this seems like a very reasonable substitute for a while-not-true loop that I haven't figured out how to implement here
            AskManualPath:

            RoNPathCheck = Interaction.InputBox($"Please provide the file path to the folder where Rise of Nations: Extended Edition is installed."
                                               + "\n\n" + @"e.g. D:\Steamgames\common\Rise of Nations", "Unable to detect RoN install");

            // check that the user has input a seemingly valid location
            if (File.Exists(Path.Combine(RoNPathCheck, "riseofnations.exe")))
            {
                // success: manual path
                await RoNPathFound();
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

        private async Task AssignPaths()
        {
            // create / find paths for RoN, Steam Workshop, and relevant mods
            gameExe = Path.Combine(RoNPathFinal, "riseofnations.exe"); //in EE v1.20 this is the main game exe, with patriots.exe as the launcher (in T&P main game was rise.exe)

            //just in case the directory doesn't already exist - I'm actually not sure if it's ALWAYS created in a COMPLETELY FRESH install of RoN:EE
            Directory.CreateDirectory(Path.Combine(RoNPathFinal, "mods"));

            //debug (problems with paths seemingly being either 1) no path (as in string "no path", which is the default value), or 2) somehow thinking workshop folder is in steamapps/common/workshop instead of steamapps/workshop
            //MessageBox.Show(RoNPathFinal);

            localMods = Path.Combine(RoNPathFinal, "mods");
            workshopPath = Path.GetFullPath(Path.Combine(RoNPathFinal, @"..\..", @"workshop\content\287450")); //maybe not the best method, but serviceable? Path.GetFullPath used to make final path more human-readable

            modnameCBP = "Community Balance Patch"; // this has to be static, which loses the benefit of having the version display in in-game mod manager, but acceptable because it will display in CBP Launcher instead

            // for testing purposes, access pre-release (of a7)
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
            versionFileCBPLocal = Path.Combine(localPathCBP, "version.txt"); // moved here in order to move with the data files (useful), and better structure to support other mods in future
            versionFileCBPWorkshop = Path.Combine(workshopPathCBP, "Community Balance Patch", "version.txt");

            primaryDataCBP = Path.Combine(localPathCBP, "PrimaryData");
            secondaryDataCBP = Path.Combine(localPathCBP, "SecondaryData");
            primaryNonDataCBP = Path.Combine(localPathCBP, "PrimaryNonData");
            secondarynonDataCBP = Path.Combine(localPathCBP, "SecondaryNonData");
        }

        private async Task CheckForUpdates()
        {
            try // without the try you can accidentally create online-only DRM whoops
            {
                WebClient webClient = new WebClient();                                                               /// Moved this section from reference to here in order to display
                Version onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/version.txt")); /// latest available version as well as installed version

                if (Properties.Settings.Default.UsePrerelease == true)//pr
                {
                    onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/versionpr.txt")); /// latest available version as well as installed version
                }

                VersionTextLatest = "Latest CBP version: "
                     + VersionArray.versionStart[onlineVersion.major]
                     + VersionArray.versionMiddle[onlineVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                     + VersionArray.versionEnd[onlineVersion.subMinor]  ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                     + VersionArray.versionHotfix[onlineVersion.hotfix];

                if (File.Exists(versionFileCBPLocal)) //If there's already a version.txt in the local-mods CBP folder, then...
                {
                    Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal)); // this doesn't use UpdateLocalVersionNumber() because of the compare done below it - will break if replaced without modification

                    VersionTextInstalled = "CBP "
                                            + VersionArray.versionStart[localVersion.major]
                                            + VersionArray.versionMiddle[localVersion.minor]
                                            + VersionArray.versionEnd[localVersion.subMinor]
                                            + VersionArray.versionHotfix[localVersion.hotfix];
                    try
                    {
                        if (onlineVersion.IsDifferentThan(localVersion))
                        {
                            await OldInstallGameFiles(true, onlineVersion);
                            //GenerateLists();
                            //LoadDirectFiles();
                            //oldinstallgamefiles already includes both of those if the bool is true, so no need to repeat it
                        }
                        else
                        {
                            await GenerateLists();
                            await LoadDirectFiles();
                            
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
                    await OldInstallGameFiles(true, Version.zero);
                }

                //this will only run if the local version file (if it exists) is not different to the online one
                else if (File.Exists(Path.Combine(unloadedModsPath, "Community Balance Patch", "version.txt")))
                {
                    Version localVersion = new Version(File.ReadAllText(Path.Combine(unloadedModsPath, "Community Balance Patch", "version.txt")));
                    await OldInstallGameFiles(false, Version.zero);
                    await GenerateLists();
                    await LoadDirectFiles();
                }

                else
                {
                    await OldInstallGameFiles(false, Version.zero);
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
                VersionTextLatest = "Error checking version";
                MessageBox.Show($"Error checking for updates. Maybe no connection could be established? {ex}");
            }
        }

        // this is specifically to warn about the absolutely asinine damage bug (which can cause OoS's) that I discovered with Barks and Triremes - check their RoN wiki page X_X
        // no longer needed because I found a fix to the bug instead of needing grating workarounds
        /*private void WarnLocalModDataFiles()
        {
            //filter this at the top so that people who've turned it off are least affected by any performance hit / annoyance
            if (Properties.Settings.Default.DetectBullshit || Properties.Settings.Default.FirstTimeRun)
            {
                try
                {
                    // get a list of the subfolders in the local mods folder, i.e. a list of local mods
                    List<string> searchThisList = new List<string>(Directory.GetDirectories(localMods, "*", SearchOption.TopDirectoryOnly));
                    string modList = "";
                    bool sendWarning = false;

                    // remove false positives from CBP (which is now avoiding using data files in this location) and unloaded mods (which avoids the loading path)
                    //if (searchThisList.Contains(localPathCBP))
                    //    searchThisList.Remove(localPathCBP);//actually now that the mod format has been updated, there are actually no /Data files there
                    if (searchThisList.Contains(unloadedModsPath))
                        searchThisList.Remove(unloadedModsPath);

                    // if there's any subfolders left with XML files...
                    foreach (string modFolder in searchThisList)
                    {
                        //we want to check only for XML files in a /Data directory, because otherwise there'll be false positives
                        string modDataFolder = Path.Combine(modFolder, "Data");
                        string modArtFolder = Path.Combine(modFolder, "art");// not sure if the search is case sensitive or not

                        //this is a ~maybe~ overall better (possibly faster at same folders, but can also be slower *but* search ALL the folders maybe?)
                        /*List<string> filteredFiles = Directory
                                                     .EnumerateFiles(modFolder)
                                                     .Where(file => file.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase)
                                                                 || file.EndsWith("tga", StringComparison.InvariantCultureIgnoreCase))
                                                     .ToList();

                        foreach (string s in filteredFiles)
                            MessageBox.Show(s);*/

                        //also filter out potential dropdown mods (info.xml in root of mod folder), because those aren't loaded by default
                        /*if (Directory.Exists(modDataFolder) && !File.Exists(Path.Combine(modFolder, "info.xml")))
                        {
                            if (Directory.GetFiles(modDataFolder, "*.xml", SearchOption.TopDirectoryOnly).Length == 0
                             && Directory.GetFiles(modArtFolder, "*.tga", SearchOption.AllDirectories).Length == 0) { }//if no match, all good
                            else
                            {
                                //but if there is a match, add the path of that mod to the list and turn on the flag which sends the message
                                modList += (modFolder + "\n\n");
                                sendWarning = true;
                            }
                        }
                    }

                    if (sendWarning)
                        MessageBox.Show("These local mods *may* contain files which trigger a known bug in the first game of every session: " + "\n\n" + modList
                                        //+ "This bug can cause OoS issues in the first game of every session you play.\n\n"
                                        + "To prevent this issue, either move/remove those mods, "
                                        + "or make sure you ALWAYS start and quit from one game before playing any \"real\" games."
                                        , "Potential OoS issue detected"
                                        , MessageBoxButton.OK
                                        , MessageBoxImage.Warning);

                    //we only want this message to show on button press, not on automatic checks
                    else if (BullshitButtonPress)
                    {
                        BullshitButtonPress = false;
                        MessageBox.Show("No TGA art files or XML data files detected in local mods folder. Note that there are still other, less common files which could still cause the problem.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error with XML detection for bark/trireme OoS bug: " + ex);
                    // not instaclosing because it should be a relatively non-problematic error.. hopefully
                }
            }
            //else do nothing
        }*/

        /*private void NewInstallGameFiles(bool _isUpdate, Version _onlineVersion)//end of night comment: probably just keep the old one (..for now), meaning that some stuff such as archiving doesn't need to be here too
        {         //later on can refactor the whole thing maybe
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                if (Properties.Settings.Default.NoWorkshopFiles == false)
                {
                    //load CBP files from workshop files
                    if (_isUpdate)
                    {
                        //if it's an update, we want to archive the old CBP
                    }
                    //then do logic-y stuff regardless of archive or not
                    // e.g. CopyToCBPFolder();
                }
                else if (Properties.Settings.Default.NoWorkshopFiles == true)
                {
                    //load CBP files from online source
                }
            }
            if (Properties.Settings.Default.CBPUnloaded == true)
            {
                //load normal files
            }
        }*/

        //using the generated modded and original lists, check then (if needed) load each (appropriate modded or original) file in each list
        private async Task LoadDirectFiles()
        {
            // check each file in the modded list and make sure it's an up-to-date CBP file
            Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal));//at some point this should definitely be spun out into a less-localised variable so that it can be used in multiple places

            foreach (string filename in CBPFileListModded)
            {
                if (CheckIfCBPFile(filename) == true)
                {
                    //it's a CBP file (but not necessarily the right version, so deal with that too)
                    string text = File.ReadLines(Path.Combine(RoNDataPath, filename)).Skip(2).Take(1).First();
                    Version fileVersion = new Version(text.Substring(9, 11));

                    if (fileVersion.IsDifferentThan(localVersion))// I assume it's faster to check this than straight up always-write files
                    {
                        await ActuallyLoadFiles(filename);
                    }
                    //else no action required
                }
                else
                {
                    await ActuallyLoadFiles(filename);
                }
            }

            // check each file in the original list and make sure it's **NOT** a CBP file (we don't care if it's user-modded, since they did that themselves)
            foreach (string filename in CBPFileListOriginal)
            {
                if (CheckIfCBPFile(filename) == true)
                {
                    //replace it from the backup that was previously copied (not necessarily copied in this session)
                    File.Copy(Path.Combine(folderCBPoriginal, filename), Path.Combine(RoNDataPath, filename), true);
                }
                else
                {
                    //*chef's kiss* no action required
                }
            }
        }

        private async Task ActuallyLoadFiles(string filename)
        {
            if (File.Exists(Path.Combine(primaryDataCBP, filename)))
                File.Copy(Path.Combine(primaryDataCBP, filename), Path.Combine(RoNDataPath, filename), true);

            else if (File.Exists(Path.Combine(secondaryDataCBP, filename)))
                File.Copy(Path.Combine(secondaryDataCBP, filename), Path.Combine(RoNDataPath, filename), true);

            // in theory (shouldn't *actually* happen but ya know) you could have scuffed files in neither list, but for now it's not handled
        }

        //unneeded because Bark/Trireme OoS bug fixed (the code relating to all the non-data loading was not completed, although individual functions may be working)

        //semi-forced into doing shitty hardcoded lists because of the sheer scope of the bark/trireme bug - I don't have the skill to do it dynamically, or the time to figure it out for just a few files
        /*private void LoadNonDataFiles()
        {
            // these would make more sense as global variables but this is tolerable for now
            string conquestCBP = Path.Combine(Path.Combine(secondarynonDataCBP, "conquest"));
            string conquestEE = Path.Combine(Path.Combine(RoNPathFinal, "conquest"));

            string napoleonMap = File.ReadLines(Path.Combine(conquestCBP, "CTW_Napoleon_Map_01.xml")).Skip(3).Take(1).First();
            if (napoleonMap.Substring(5).StartsWith("CBP") == false)
                File.Copy(Path.Combine(conquestCBP, "CTW_Napoleon_Map_01.xml"), Path.Combine(conquestEE, "CTW_Napoleon_Map_01.xml"), true);

            string worldMap = File.ReadLines(Path.Combine(conquestCBP, "CTW_World_Map_01.xml")).Skip(2).Take(1).First();
            if (worldMap.Substring(5).StartsWith("CBP") == false)
                File.Copy(Path.Combine(conquestCBP, "CTW_World_Map_01.xml"), Path.Combine(conquestEE, "CTW_World_Map_01.xml"), true);

            //bhs file, different syntax etc from xml
            string napoleonPostTurn = File.ReadLines(Path.Combine(conquestCBP, "Napoleon", "napoleon_post_turn.bhs")).Skip(0).Take(1).First();
            if (napoleonPostTurn.Substring(2).StartsWith("CBP") == false)
                File.Copy(Path.Combine(conquestCBP, "napoleon_post_turn.bhs"), Path.Combine(conquestEE, "napoleon_post_turn.bhs"), true);
        }*/


        /*private void CopyArtFiles()
        {
            //copy art files from e.g. /mods/Community Balance Patch/Art/art/*
            // /mods/Community Balance Patch/Art/art/snow/*

            // to /art/xxxx and /art/snow/*

            // again, should be global strings but clenched teeth for now

            if (Properties.Settings.Default.ArtFilesCopied == false)
            {
                try
                {
                    string artEE = Path.Combine(RoNPathFinal, "art");
                    string snowEE = Path.Combine(RoNPathFinal, "art", "snow");
                    string artCBP = Path.Combine(localPathCBP, "Art Files", "art");
                    string snowCBP = Path.Combine(RoNPathFinal, "Art Files", "art", "snow");

                    // this does the /art/ folder
                    string[] artFilesArray = Directory.GetFiles(artCBP);
                    foreach (string artFile in artFilesArray)
                    {
                        // because these files could be updated over time, we should copy/overwrite all of them if the flag says so (otherwise there's complicated stuff about tracking versions etc)
                        File.Copy("source", "destination", true);
                        // log the copy I guess
                    }

                    // this does the art/snow folder
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while copying art files: " + ex);
                }
            }
            else
            {
                //log that
            }
        }*/

        private async Task UnloadDirectFiles()
        {
            //for every file, check it and then (if needed) load the original file
            foreach (string filename in CBPFileListAll)
            {
                if (CheckIfCBPFile(filename) == true)
                {
                    File.Copy(Path.Combine(folderCBPoriginal, filename), Path.Combine(RoNDataPath, filename), true);
                }
                //else no action required
            }
        }
        
        /*//semi-forced into doing shitty hardcoded lists because of the sheer scope of the bark/trireme bug - I don't have the skill to do it dynamically, or the time to figure it out for just a few files
        private void UnloadNonDataFiles()
        {
            // these would make more sense as global variables but this is tolerable for now
            string conquestOriginal = Path.Combine(Path.Combine(folderCBPoriginal, "conquest"));
            string conquestEE = Path.Combine(Path.Combine(RoNPathFinal, "conquest"));

            string napoleonMap = File.ReadLines(Path.Combine(conquestOriginal, "CTW_Napoleon_Map_01.xml")).Skip(2).Take(1).First();
            if (napoleonMap.Substring(5).StartsWith("CBP"))
                File.Copy(Path.Combine(conquestOriginal, "CTW_Napoleon_Map_01.xml"), Path.Combine(conquestEE, "CTW_Napoleon_Map_01.xml"), true);

            string worldMap = File.ReadLines(Path.Combine(conquestOriginal, "CTW_World_Map_01.xml")).Skip(2).Take(1).First();
            if (worldMap.Substring(5).StartsWith("CBP"))
                File.Copy(Path.Combine(conquestOriginal, "CTW_World_Map_01.xml"), Path.Combine(conquestEE, "CTW_World_Map_01.xml"), true);

            //bhs file, different syntax etc from xml
            string napoleonPostTurn = File.ReadLines(Path.Combine(conquestOriginal, "Napoleon", "napoleon_post_turn.bhs")).Skip(0).Take(1).First();
            if (napoleonPostTurn.Substring(2).StartsWith("CBP"))
                File.Copy(Path.Combine(conquestOriginal, "napoleon_post_turn.bhs"), Path.Combine(conquestEE, "napoleon_post_turn.bhs"), true);
        }*/

        private bool CheckIfCBPFile(string filename)
        {
            // go to line 3, read 1 line, from start, then skip the (potentially) "<!-- " XML comment part and read what's there
            string text = File.ReadLines(Path.Combine(RoNDataPath, filename)).Skip(2).Take(1).First();
            if (text.Substring(5).StartsWith("CBP"))
                return true;
            else
                return false;
        }

        // just dumping this here - somewhere need to [if not null] do     CBPFileList = Properties.Settings.Default.SavedFileListCBP.Cast<string>().ToList();

        //generate complete list of (original) files based on which CBP files exist so that we know what to backup
        private async Task GenerateFileListAll()
        {
            //apparently using FileInfo (.Name) is much (non-trivially) heavier than Path.GetFileName

            //primary files
            string[] primaryFiles = Directory.GetFiles(primaryDataCBP);
            foreach (string filename in primaryFiles)
            {
                CBPFileListAll.Add(
                    //"Primary file: " + 
                    Path.GetFileName(filename));//don't actually put the primary file prefix in the list!
            }

            //secondary files
            string[] secondaryFiles = Directory.GetFiles(secondaryDataCBP);
            foreach (string filename in secondaryFiles)
            {
                CBPFileListAll.Add(
                    //"Secondary file: " + 
                    Path.GetFileName(filename));//don't actually put the secondary file prefix in the list!
            }

            //custom file list logic can go here

            //debug
            /*foreach (string filename in CBPFileListAll)
            {
                Console.WriteLine(filename);//change to log later
            }*/
        }

        // before loading/unload files, need copies of the originals
        private async Task BackupOriginalFiles()
        {
            // check if user is running modded files first? Or just assume that they're okay? maybe check for the CBP "(original)" marked files and assume user is running CBP if they exist?

            if (File.Exists(helpXMLOrig + " (old)"))
            {
                //looks like user has old version of CBP loaded, so we can't use these files
                MessageBox.Show("It looks like you currently have a pre-release version of CBP files loaded (from before PR6). Will now attempt to unload these files before continuing. (if you see this message repeatedly, ask for help)");

                try
                {
                    prereleaseFilesDetected = true;
                    //UnloadCBP();//had to replace with "custom" unloading, because this seems to trip a system.io access denied error (probably because I still want access to those files)

                    File.Delete(helpXMLOrig);
                    File.Delete(interfaceXMLOrig);
                    File.Delete(setupwinXMLOrig);
                    if (File.Exists(Path.Combine(patriotsOrig, " (original)")) && Properties.Settings.Default.UseDefaultLauncher == true)//this (MAYBE???) handles the use case that the user checked the setting AFTER the function handling it already finished
                    { File.Delete(patriotsOrig); }
                    File.Move(helpXMLOrig + " (old)", helpXMLOrig);
                    File.Move(interfaceXMLOrig + " (old)", interfaceXMLOrig);
                    File.Move(setupwinXMLOrig + " (old)", setupwinXMLOrig);
                    if (File.Exists(Path.Combine(patriotsOrig, " (original)")) && Properties.Settings.Default.UseDefaultLauncher == true)
                    { File.Move(patriotsOrig + " (original)", patriotsOrig); }

                    Properties.Settings.Default.OldFilesRenamed = false;//yes I know this is almost definitely now redundant
                    SaveSettings();

                    prereleaseFilesDetected = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not unload old CBP files automatically. Unable to continue. " + ex);
                    Environment.Exit(-1);
                }
                await CheckForUpdates();
            }

            if (Properties.Settings.Default.FilesBackedUp == false)
            {
                //copy files from e.g. /data/ to /CBP/Original Files
                try
                {
                    //data files (primary and secondary)
                    foreach (string filename in CBPFileListAll)
                    {
                        if (!File.Exists(Path.Combine(folderCBPoriginal, filename)))
                            File.Copy(Path.Combine(RoNDataPath, filename), Path.Combine(folderCBPoriginal, filename));//if this fails partway then maybe need a way to overwrite (or at least delete) what's there
                        else MessageBox.Show(filename + " already has a backup file so has been skipped.");
                    }

                    Properties.Settings.Default.FilesBackedUp = true;
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem backing up data files. " + ex);
                    Environment.Exit(-1);
                }
            }

            else if (Properties.Settings.Default.FilesBackedUp == true)
            {
                //log that
            }

            /*//I hate this but for 3 files I can live with it
            if (Properties.Settings.Default.NonDataFilesBackedUp == false)
            {
                try
                {
                    // conquest/CTW_Napoleon_Map_01.xml
                    string nMap = "CTW_Napoleon_Map_01.xml";
                    if (!File.Exists(Path.Combine(folderCBPoriginal, "conquest", nMap)))
                        File.Copy(Path.Combine(RoNPathFinal, "conquest", nMap), Path.Combine(folderCBPoriginal, "conquest", nMap));
                    else MessageBox.Show("Backup of Napoleon Map skipped (file already exists)");

                    // conquest/CTW_World_Map_01.xml
                    string wMap = "CTW_World_Map_01.xml";
                    if (!File.Exists(Path.Combine(folderCBPoriginal, "conquest", wMap)))
                        File.Copy(Path.Combine(RoNPathFinal, "conquest", wMap), Path.Combine(folderCBPoriginal, "conquest", wMap));
                    else MessageBox.Show("Backup of World Map skipped (file already exists)");

                    // conquest/Napoleon/napoleon_post_turn.bhs
                    string nPost = "napoleon_post_turn.bhs";
                    if (!File.Exists(Path.Combine(folderCBPoriginal, "conquest", "Napoleon", nPost)))
                        File.Copy(Path.Combine(RoNPathFinal, "conquest", "Napoleon", nPost), Path.Combine(folderCBPoriginal, "conquest", "Napoleon", nPost));
                    else MessageBox.Show("Backup of Napoleon postturn skipped (file already exists)");

                    Properties.Settings.Default.NonDataFilesBackedUp = true;
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem backing up non-data files. " + ex);
                    Environment.Exit(-1);
                }
            }

            else if (Properties.Settings.Default.NonDataFilesBackedUp == true)
            {
                //log that
            }*/
        }

        private async Task GenerateFileListModded()
        {
            if (Properties.Settings.Default.UsePrimaryFileList == true)
            {
                string[] primaryFiles = Directory.GetFiles(primaryDataCBP);
                //CBPFileListModded.AddRange(primaryFiles); //this gives the full file path, which can be handy but we just want the filename itself
                foreach (string name in primaryFiles)
                {
                    CBPFileListModded.Add(Path.GetFileName(name));
                    //MessageBox.Show("added " + name);//change to log event later
                }
            }
            if (Properties.Settings.Default.UseSecondaryFileList == true)
            {
                string[] secondaryFiles = Directory.GetFiles(secondaryDataCBP);
                //CBPFileListModded.AddRange(secondaryFiles); //this gives the full file path, which can be handy but we just want the filename itself
                foreach (string name in secondaryFiles)
                {
                    CBPFileListModded.Add(Path.GetFileName(name));
                    //MessageBox.Show("added " + name + "to CBPFileListModded");//change to log event later
                }
            }
            //after that, handle custom lists (where primary and/or secondary are turned off and individual files are loaded/unloaded instead)
        }

        private async Task GenerateFileListOriginal()
        {
            IEnumerable<string> differencequery = CBPFileListAll.Except(CBPFileListModded);
            foreach (string name in differencequery)
            {
                CBPFileListOriginal.Add(name);
                //MessageBox.Show("Added " + name + "to CBPFileListOriginal"); //change to log event later
            }
        }

        private async Task OldInstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                if (Properties.Settings.Default.NoWorkshopFiles == false)
                {
                    // try using workshop files
                    try
                    {
                        // check if the local version is the same as the workshop version (even though online version is different)
                        if (File.Exists(versionFileCBPLocal) && File.Exists(versionFileCBPWorkshop))
                        {
                            // get local version, workshop version, and online version (online version is different depending on whether using pre-release or not)
                            Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal));
                            Version workshopVersion = new Version(File.ReadAllText(versionFileCBPWorkshop));
                            WebClient webClient = new WebClient();                                                               /// Moved this section from reference to here in order to display
                            Version onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/version.txt")); /// latest available version as well as installed version

                            if (Properties.Settings.Default.UsePrerelease == true)//pr
                            {
                                onlineVersion = new Version(webClient.DownloadString("http://mhloppy.com/CBP/versionpr.txt")); /// latest available version as well as installed version
                            }

                            // if the local and workshop versions are the same BUT the workshop version is different, then alert the user
                            if ((!localVersion.IsDifferentThan(workshopVersion)) && localVersion.IsDifferentThan(onlineVersion))// I assume it's faster to check this than straight up always-write files
                            {
                                string newestVersion = VersionArray.versionStart[onlineVersion.major]
                                                     + VersionArray.versionMiddle[onlineVersion.minor]
                                                     + VersionArray.versionEnd[onlineVersion.subMinor]
                                                     + VersionArray.versionHotfix[onlineVersion.hotfix];

                                MessageBox.Show(newestVersion + " has been published on Steam Workshop, but Steam hasn't downloaded the new files yet so CBP Launcher is unable to install them.");
                            }
                        }

                        // extra steps depending on whether this an update to existing install or first time install
                        if (_isUpdate)
                        {
                            Status = LauncherStatus.installingUpdateLocal;

                            // if archive setting is enabled, archive the old version; it looks for an unversioned CBP folder and has a separate check for a6c specifically
                            if (Properties.Settings.Default.CBPArchive == true)
                            {
                                // may need a third archive function with new mod format?

                                // standard (non-a6c) archiving
                                if (Directory.Exists(Path.Combine(localPathCBP)))
                                {
                                    await ArchiveNormal();
                                }

                                // compatibility with archiving a6c
                                else if (Directory.Exists(Path.Combine(localMods, "Community Balance Patch (Alpha 6c)")))
                                {
                                    await ArchiveA6c();
                                }

                                else
                                {
                                    MessageBox.Show($"Archive setting is on, but there doesn't seem to be any compatible CBP install to archive. No action has been taken.");
                                }
                            }
                            else
                            {
                                //todo: just delete the old files instead
                            }
                        }
                        else
                        {
                            Status = LauncherStatus.installingFirstTimeLocal;
                        }

                        if (abortWorkshopCopyCBP == false)
                        {
                            DirectoryCopy(Path.Combine(workshopPathCBP, "Community Balance Patch"), Path.Combine(localPathCBP), true);
                        }
                        else
                        {
                            Console.WriteLine("did not copy workshop CBP to local CBP because of archive abort flag");
                        }

                        if (Properties.Settings.Default.UseDefaultLauncher == false)
                        {
                            //keep CBP Setup GUI up to date
                            if (Process.GetProcessesByName("patriots").Length < 1)
                                File.Copy(Path.Combine(workshopPathCBP, "CBPSetupGUI.exe"), patriotsOrig, true);//should make sure it's closed first? maybe do a version check too?
                            else
                            {
                                //set a flag to do it later so that user doesn't get slowed down
                                updateSetupLater = true;
                            }
                        }

                        await ReplaceRestoreDefaultLauncher();

                        try
                        {
                            await GenerateLists();
                            await LoadDirectFiles();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error generating lists / directly loading files: {ex}");
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
                    if (Directory.Exists(Path.Combine(localPathCBP)) == false)
                    {
                        Directory.Move(Path.Combine(unloadedModsPath, "Community Balance Patch"), Path.Combine(localPathCBP));

                        await GenerateLists();
                        await ReplaceRestoreDefaultLauncher();
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

                File.WriteAllText(versionFileCBPLocal, onlineVersionString); // I thought this is where return would go, but it doesn't, so I evidently don't know what I'm doing

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

        private async Task UnloadCBP()
        {
            if (Properties.Settings.Default.CBPUnloaded == false)
            {
                try
                {
                    if (await CheckIfSetupRunning() == false)
                    {
                        await GenerateLists();

                        Directory.Move(localPathCBP, Path.Combine(unloadedModsPath, "Community Balance Patch"));
                        Properties.Settings.Default.CBPUnloaded = true;
                        Properties.Settings.Default.CBPLoaded = false;
                        SaveSettings();

                        VersionTextInstalled = "CBP not loaded";

                        try
                        {
                            await UnloadDirectFiles();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error directly unloading files: {ex}");
                        }

                        Status = LauncherStatus.readyCBPDisabled;
                    }
                    else
                    {
                        MessageBox.Show($"CBP Setup GUI (patriots.exe) doesn't seem to be closing, so CBP Launcher will be closed.");
                        Status = LauncherStatus.unloadFailed;
                        Environment.Exit(-1);
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

        // abstraction layer just makes it so I can (optionally) separate some of the logic between functions, or do things e.g. async
        private async Task<bool> CheckIfSetupRunning()//the bool isn't used, but I'm keeping it just because it kinda helps readability
        {
            if (Process.GetProcessesByName("patriots").Length > 0 && Properties.Settings.Default.UseDefaultLauncher == false)
            {
                // saying the message (and requiring the OK to be pressed to continue) seems more confusing than just saying nothing and delaying for a couple seconds
                //Action sayWaiting = new Action(() =>{ MessageBox.Show($"Waiting for CBP Setup GUI (patriots.exe) to close"); });
                //await Dispatcher.CurrentDispatcher.BeginInvoke(sayWaiting);

                //try to check on a loop whether CBP Setup GUI is running
                int i = 0;
                while (i <10)//if we're waiting more than 7.2 seconds, something is wrong because it should close in ~5 seconds max
                {
                    await Delay(800);

                    if (Process.GetProcessesByName("patriots").Length > 0)
                    {
                        i++;
                        // fascinating difference between ++i and i++
                        // https://stackoverflow.com/questions/3346450/what-is-the-difference-between-i-and-i
                    }
                    else
                        return false;
                }

                //something has gone wrong
                return true;
            }
            return false;
        }

        private void UpdateLocalVersionNumber()
        {
            if (File.Exists(versionFileCBPLocal))
            {
                Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal)); // moved to separate thing to reduce code duplication

                VersionTextInstalled = "CBP "
                                        + VersionArray.versionStart[localVersion.major]
                                        + VersionArray.versionMiddle[localVersion.minor]  ///space between major and minor moved to the string arrays in order to support the eventual 1.x release(s)
                                    + VersionArray.versionEnd[localVersion.subMinor]      ///it's nice to have a little bit of forward thinking in the mess of code sometimes ::fingerguns::
                                    + VersionArray.versionHotfix[localVersion.hotfix];
            }
            else
            {
                VersionTextInstalled = "CBP not installed";
            }
        }

        private void ReadRegistry() // apparently this is not a good method for this? use using instead? (but I don't know how to make that work with the bit-check :( ) https://stackoverflow.com/questions/1675864/read-a-registry-key
        {
            try
            {
                if (Environment.Is64BitOperatingSystem) //I don't *fully* understand what's going on here (ported from stackexchange), but this block seems to be needed to prevent null value return due to 32/64 bit differences???
                {
                    RegPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                }
                else
                {
                    RegPath = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                }

                RegPathDebug = "Debug: registry read as " + RegPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error with ReadRegistry:" + ex);
            }
        }

        private async Task PlayButton_Click()
        {
            //legacy: from when Bark/Trireme OoS detection was needed
            //the setting that triggers this is toggled off after the first time the launcher is run
            //WarnLocalModDataFiles();
            //Properties.Settings.Default.FirstTimeRun = false;
            //SaveSettings();

            if (File.Exists(gameExe) && Status == LauncherStatus.readyCBPEnabled || Status == LauncherStatus.readyCBPDisabled) // make sure all "launch" button options are included here
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe) // if you do this wrong (I don't fully remember what "wrong" was) the game can launch weirdly e.g. errors, bad mod loads etc.
                {
                    WorkingDirectory = RoNPathFinal //this change compared to reference app was suggested by VS itself - I'm assuming it's functionally equivalent at worst
                };
                Process.Start(startInfo);
                //DEBUG: Process.Start(gameExe);

                if (updateSetupLater == true)
                {
                    await Delay(3000);
                    if (Process.GetProcessesByName("patriots").Length < 1)
                        File.Copy(Path.Combine(workshopPathCBP, "CBPSetupGUI.exe"), patriotsOrig, true);//should make sure it's closed first? maybe do a version check too?
                    else
                    {
                        await Delay(3000);
                        if (Process.GetProcessesByName("patriots").Length < 1)
                            File.Copy(Path.Combine(workshopPathCBP, "CBPSetupGUI.exe"), patriotsOrig, true);
                        else
                            MessageBox.Show("CBP Setup GUI was not updated (if you rarely see this message you can probably ignore it)");
                    }
                }

                //should debug log a line here, because oddly enough this sometimes doesn't seem to trigger
                Application.Current.MainWindow.Close();
            }
            else if (Status == LauncherStatus.installFailed)
            {
                await CheckForUpdates(); // because CheckForUpdates currently includes the logic for *all* installing/loading, it's used for both installFailed and loadFailed right now
            }
            else if (Status == LauncherStatus.loadFailed)
            {
                await CheckForUpdates();
            }
            else if (Status == LauncherStatus.unloadFailed)
            {
                await UnloadCBP();
            }
        }

        private async Task ForceUpdatePatchnotes()
        {
            // temporarily save the current tab (patch notes), change the tab to a dummy tab (of same background color as the flowdoc background), then immediately swap back to the original tab (patch notes)
            if (CurrentTab == ClassicPlusPatchNotes)
            {
                object tab = CurrentTab;
                CurrentTab = ClassicPlusDummyTab;
                await Delay(1);//without this it seems like it doesn't work lol
                CurrentTab = tab;
            }
            if (CurrentTab == SpartanV1PatchNotes)
            {
                object tab = CurrentTab;
                CurrentTab = SpartanV1DummyTab;
                await Delay(1);//without this it seems like it doesn't work lol
                CurrentTab = tab;
            }
        }

        private void ResetSettings()
        {
            Properties.Settings.Default.Reset();

            MessageBox.Show($"Settings reset. Default settings will be loaded the next time the program is loaded.");
        }

        private void CBPDefaultCheckbox_Inversion()
        {
            Properties.Settings.Default.DefaultCBP = !Properties.Settings.Default.DefaultCBP;
            SaveSettings();
        }

        private void UsePrereleaseCheckbox_Inversion()
        {
            Properties.Settings.Default.UsePrerelease = !Properties.Settings.Default.UsePrerelease;
            SaveSettings();
        }

        private void UseDefaultLauncher_Inversion()
        {
            Properties.Settings.Default.UseDefaultLauncher = !Properties.Settings.Default.UseDefaultLauncher;
            SaveSettings();
        }

        private void UsePrimaryFiles_Inversion()
        {
            Properties.Settings.Default.UsePrimaryFileList = !Properties.Settings.Default.UsePrimaryFileList;
            SaveSettings();
        }

        private void UseSecondaryFiles_Inversion()
        {
            Properties.Settings.Default.UseSecondaryFileList = !Properties.Settings.Default.UseSecondaryFileList;
            SaveSettings();
        }

        private void DetectBullshit_Inversion()
        {
            Properties.Settings.Default.DetectBullshit = !Properties.Settings.Default.DetectBullshit;
            SaveSettings();
        }

        private async Task ReplaceRestoreDefaultLauncher()
        {
            //IMPLEMENTATION INCOMPLETE (also for the first-time-run CBP-or-not question)
            
            //restore old launcher
            if (File.Exists(patriotsOrig + " (original)") && Properties.Settings.Default.UseDefaultLauncher == true)
            {
                try
                {
                    if (await CheckIfSetupRunning() == false)
                    {
                        //delete local copy of CBP Setup GUI (which has been renamed to patriots.exe), then restore the old patriots.exe (the original launcher)
                        File.Delete(patriotsOrig);
                        File.Move(patriotsOrig + " (original)", patriotsOrig);

                        MessageBox.Show("Have attempted to restore original launcher - it should be active next time RoN is started. To use CBP Launcher again re-check this box or re-run first time setup and then choose the appropriate option(s).");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error restoring original launcher: " + ex);
                }
            }

            //replace old launcher with CBP Setup GUI
            if (!File.Exists(patriotsOrig + " (original)") && Properties.Settings.Default.UseDefaultLauncher == false)
            {
                try
                {
                    if (await CheckIfSetupRunning() == false)
                    {
                        //rename the original launcher and then replace it with CBP Setup GUI (but renamed to patriots.exe)
                        File.Move(patriotsOrig, patriotsOrig + " (original)");
                        File.Copy(Path.Combine(workshopPathCBP, "CBPSetupGUI.exe"), patriotsOrig);

                        MessageBox.Show("Have attempted to replace original launcher - CBP Launcher should be active when RoN is started.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error replacing original launcher: " + ex);
                }
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        private void UpgradeSettings()
        {
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.UpgradeRequired = false;
            SaveSettings();
        }

        private async Task AskDefaultLauncher()
        {
            if (Properties.Settings.Default.DefaultLauncherAnswered == false)
            {
                string message = $"Do you want CBP Launcher to replace the default launcher?\n(This option can be changed at any time)";

                if (MessageBox.Show(message, "Default to CBP Launcher?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Properties.Settings.Default.DefaultLauncherAnswered = true;
                    Properties.Settings.Default.UseDefaultLauncher = false;
                    SaveSettings();
                    await ReplaceRestoreDefaultLauncher();
                }
                else
                {
                    Properties.Settings.Default.DefaultLauncherAnswered = true;
                    Properties.Settings.Default.UseDefaultLauncher = true;
                    SaveSettings();
                }
            }
        }

        private async Task AskDefaultCBP()
        {
            if (Properties.Settings.Default.FirstTimeRun == true)//this is a different variable right now almost purely because of implementation timing
            {
                string message = $"Do you want CBP to be loaded by default when CBP Launcher starts?"
                               + "\n\n(CBP can be manually loaded or unloaded freely regardless of this answer, and this setting can be changed later)";

                if (MessageBox.Show(message, "Default to CBP?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Properties.Settings.Default.FirstTimeRun = false;
                    Properties.Settings.Default.DefaultCBP = true;
                    SaveSettings();
                }
                else
                {
                    Properties.Settings.Default.FirstTimeRun = false;
                    Properties.Settings.Default.DefaultCBP = false;
                    SaveSettings();
                }
            }
        }

        private async Task RoNPathFound()
        {
            if (RoNPathFinal == $"no path")
            {
                MessageBox.Show($"Rise of Nations detected in " + RoNPathCheck);
            }
            RoNPathFinal = RoNPathCheck;
            RoNDataPath = Path.Combine(RoNPathFinal, "Data");

            Properties.Settings.Default.RoNPathSetting = RoNPathFinal;
            SaveSettings();

            //a7 temp
            helpXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, helpXML));
            interfaceXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, interfaceXML));
            setupwinXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, setupwinXML));
            patriotsOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "patriots.exe"));
        }

        private async Task ArchiveNormal()
        {
            try
            {
                // for each folder in archive folder, read version.txt
                // if it matches local version.txt, don't archive because the versions are the same

                DirectoryInfo di = new DirectoryInfo(archiveCBP);
                DirectoryInfo[] diArr = di.GetDirectories();
                List<string> archivedVersions = new List<string>();

                foreach (DirectoryInfo dri in diArr)
                {
                    //MessageBox.Show(dri.FullName);
                    archivedVersions.Add(File.ReadAllText(Path.Combine(dri.FullName, "version.txt")));
                }

                //MessageBox.Show(string.Join(", ", archivedVersions));
                //MessageBox.Show(versionFileCBP);

                bool versionExists = archivedVersions.Contains(File.ReadAllText(versionFileCBPLocal));

                if (versionExists == false)
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
                else
                {
                    //log
                    MessageBox.Show("It looks like the version to archive already exists, so no action has been taken.");
                    abortWorkshopCopyCBP = true;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                MessageBox.Show($"Error archiving previous CBP version: {ex}");
            }
        }

        // can't use same version check because it uses a 3-digit identifier, not 4-digit, but since we know its name it's not too bad
        private async Task ArchiveA6c()
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
                MessageBox.Show($"Error archiving previous CBP version (compatibility for a6c): {ex}");
            }
        }

        private void GetLauncherVersion()
        {
            LauncherVersion = "CBP Launcher v" + Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(2);//this is cutting off the first two (rather than last two) numbers
        }

        private async Task GenerateLists()//maybe temporary function to be revised later?
        {
            try
            {
                await GenerateFileListAll();
                await GenerateFileListModded();
                await GenerateFileListOriginal();
                await BackupOriginalFiles();
                //CopyArtFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while generating lists: " + ex);
                Environment.Exit(-1);
            }
        }

        private async Task Delay(int ms)
        {
            Task pause = Task.Delay(ms);
            await pause;
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
            if (_versionStrings.Length != 4)
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
            // afaik it's the same practical effect as reference, but significantly condensed
            if (major != _otherVersion.major || minor != _otherVersion.minor || subMinor != _otherVersion.subMinor || hotfix != _otherVersion.hotfix)//if any part of the versions are different
                return true;
            else
                return false; //detecting if they're different, so false = not different;
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
                                                              , " (PR1)", " (PR2)", " (PR3)", " (PR4)", " (PR5)", " (PR6)", " (PR7)", " (PR8)", " (PR9)" }; // 11-19 respectively
    }
}
