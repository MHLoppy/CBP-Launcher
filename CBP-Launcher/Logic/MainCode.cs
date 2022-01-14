using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using CBPLauncher.Core;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using TgaLib;
using CBPSDK;
using static CBPLauncher.Logic.BasicIO;
using NLog;
using DJ;

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
        private string RoNPathFinal;
        private string RoNPathCheck;
        private string workshopPath;
        private string unloadedModsPath;
        private string RoNDataPath;
        private bool antiSpam = false;
        private List<IPluginCBP> pluginList = null;
        private List<string> pluginsPathList = new List<string>();
        private string pluginTitles = "";

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
        private string folderCBPmodded;
        private string folderCBPoriginal;
        private List<string> CBPFileListAll = new List<string>();//the empty list seems to sometimes have a null error (in app.xaml of all places) in VS2019..... except it doesn't seem to matter at all ? ? ? ? ?
        private List<string> CBPFileListModded = new List<string>();
        private List<string> CBPFileListOriginal = new List<string>();
        private bool updateSetupLater = false;
        private bool prereleaseFilesDetected = false;
        private string primaryDataCBP;
        private string secondaryDataCBP;
        private string primaryNonDataCBP;
        private string secondaryNonDataCBP;
        private string currentPathCBP = "";
        private string currentPathOpt = "";
        private int optCounter = 0;
        private bool abortArchive;
        private string conquestEE;//RoN/conquest
        private string conquestCBP;//NonData/conquest
        private string conquestBackup;//CBP/Original files/conquest
        private string napoleonMap;
        private string worldMap;
        private string napoleonPostTurn;
        private bool pluginFileProblem;

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

        // this can be used to lock out the load/unload buttons when doing I/O (e.g. optional changes)
        // but for now I've decided to just check for it in the I/O function (partly because it's easier to do a routed event than a command for exiting on the button's press)
        /*private bool loadUnloadAllowed = true;
        public bool LoadUnloadAllowed
        {
            get => loadUnloadAllowed;
            set
            {
                loadUnloadAllowed = value;
                OnPropertyChanged();
            }
        }*/

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

        private string logHeaderText = "Simple Log";
        public string LogHeaderText
        {
            get => logHeaderText;
            set
            {
                logHeaderText = value;
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

        private bool optionalMaintainCheckbox = Properties.Settings.Default.OptionalMaintain;
        public bool OptionalMaintainCheckbox
        {
            get => optionalMaintainCheckbox;
            set
            {
                optionalMaintainCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool addIconGameNameCheckbox = Properties.Settings.Default.AddIconGameName;
        public bool AddIconGameNameCheckbox
        {
            get => addIconGameNameCheckbox;
            set
            {
                addIconGameNameCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool useFancyLoggerCheckBox = Properties.Settings.Default.UseFancyLogging;
        public bool UseFancyLoggerCheckBox
        {
            get => useFancyLoggerCheckBox;
            set
            {
                useFancyLoggerCheckBox = value;
                OnPropertyChanged();
            }
        }

        private bool warnCompatibilityCheckbox = Properties.Settings.Default.WarnCompatibility;
        public bool WarnCompatibilityCheckbox
        {
            get => warnCompatibilityCheckbox;
            set
            {
                warnCompatibilityCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool disablePluginLoadingCheckbox = Properties.Settings.Default.DisablePluginLoading;
        public bool DisablePluginLoadingCheckbox
        {
            get => disablePluginLoadingCheckbox;
            set
            {
                disablePluginLoadingCheckbox = value;
                OnPropertyChanged();
            }
        }

        private bool archiveDeleteCheckbox = Properties.Settings.Default.ArchiveDelete;
        public bool ArchiveDeleteCheckbox
        {
            get => archiveDeleteCheckbox;
            set
            {
                archiveDeleteCheckbox = value;
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
                OnPropertyChanged();//this may not be needed here, not sure
            }
        }


        private string optTitle = "Checking title...";
        public string OptTitle
        {
            get => optTitle;
            set
            {
                optTitle = value;
                OnPropertyChanged();
            }
        }

        private string optDescription = "Checking description...";
        public string OptDescription
        {
            get => optDescription;
            set
            {
                optDescription = value;
                OnPropertyChanged();
            }
        }

        private string optCompatibility = "Checking compatibility";
        public string OptCompatibility
        {
            get => optCompatibility;
            set
            {
                optCompatibility = value;
                OnPropertyChanged();
            }
        }

        private ImageSource optPreview;
        public ImageSource OptPreview
        {
            get => optPreview;
            set
            {
                optPreview = value;
                OnPropertyChanged();
            }
        }

        private ImageSource optCurrent;
        public ImageSource OptCurrent
        {
            get => optCurrent;
            set
            {
                optCurrent = value;
                OnPropertyChanged();
            }
        }

        private ImageSource optOriginal;
        public ImageSource OptOriginal
        {
            get => optOriginal;
            set
            {
                optOriginal = value;
                OnPropertyChanged();
            }
        }

        private ImageSource optReplacement;
        public ImageSource OptReplacement
        {
            get => optReplacement;
            set
            {
                optReplacement = value;
                OnPropertyChanged();
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
        public RelayCommand ConfigOptionalCommand { get; set; }
        public RelayCommand OptionalMaintainCommand { get; set; }
        public RelayCommand AddIconGameNameCommand { get; set; }
        public RelayCommand UseFancyLoggingCommand { get; set; }
        public RelayCommand WarnCompatibilityCommand { get; set; }
        public RelayCommand DisablePluginLoadingCommand { get; set; }
        public RelayCommand OverridePathCommand { get; set; }
        public RelayCommand ArchiveDeleteCommand { get; set; }


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


        public RelayCommand OptionalCurrentCommand { get; set; }
        public RelayCommand OptionalDefaultCommand { get; set; }
        public RelayCommand OptionalReplacementCommand { get; set; }


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
                        LaunchStatusText = "Connectivity issue (CBP loaded)";
                        LaunchStatusColor = Brushes.OrangeRed;
                        LaunchButtonText = "Launch game";
                        LaunchEnabled = true;
                        LogoCBP = true;
                        LogoRoNEE = false;
                        break;
                    case LauncherStatus.connectionProblemUnloaded:
                        LaunchStatusText = "Connectivity issue (CBP not loaded)";
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

        /*public static async Task<MainCode> CreateAsync()
        {
            MainCode uwu = new MainCode();
            await uwu.InitializeAsync();
            return uwu;
        }

        public MainCode() { }
        private async Task InitializeAsync()
        {
            //things to do automagically

            if (IsInDesignMode() == false)
            {
                // moved into separate function
                await AutoRunWrapper();
            }
            else
            {
                //designtime baybeeee

                //to stop strange A N G E R Y VS2019 error messages which don't actually matter
                CBPFileListAll.Add("uwu");

                //(turns out that didn't stop the messages  n w n
            }
        }*/

        public MainCode()
        {
            if (IsInDesignMode() == false)
            {
                BigBadWarning();

                ConfigureNLog();
                CBPLogger.GetInstance.Info("Logging has begun.");
                CBPLogger.GetInstance.Info("CBP Launcher " + Assembly.GetExecutingAssembly().GetName().Version.ToString());

                if ((Properties.Settings.Default.FirstTimeRun == true) && (Properties.Settings.Default.JustReset == false))
                    WriteDefaultSettings();

                CheckReinstall();

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

        private void BigBadWarning()
        {
            // big bad error message if you try to run it from the wrong place
            if (Path.GetFileName(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."))) == "workshop")
            {
                /*if (MessageBox.Show("Running CBP Launcher from the Workshop folder is NOT SUPPORTED, and is likely to produce errors. You should be running CBP Setup GUI instead to install CBP Launcher to RoN's location.\n\nDo you want to continue loading CBP Launcher anyway?", "UNSUPPORTED LOCATION", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                    Application.Current.MainWindow.Close();*/

                //HnZ suggests don't even give them the option to say continue anyway. I don't personally agree, but can see where he's coming from
                /*MessageBox.Show("Running CBP Launcher from the Workshop folder is NOT SUPPORTED, and can cause errors.\n\n Run CBP Setup GUI to install CBP Launcher to RoN's location.", "UNSUPPORTED LOCATION", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.MainWindow.Close();*/

                string response = Interaction.InputBox("Running CBP Launcher from the Workshop folder is NOT SUPPORTED, and is likely to produce errors. "
                                                        + "Run CBPSetupGUI.exe instead."
                                                        + "\n\nIf you want to run CBP Launcher from here anyway, type \"I understand\"."
                                                        , "UNSUPPORTED LOCATION");
                if (response.Contains("I understand"))
                {
                    CBPLogger.GetInstance.Warning("User is continuing to run from Workshop location.");
                    MessageBox.Show("I hope you know what you're doing uwu", "Continuing", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                /*else if (response.Contains("reset"))
                {
                    ResetSettings(false);
                    MessageBox.Show("CBP Launcher's settings have been reset and it will now close.");
                    LogManager.Shutdown();
                    Environment.Exit(0);
                }*/
                else
                {
                    LogManager.Shutdown();
                    Environment.Exit(161);
                }
            }
        }

        private void CheckReinstall()
        {
            // unreliably check if RoN was recently installed (the implication being that it may have been recently *re*installed, which we need to know about)
            string crossplay = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrossplayNetLib.dll");
            if ((RecentlyCreated(crossplay, 2) == true) && (Properties.Settings.Default.FirstTimeRun == false))
            {
                CBPLogger.GetInstance.Warning("It looks like RoN may have been recently re-installed.");

                if (MessageBox.Show("If you've recently re-installed Rise of Nations, it's recommended that you reset CBP Launcher's settings to prevent errors. If you did not recently re-install RoN, you can probably say no."
                    + "\n\nReset CBP Launcher's settings?", "Reset settings?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    CBPLogger.GetInstance.Info("User said to reset settings.");

                    ResetSettings(false);
                    Properties.Settings.Default.FuckStopTellingMe = true;
                    SaveSettings();
                    MessageBox.Show("CBP Launcher's settings have been reset and it will now close. You should be able to run everything normally from here.");
                }
                else
                {
                    CBPLogger.GetInstance.Info("User said not to reset settings.");
                }
            }
        }

        private bool RecentlyCreated(string file, int hours)//https://stackoverflow.com/questions/1119158/check-if-file-created-within-last-x-hours
        {
            if (File.Exists(file))
            {
                CBPLogger.GetInstance.Debug("Located " + file);
                DateTime threshold = DateTime.Now.AddHours(-hours);
                return File.GetCreationTime(file) >= threshold;
            }
            else
            {
                CBPLogger.GetInstance.Warning("Unable to locate " + file);
                return false;
            }
        }

        //since sometimes their underlying values are changed without refreshing them :(
        private void RefreshCheckboxValues()
        {
            CBPDefaultCheckbox = Properties.Settings.Default.DefaultCBP;
            UsePrereleaseCheckbox = Properties.Settings.Default.UsePrerelease;
            UseDefaultLauncherCheckbox = Properties.Settings.Default.UseDefaultLauncher;
            UsePrimaryFilesCheckbox = Properties.Settings.Default.UsePrimaryFileList;
            UseSecondaryFilesCheckbox = Properties.Settings.Default.UseSecondaryFileList;
            DetectBullshitCheckbox = Properties.Settings.Default.DetectBullshit;
            OptionalMaintainCheckbox = Properties.Settings.Default.OptionalMaintain;
            AddIconGameNameCheckbox = Properties.Settings.Default.AddIconGameName;
            UseFancyLoggerCheckBox = Properties.Settings.Default.UseFancyLogging;
            WarnCompatibilityCheckbox = Properties.Settings.Default.WarnCompatibility;
            DisablePluginLoadingCheckbox = Properties.Settings.Default.DisablePluginLoading;
            ArchiveDeleteCheckbox = Properties.Settings.Default.ArchiveDelete;

            CBPLogger.GetInstance.Debug("Checkbox values refreshed.");
        }

        private async Task AutoRunWrapper()
        {
            await AutoRun();
            await CreateCommands();

            // don't spend time loading plugins if not being used
            if (Properties.Settings.Default.DisablePluginLoading == false)
            {
                LoadPlugins();
            }
            else
            {
                CBPLogger.GetInstance.Info("Plugin loading is disabled.");
            }
            RefreshCheckboxValues();
        }

        private async Task AutoRun()
        {
            try
            {
                if (Properties.Settings.Default.UpgradeRequired == true)
                {
                    UpgradeSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during initialization: {ex}");
                CBPLogger.GetInstance.Error($"Error during initialization: {ex}");
                LogManager.Shutdown();
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

            try
            {
                ReadRegistry();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading registry: {ex}");
                CBPLogger.GetInstance.Error($"Error reading registry: {ex}");
                LogManager.Shutdown();
                Environment.Exit(0); // for now, if a core part of the program fails then it needs to close to prevent broken but user-accessible functionality
            }

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
                CBPLogger.GetInstance.Error($"Error creating paths (part 1): {ex}");
                LogManager.Shutdown();
                Environment.Exit(0);
            }

            try
            {
                await AssignPaths();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error assigning paths (path 2):" + ex);
                CBPLogger.GetInstance.Error("Error assigning paths (path 2):" + ex);
                LogManager.Shutdown();
                Environment.Exit(0);
            }

            // show detected paths in the UI
            try
            {
                EEPath = RoNPathFinal;
                WorkshopPathDebug = workshopPath;
                WorkshopPathCBPDebug = workshopPathCBP;
                GetLauncherVersion();

                CBPLogger.GetInstance.Info("Current directory: " + rootPath);
                CBPLogger.GetInstance.Info("RoN:EE detected in: " + EEPath);
                CBPLogger.GetInstance.Info("Steam Workshop detected in: " + WorkshopPathDebug);
                CBPLogger.GetInstance.Info("Steam Workshop (CBP) detected in: " + WorkshopPathCBPDebug);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying paths in UI {ex}");
                CBPLogger.GetInstance.Error($"Error displaying paths in UI {ex}");
                LogManager.Shutdown();
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

                Directory.CreateDirectory(Path.Combine(folderCBPoriginal, "conquest"));
                Directory.CreateDirectory(Path.Combine(folderCBPoriginal, "conquest", "Napoleon"));
                conquestCBP = Path.Combine(localPathCBP, "NonData", "conquest");
                conquestEE = Path.Combine(RoNPathFinal, "conquest");
                conquestBackup = Path.Combine(folderCBPoriginal, "conquest");
                napoleonMap = Path.Combine(conquestEE, "CTW_Napoleon_Map_01.xml");
                worldMap = Path.Combine(conquestEE, "CTW_World_Map_01.xml");
                napoleonPostTurn = Path.Combine(conquestEE, "Napoleon", "napoleon_post_turn.bhs");

                //not creating dir, but needs to be after so I'm putting it here
                LogHeaderText = "Detailed logs are saved in the logs folder located in:\n" + folderCBProot;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating directories {ex}");
                CBPLogger.GetInstance.Error($"Error creating directories {ex}");
                LogManager.Shutdown();
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
                CBPLogger.GetInstance.Error($"Error with primary (old content_rendered) step: {ex}");
                LogManager.Shutdown();
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
                MessageBox.Show("Pre-release toggle modified. To use the new files, restart CBP Launcher.");
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
                    await GenerateDynamicHelpText();
                }
            });

            UseSecondaryFilesCommand = new RelayCommand(async o =>
            {
                UseSecondaryFiles_Inversion();
                if (Properties.Settings.Default.CBPLoaded)//otherwise it can't find the files!
                {
                    await GenerateLists();
                    await LoadDirectFiles();
                    await GenerateDynamicHelpText();
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

            OptionalMaintainCommand = new RelayCommand(o =>
            {
                OptionalMaintain_Inversion();
            });

            AddIconGameNameCommand = new RelayCommand(async o =>
            {
                AddIconGameName_Inversion();

                if (Properties.Settings.Default.AddIconGameName)
                    await AddIconGameName();
                else
                    await RemoveIconGameName();
            });

            UseFancyLoggingCommand = new RelayCommand(o =>
            {
                UseFancyLogging_Inversion();

                if (Properties.Settings.Default.UseFancyLogging)
                    MessageBox.Show("Fancy log viewer will be enabled on next startup.");
                else
                    MessageBox.Show("Fancy log viewer will be disabled on next startup.");
            });

            WarnCompatibilityCommand = new RelayCommand(o =>
            {
                WarnCompatibility_Inversion();
            });

            DisablePluginLoadingCommand = new RelayCommand(o =>
            {
                if (Properties.Settings.Default.AnyPluginsLoaded == true)
                {
                    // tell user to unload their plugins
                    MessageBox.Show("Existing plugins must be unloaded before disabling plugin loading.");
                    return;
                }

                DisablePluginLoading_Inversion();

                if (Properties.Settings.Default.DisablePluginLoading)
                    MessageBox.Show("Plugin loading has been disabled.\n\nNOTE: ANY CHANGES PREVIOUSLY MADE BY PLUGINS ARE NOT AUTOMATICALLY UNDONE BY DISABLING PLUGIN LOADING.");
                else
                    MessageBox.Show("Plugin loading has been enabled.\n\nNote that plugins which rely on their own update function will not run this function until the next time CBP Launcher is started.");
            });

            ArchiveDeleteCommand = new RelayCommand(o =>
            {
                ArchiveDelete_Inversion();
            });

            OverridePathCommand = new RelayCommand(o =>
            {
                OverridePathPopup();
            });

            ResetSettingsCommand = new RelayCommand(o =>
            {
                if (MessageBox.Show("Are you sure you want to reset all settings?", "Confirm settings reset", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ResetSettings(true);
                }
                else
                {
                    MessageBox.Show("No action has been taken.");
                }
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
                if (antiSpam == false)
                {
                    antiSpam = true;
                    await UnloadCBP();
                }
                antiSpam = false;
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
                Properties.Settings.Default.MicroSkin = false;
                SaveSettings();
            });

            SkinSpartanV1MiniCommand = new RelayCommand(o =>
            {
                CurrentSkin = SpartanV1Mini;
                Properties.Settings.Default.MicroSkin = true;
                SaveSettings();
            });

            SkinClassicPlusMiniCommand = new RelayCommand(o =>
            {
                CurrentSkin = ClassicPlusMini;
                Properties.Settings.Default.MicroSkin = true;
                SaveSettings();
            });

            SkinClassicPlusCommand = new RelayCommand(o =>
            {
                CurrentSkin = ClassicPlus;
                Properties.Settings.Default.MicroSkin = false;
                SaveSettings();
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

            if ((Properties.Settings.Default.SkinSpV1 == true) && (Properties.Settings.Default.MicroSkin == false))
            {
                CurrentSkin = SpartanV1;
                CurrentTab = SpartanV1PatchNotes;
            }
            else if ((Properties.Settings.Default.SkinSpV1 == true) && (Properties.Settings.Default.MicroSkin == true))
            {
                CurrentSkin = SpartanV1Mini;
                CurrentTab = SpartanV1PatchNotes;
            }
            else if ((Properties.Settings.Default.SkinSpV1 == false) && (Properties.Settings.Default.MicroSkin == true))
            {
                CurrentSkin = ClassicPlusMini;
                CurrentTab = ClassicPlusPatchNotes;
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
                PluginSecurityWarning();
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
                PluginSecurityWarning();
            });

            CPTabOptionsCommand = new RelayCommand(o =>
            {
                CurrentTab = ClassicPlusOptions;
            });

            CPTabLogCommand = new RelayCommand(o =>
            {
                CurrentTab = ClassicPlusLog;
            });

            ConfigOptionalCommand = new RelayCommand(async o =>
            {
                await ConfigureOptionalChanges();
            });

            OptionalCurrentCommand = new RelayCommand(async o =>
            {
                await OptionalChangeUseExisting();
            });

            OptionalDefaultCommand = new RelayCommand(async o =>
            {
                await OptionalChangeUseDefault();
            });

            OptionalReplacementCommand = new RelayCommand(async o =>
            {
                await OptionalChangeUseReplacement();
            });
        }

        private void AssignZipPath()
        {
            // core paths
            rootPath = AppDomain.CurrentDomain.BaseDirectory;
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
                        CBPLogger.GetInstance.Debug("FindPathAuto1 unable to find path, moving to 2.");
                        await FindPathAuto2();
                    }
                }
            }
            else
            {
                RoNPathFinal = Properties.Settings.Default.RoNPathSetting;//moved here so that it works after upgrading settings

                //a7 temp
                RoNDataPath = Path.Combine(RoNPathFinal, "Data");

                helpXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, helpXML));// now also used by dynamic cbp status generation
                interfaceXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, interfaceXML));
                setupwinXMLOrig = Path.GetFullPath(Path.Combine(RoNDataPath, setupwinXML));
                patriotsOrig = Path.GetFullPath(Path.Combine(RoNPathFinal, "patriots.exe"));

                CBPLogger.GetInstance.Debug("Using RoN path saved in settings: " + RoNPathFinal);
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
                CBPLogger.GetInstance.Debug("FindPathAuto2 unable to find path, moving to 3.");
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
                CBPLogger.GetInstance.Debug("FindPathAuto3 unable to find path, moving to manual.");
                await FindPathManual();
            }
        }

        private async Task FindPathManual()
        {
            //people hate gotos (less so in C# but still) but this seems like a very reasonable substitute for a while-not-true loop that I haven't figured out how to implement here
            AskManualPath:

            RoNPathCheck = Interaction.InputBox($"Please provide the file path to the folder where Rise of Nations: Extended Edition is installed."
                                               + "\n\n" + @"e.g. D:\Steamgames\common\Rise of Nations", "Unable to detect RoN install");

            CBPLogger.GetInstance.Debug($"User has supplied {RoNPathCheck} as RoN path.");

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
                    CBPLogger.GetInstance.Warning("RoN path(s) provided by user not valid.");
                    LogManager.Shutdown();
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
                CBPLogger.GetInstance.Debug("Using pre-release of CBP.");
            }
            else
            {
                workshopIDCBP = "2287791153"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)
                CBPLogger.GetInstance.Debug("Using non-PR of CBP.");
            }
            //workshopIDCBP = "2287791153"; // by separating the mod ID, more mods can be supported in the future and it can become a local/direct mods mod manager (direct needs more work still though)

            workshopPathCBP = Path.Combine(Path.GetFullPath(workshopPath), workshopIDCBP); /// getfullpath ensures the slash is included between the two
            localPathCBP = Path.Combine(Path.GetFullPath(localMods), modnameCBP);          /// I tried @"\" and "\\" and both made the first part (localMods) get ignored in the combined path
            versionFileCBPLocal = Path.Combine(localPathCBP, "version.txt"); // moved here in order to move with the data files (useful), and better structure to support other mods in future
            versionFileCBPWorkshop = Path.Combine(workshopPathCBP, "Community Balance Patch", "version.txt");

            primaryDataCBP = Path.Combine(localPathCBP, "PrimaryData");
            secondaryDataCBP = Path.Combine(localPathCBP, "SecondaryData");
            primaryNonDataCBP = Path.Combine(localPathCBP, "PrimaryNonData");
            secondaryNonDataCBP = Path.Combine(localPathCBP, "SecondaryNonData");
        }

        private async Task CheckForUpdates()
        {
            try // without the try you can accidentally create online-only DRM whoops
            {
                // if CBP is unloaded, load the folder (but not the data files and extra stuff outside the folder yet) so that we can archive those files
                // we also don't juse rely on the setting so that settings reset is better supported
                // (although to be honest, that could probably be made a pre-check where it checks for files then assigns settings based on that)
                if (File.Exists(Path.Combine(unloadedModsPath, "Community Balance Patch", "version.txt")))
                {
                    CBPLogger.GetInstance.Debug("Detected unloaded CBP folder when checking for updates...");
                    Directory.Move(Path.Combine(unloadedModsPath, "Community Balance Patch"), Path.Combine(localPathCBP));
                    Properties.Settings.Default.CBPLoaded = true;
                    Properties.Settings.Default.CBPUnloaded = false;
                    SaveSettings();
                }

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
                CBPLogger.GetInstance.Debug(VersionTextLatest);

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
                            CBPLogger.GetInstance.Debug("Online and local versions are different...");
                            await OldInstallGameFiles(true, onlineVersion);
                            //GenerateLists();
                            //LoadDirectFiles();
                            //oldinstallgamefiles already includes both of those if the bool is true, so no need to repeat it
                        }
                        else
                        {
                            CBPLogger.GetInstance.Debug("Online and local versions are the same...");
                            await GenerateLists();
                            await LoadDirectFiles();
                            if (Properties.Settings.Default.AddIconGameName)
                            {
                                await AddIconGameName();
                            }
                            await GenerateDynamicHelpText();

                            Status = LauncherStatus.readyCBPEnabled; //if the local version.txt matches the version found in the online file, then no patch required
                            Properties.Settings.Default.CBPLoaded = true;
                            Properties.Settings.Default.CBPUnloaded = false;
                            SaveSettings();
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        CBPLogger.GetInstance.Error($"Error installing patch files: {ex}");
                        MessageBox.Show($"Error installing patch files: {ex}");
                    }
                }

                // compatibility with a6c (maybe making it compatible was a mistake)
                else if (Directory.Exists(Path.Combine(localMods, "Community Balance Patch (Alpha 6c)")))
                {
                    await OldInstallGameFiles(true, Version.zero);
                }

                //this will only run if the local version file (if it exists) is not different to the online one
                //update: no longer needed, because now we always start off by loading from unloaded if it exists (and this whole function is only called when the intended result is to load CBP)
                /*else if (File.Exists(Path.Combine(unloadedModsPath, "Community Balance Patch", "version.txt")))
                {
                    //to simplify archiving etc, just load the mod first (but not the direct files, to save time)
                    Directory.Move(Path.Combine(unloadedModsPath, "Community Balance Patch"), Path.Combine(localPathCBP));
                    Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal));

                    //Version localVersion = new Version(File.ReadAllText(Path.Combine(unloadedModsPath, "Community Balance Patch", "version.txt")));
                    await OldInstallGameFiles(true, Version.zero);
                    await GenerateLists();
                    await LoadDirectFiles();
                    await GenerateDynamicHelpText();
                    if (Properties.Settings.Default.AddIconGameName)
                        await AddIconGameName();
                }*/

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
                CBPLogger.GetInstance.Error($"Error checking for updates. Maybe no connection could be established? {ex}");
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
            try
            {
                // check each file in the modded list and make sure it's an up-to-date CBP file
                Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal));//at some point this should definitely be spun out into a less-localised variable so that it can be used in multiple places

                foreach (string filename in CBPFileListModded)
                {
                    if (CheckIfCBPFile(filename) == true)
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is a CBP file...");

                        //it's a CBP file (but not necessarily the right version, so deal with that too)
                        string text = File.ReadLines(Path.Combine(RoNDataPath, filename)).Skip(2).Take(1).First();
                        Version fileVersion = new Version(text.Substring(9, 11));

                        if (fileVersion.IsDifferentThan(localVersion))// I assume it's faster to check this than straight up always-write files
                        {
                            CBPLogger.GetInstance.Debug("..but isn't up to date - updating...");
                            await ActuallyLoadFiles(filename);
                        }
                        //else no action required
                    }
                    else
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is not a CBP file...");
                        await ActuallyLoadFiles(filename);
                    }
                }

                // check each file in the original list and make sure it's **NOT** a CBP file (we don't care if it's user-modded, since they did that themselves)
                foreach (string filename in CBPFileListOriginal)
                {
                    if (CheckIfCBPFile(filename) == true)
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is a CBP file - swapping to original...");

                        //replace it from the backup that was previously copied (not necessarily copied in this session)
                        File.Copy(Path.Combine(folderCBPoriginal, filename), Path.Combine(RoNDataPath, filename), true);
                    }
                    else
                    {
                        //*chef's kiss* no action required
                        CBPLogger.GetInstance.Debug($"{filename} is not a CBP file - no action taken.");
                    }
                }

                //these use separate logic
                await LoadConquestFiles();
                await LoadArtFiles();
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error($"Error changing data files: {ex}");
                MessageBox.Show("Error changing data files: " + ex);
                LogManager.Shutdown();
                Environment.Exit(-1);
            }
        }

        private async Task ActuallyLoadFiles(string filename)
        {
            try
            {
                if (File.Exists(Path.Combine(primaryDataCBP, filename)))
                    File.Copy(Path.Combine(primaryDataCBP, filename), Path.Combine(RoNDataPath, filename), true);

                else if (File.Exists(Path.Combine(secondaryDataCBP, filename)))
                    File.Copy(Path.Combine(secondaryDataCBP, filename), Path.Combine(RoNDataPath, filename), true);
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error($"Error actually loading files: {ex}");
                MessageBox.Show("Error actually loading files: " + ex);
                LogManager.Shutdown();
                Environment.Exit(-1);
            }
            // in theory (shouldn't *actually* happen but ya know) you could have scuffed files in neither list, but for now it's not handled
        }

        //semi-forced into doing shitty hardcoded lists
        private async Task LoadConquestFiles()
        {
            try
            {
                // check if file in RoN/conquest is CBP and up to date, if not replace it with latest CBP file
                if (File.Exists(napoleonMap))
                {
                    if ((CheckConquestFileCBP(napoleonMap, false) == false) || (CheckConquestCBPNeedsUpdating(napoleonMap, false) == true))
                    {
                        await CopyNapMap();
                    }
                }
                else
                {
                    await CopyNapMap();
                }

                if (File.Exists(worldMap))
                {
                    if ((CheckConquestFileCBP(worldMap, false) == false) || (CheckConquestCBPNeedsUpdating(worldMap, false) == true))
                    {
                        await CopyWorldMap();
                    }
                }
                else
                {
                    await CopyWorldMap();
                }

                //bhs file, different syntax etc from xml so we look at a different line (and on a different point on the line) for the CBP comment
                if (File.Exists(napoleonPostTurn))
                {
                    if ((CheckConquestFileCBP(napoleonPostTurn, true) == false) || (CheckConquestCBPNeedsUpdating(napoleonPostTurn, true) == true))
                    {
                        await CopyNapPost();
                    }
                }
                else
                {
                    await CopyNapPost();
                }

                CBPLogger.GetInstance.Debug("Finished loading of conquest files.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading conquest files: " + ex);
                CBPLogger.GetInstance.Error("Error loading conquest files: " + ex);
            }
        }

        private async Task CopyNapMap()
        {
            File.Copy(Path.Combine(conquestCBP, "CTW_Napoleon_Map_01.xml"), napoleonMap, true);
            CBPLogger.GetInstance.Debug("CBP version of Napoleon Map installed/updated.");
        }

        private async Task CopyWorldMap()
        {
            File.Copy(Path.Combine(conquestCBP, "CTW_World_Map_01.xml"), worldMap, true);
            CBPLogger.GetInstance.Debug("CBP version of World Map installed/updated.");
        }

        private async Task CopyNapPost()
        {
            File.Copy(Path.Combine(conquestCBP, "Napoleon", "napoleon_post_turn.bhs"), napoleonPostTurn, true);
            CBPLogger.GetInstance.Debug("CBP version of Napoleon post-turn installed/updated.");
        }

        private async Task UnloadConquestFiles()
        {
            // these might make more sense as global variables but this is tolerable for now
            string conquestOrig = Path.Combine(folderCBPoriginal, "conquest");
            string conquestEE = Path.Combine(RoNPathFinal, "conquest");

            try
            {
                // check if file in RoN/conquest is CBP, if not replace it with original, non-CBP file from backup
                string napoleonMap = File.ReadLines(Path.Combine(conquestEE, "CTW_Napoleon_Map_01.xml")).Skip(2).Take(1).First();
                if (napoleonMap.Substring(5).StartsWith("CBP") == true)
                {
                    File.Copy(Path.Combine(conquestOrig, "CTW_Napoleon_Map_01.xml"), Path.Combine(conquestEE, "CTW_Napoleon_Map_01.xml"), true);
                    CBPLogger.GetInstance.Debug("Backed up version of Napoleon Map restored.");
                }

                string worldMap = File.ReadLines(Path.Combine(conquestEE, "CTW_World_Map_01.xml")).Skip(2).Take(1).First();
                if (worldMap.Substring(5).StartsWith("CBP") == true)
                {
                    File.Copy(Path.Combine(conquestOrig, "CTW_World_Map_01.xml"), Path.Combine(conquestEE, "CTW_World_Map_01.xml"), true);
                    CBPLogger.GetInstance.Debug("Backed up version of World Map restored.");
                }

                //bhs file, different syntax etc from xml so we look at a different line (and on a different point on the line) for the CBP comment
                string napoleonPostTurn = File.ReadLines(Path.Combine(conquestEE, "Napoleon", "napoleon_post_turn.bhs")).Skip(1).Take(1).First();
                if (napoleonPostTurn.Substring(3).StartsWith("CBP") == true)
                {
                    File.Copy(Path.Combine(conquestOrig, "Napoleon", "napoleon_post_turn.bhs"), Path.Combine(conquestEE, "Napoleon", "napoleon_post_turn.bhs"), true);
                    CBPLogger.GetInstance.Debug("Backed up version of Napoleon post-turn restored.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error unloading conquest files (this is not a critical error) " + ex);
                CBPLogger.GetInstance.Error("Error unloading conquest files " + ex);
            }
        }

        private bool CheckConquestFileCBP(string file, bool isBHS)
        {
            try
            {
                //int skipLines = 0;
                int skipChars = 0;
                
                if (isBHS == false)
                {
                    //skipLines = 2;
                    skipChars = 5;
                }
                else if (isBHS == true)
                {
                    //skipLines = 1;
                    skipChars = 3;
                }

                if (file.Substring(skipChars).StartsWith("CBP") == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //not a critical error - bad conquest files will still allow multiplayer
                MessageBox.Show("Error checking if conquest files are patched (this is not a critical error): " + ex);
                CBPLogger.GetInstance.Error("Error checking if conquest files are patched: " + ex);
                return false;
            }
        }

        //check if cbp conquest file is up to date
        private bool CheckConquestCBPNeedsUpdating(string file, bool isBHS)
        {
            try
            {
                Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal));
                int skipLines = 0;
                int skipChars = 0;

                // bhs file needs to check a different place since the commenting is a different from xml files
                if (isBHS == false)
                {
                    skipLines = 2;
                    skipChars = 9;
                }
                else if (isBHS == true)
                {
                    skipLines = 1;
                    skipChars = 7;
                }

                string text = File.ReadLines(file).Skip(skipLines).Take(1).First();
                Version fileVersion = new Version(text.Substring(skipChars, 11));

                if (fileVersion.IsDifferentThan(localVersion))
                    return false;
                else
                    return true;
            }
            catch (Exception ex)
            {
                //not a critical error - bad conquest files will still allow multiplayer
                MessageBox.Show("Error checking version of patched conquest files (this is not a critical error): " + ex);
                CBPLogger.GetInstance.Error("Error checking version of patched conquest files: " + ex);
                return false;
            }
        }

        private async Task LoadArtFiles()//no unload because separate-from-normal-RoN files with no version tracking (since they're TGAs)
        {
            try
            {
                //copy contents of /mods/NonData/art (including subdirectories) to /CBP/art, overwriting existing files if they exist
                DirectoryCopy(Path.Combine(localPathCBP, "NonData", "art"), Path.Combine(folderCBPmodded, "art"), true, true);
                CBPLogger.GetInstance.Debug("Art files copied.");

                await OptionalMaintainSelection();
                CBPLogger.GetInstance.Debug("Optional changes re-applied.");
            }
            catch (Exception ex)
            {
                //some of the art files are actually essential - should not allow user to continue if they error here
                MessageBox.Show("Error loading art files " + ex);
                CBPLogger.GetInstance.Error("Error loading art files " + ex);
                LogManager.Shutdown();
                Environment.Exit(-1);
            }
        }

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
            try
            {
                foreach (string filename in CBPFileListAll)
                {
                    if (CheckIfCBPFile(filename) == true)
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is a CBP file - unloading for original...");
                        File.Copy(Path.Combine(folderCBPoriginal, filename), Path.Combine(RoNDataPath, filename), true);
                    }
                    //else no action required
                    CBPLogger.GetInstance.Debug($"{filename} is not a CBP file - no action taken.");
                }

                //separate logic
                await UnloadConquestFiles();
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error($"Error unloading files: {ex}");
                MessageBox.Show($"Error unloading files: {ex}");
                LogManager.Shutdown();
                Environment.Exit(-1);
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

                CBPLogger.GetInstance.Debug("Added from primary list to full list: " + filename);
            }

            //secondary files
            string[] secondaryFiles = Directory.GetFiles(secondaryDataCBP);
            foreach (string filename in secondaryFiles)
            {
                CBPFileListAll.Add(
                    //"Secondary file: " + 
                    Path.GetFileName(filename));//don't actually put the secondary file prefix in the list!

                CBPLogger.GetInstance.Debug("Added from secondary list to full list: " + filename);
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
                CBPLogger.GetInstance.Info("Seems like there are old files from an early Alpha 7 pre-release...");
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
                    CBPLogger.GetInstance.Error("Could not unload old CBP files automatically. Unable to continue. " + ex);
                    LogManager.Shutdown();
                    Environment.Exit(-1);
                }
                await CheckForUpdates();
            }

            if (Properties.Settings.Default.FilesBackedUp == false)
            {
                //copy files from e.g. /data/ to /CBP/Original Files
                CBPLogger.GetInstance.Debug("FilesBackedUp is false.");
                try
                {
                    //data files (primary and secondary)
                    foreach (string filename in CBPFileListAll)
                    {
                        CBPLogger.GetInstance.Info("File list (all): " + filename);

                        if (!File.Exists(Path.Combine(folderCBPoriginal, filename)))
                            File.Copy(Path.Combine(RoNDataPath, filename), Path.Combine(folderCBPoriginal, filename));//if this fails partway then maybe need a way to overwrite (or at least delete) what's there
                        else CBPLogger.GetInstance.Info(filename + " already has a backup file so has been skipped.");
                    }

                    Properties.Settings.Default.FilesBackedUp = true;
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem backing up data files. " + ex);
                    CBPLogger.GetInstance.Error("Problem backing up data files. " + ex);
                    LogManager.Shutdown();
                    Environment.Exit(-1);
                }
            }

            else if (Properties.Settings.Default.FilesBackedUp == true)
            {
                CBPLogger.GetInstance.Debug("Settings say that data files are already backed up.");
            }

            //I hate this but for 3 files I can live with it
            if (Properties.Settings.Default.NonDataFilesBackedUp == false)
            {
                try
                {
                    /*
                    // conquest/CTW_Napoleon_Map_01.xml
                    string nMap = "CTW_Napoleon_Map_01.xml";
                    if (!File.Exists(Path.Combine(folderCBPoriginal, "conquest", nMap)))
                        File.Copy(Path.Combine(RoNPathFinal, "conquest", nMap), Path.Combine(folderCBPoriginal, "conquest", nMap));
                    else CBPLogger.GetInstance.Debug("Backup of Napoleon Map file already exists - no action taken.");

                    // conquest/CTW_World_Map_01.xml
                    string wMap = "CTW_World_Map_01.xml";
                    if (!File.Exists(Path.Combine(folderCBPoriginal, "conquest", wMap)))
                        File.Copy(Path.Combine(RoNPathFinal, "conquest", wMap), Path.Combine(folderCBPoriginal, "conquest", wMap));
                    else CBPLogger.GetInstance.Debug("Backup of World Map file already exists - no action taken.");

                    // conquest/Napoleon/napoleon_post_turn.bhs
                    string nPost = "napoleon_post_turn.bhs";
                    if (!File.Exists(Path.Combine(folderCBPoriginal, "conquest", "Napoleon", nPost)))
                        File.Copy(Path.Combine(RoNPathFinal, "conquest", "Napoleon", nPost), Path.Combine(folderCBPoriginal, "conquest", "Napoleon", nPost));
                    else CBPLogger.GetInstance.Debug("Backup of Napoleon post-turn file already exists - no action taken.");*/

                    await BackupConquestFiles();

                    Properties.Settings.Default.NonDataFilesBackedUp = true;
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Problem backing up non-data files: " + ex);
                    Environment.Exit(-1);
                }
            }
            else if (Properties.Settings.Default.NonDataFilesBackedUp == true)
            {
                CBPLogger.GetInstance.Debug("Non-data files already backed up - no action taken.");
            }
        }

        private async Task BackupConquestFiles()
        {
            string nMap = "CTW_Napoleon_Map_01.xml";
            string wMap = "CTW_World_Map_01.xml";
            string nPost = Path.Combine("Napoleon", "napoleon_post_turn.bhs");

            string nMapEE = Path.Combine(conquestEE, nMap);
            string nMapBackup = Path.Combine(conquestBackup, nMap);
            string wMapEE = Path.Combine(conquestEE, wMap);
            string wMapBackup = Path.Combine(conquestBackup, wMap);
            string nPostEE = Path.Combine(conquestEE, nPost);
            string nPostBackup = Path.Combine(conquestBackup, nPost);

            //check for existing file, copy if not there
            if (!File.Exists(nMapBackup))
            {
                File.Copy(nMapEE, nMapBackup);
                CBPLogger.GetInstance.Debug("Napoleon Map backup created.");
            }
            else
            {
                CBPLogger.GetInstance.Debug("Napoleon Map backup already exists - no action taken.");
            }

            if (!File.Exists(wMapBackup))
            {
                File.Copy(wMapEE, wMapBackup);
                CBPLogger.GetInstance.Debug("World Map backup created.");
            }
            else
            {
                CBPLogger.GetInstance.Debug("World Map backup already exists - no action taken.");
            }

            if (!File.Exists(nPostBackup))
            {
                File.Copy(nPostEE, nPostBackup);
                CBPLogger.GetInstance.Debug("Napoleon post-turn backup created.");
            }
            else
            {
                CBPLogger.GetInstance.Debug("Napoleon post-turn backup already exists - no action taken.");
            }
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

            CBPLogger.GetInstance.Debug("Modded file list generated.");
        }

        private async Task GenerateFileListOriginal()
        {
            IEnumerable<string> differencequery = CBPFileListAll.Except(CBPFileListModded);
            foreach (string name in differencequery)
            {
                CBPFileListOriginal.Add(name);
                //MessageBox.Show("Added " + name + "to CBPFileListOriginal"); //change to log event later
            }

            CBPLogger.GetInstance.Debug("Original file list generated.");
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

                                abortArchive = true;
                                CBPLogger.GetInstance.Warning("Newer CBP released, but files not downloaded.");
                                MessageBox.Show(newestVersion + " has been published on Steam Workshop, but Steam hasn't downloaded the new files yet so CBP Launcher is unable to install them.");
                            }
                        }

                        // extra steps depending on whether this an update to existing install or first time install
                        if (_isUpdate)
                        {
                            Status = LauncherStatus.installingUpdateLocal;

                            // if archive setting is enabled, archive the old version; it looks for an unversioned CBP folder and has a separate check for a6c specifically
                            if ((Properties.Settings.Default.CBPArchive == true) && (Properties.Settings.Default.ArchiveDelete == false))
                            {
                                CBPLogger.GetInstance.Info("Archiving old CBP version...");
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
                            else if (Properties.Settings.Default.ArchiveDelete == true)
                            {
                                CBPLogger.GetInstance.Info("Deleting old CBP version...");
                                await DeleteOldVersion();
                            }
                            else
                            {
                                //not sure how you get here because the old (and currently unused) CBPArchive setting is not exposed to the user
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
                            CBPLogger.GetInstance.Debug("did not copy workshop CBP to local CBP because of archive abort flag");
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

                        await ReplaceRestoreDefaultLauncher();//this seems super clunky

                        try
                        {
                            await GenerateLists();
                            await LoadDirectFiles();

                            if (Properties.Settings.Default.AddIconGameName)
                            {
                                await AddIconGameName();
                            }
                            await GenerateDynamicHelpText();
                        }
                        catch (Exception ex)
                        {
                            CBPLogger.GetInstance.Error($"Error generating lists / directly loading files: {ex}");
                            MessageBox.Show($"Error generating lists / directly loading files: {ex}");
                        }

                        if (Properties.Settings.Default.OptionalMaintain)
                        {
                            await OptionalMaintainSelection();
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
                            CBPLogger.GetInstance.Error($"For some ungodly reason this part failed: {ex}");
                            MessageBox.Show($"Error loading CBP: {ex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = LauncherStatus.installFailed;
                        CBPLogger.GetInstance.Error($"For some ungodly reason this part failed: {ex}");
                        MessageBox.Show($"Error installing CBP from Workshop files: {ex}");
                    }
                }

                // not currently used because the setting cannot be set by the user (unless they like, edit tje settings file manually lol)
                else if (Properties.Settings.Default.NoWorkshopFiles == true) // as of v0.3 release this option isn't even exposed to the user yet, but it'll be useful later
                {
                    CBPLogger.GetInstance.Warning("For some reason NoWorkshopFiles setting is true!?!?");

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
                    CBPLogger.GetInstance.Error($"Error loading CBP: {ex}");
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
                    CBPLogger.GetInstance.Warning($"Entering fancy catch: {ex}");

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
                        LogManager.Shutdown();
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
                CBPLogger.GetInstance.Error($"Error installing new patch files: {ex}");
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

                        if (Directory.Exists(Path.Combine(unloadedModsPath, "Community Balance Patch")))
                        {
                            if (MessageBox.Show("It looks like there's already a copy of CBP in the Unloaded Mods folder:\n"
                                                + Path.Combine(unloadedModsPath, "Community Balance Patch")
                                                + "\n\nDelete it and continue?", "CBP Folder Detected", MessageBoxButton.YesNo) == MessageBoxResult.No)
                            {
                                return;
                            }
                            else
                            {
                                Directory.Delete(Path.Combine(unloadedModsPath, "Community Balance Patch"), true);
                            }
                        }

                        Directory.Move(localPathCBP, Path.Combine(unloadedModsPath, "Community Balance Patch"));

                        try
                        {
                            await UnloadDirectFiles();
                            await RemoveIconGameName();
                        }
                        catch (Exception ex)
                        {
                            CBPLogger.GetInstance.Error($"Error directly unloading files: {ex}");
                            MessageBox.Show($"Error directly unloading files: {ex}");
                        }

                        Properties.Settings.Default.CBPUnloaded = true;
                        Properties.Settings.Default.CBPLoaded = false;
                        SaveSettings();

                        VersionTextInstalled = "CBP not loaded";
                        Status = LauncherStatus.readyCBPDisabled;
                    }
                    else
                    {
                        MessageBox.Show($"CBP Setup GUI (patriots.exe) doesn't seem to be closing, so CBP Launcher will be closed.");
                        Status = LauncherStatus.unloadFailed;
                        CBPLogger.GetInstance.Error($"CBP Setup GUI (patriots.exe) wasn't closing >:(");
                        LogManager.Shutdown();
                        Environment.Exit(-1);
                    }
                }
                catch (Exception ex)
                {
                    CBPLogger.GetInstance.Error($"Error unloading mod: {ex}");
                    MessageBox.Show($"Error unloading mod: {ex}");
                    Status = LauncherStatus.unloadFailed;
                }

                //not yet implemented
                if (Properties.Settings.Default.UnloadWorkshopToo == true)
                {
                    try
                    {
                        //unload workshop
                    }
                    catch (Exception ex)
                    {
                        CBPLogger.GetInstance.Error($"Error unloading Workshop mod: {ex}");
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
                while (i < 10)//if we're waiting more than 7.2 seconds, something is wrong because it should close in ~5 seconds max
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
                CBPLogger.GetInstance.Debug("Registry read as " + RegPath);
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error("Error with ReadRegistry:" + ex);
            }
        }

        private async Task PlayButton_Click()
        {
            //legacy: from when Bark/Trireme OoS detection was needed
            //the setting that triggers this is toggled off after the first time the launcher is run
            //WarnLocalModDataFiles();
            //Properties.Settings.Default.FirstTimeRun = false;
            //SaveSettings();

            // only warn if user has not disabled this setting
            if (Properties.Settings.Default.WarnCompatibility == true)
            {
                // check compatibility again, otherwise can false-positive on a plugin that's actually loaded
                CheckPluginCompatibility();

                // to prevent plugins from potentially loading old files (e.g. rules.xml) that are outdated with new CBP updates, check the CBP version in file headers
                if (pluginFileProblem && (Properties.Settings.Default.CBPLoaded == true))
                {
                    if (MessageBox.Show("One or more plugins has created an unsupported conflict with CBP files. To resolve this:"
                        + "\n1) Unload any plugins that are marked as NOT CBP compatible AND/OR not multiplayer compatible by default (see each plugin's info)."
                        + "\n2) Unload and then reload CBP."
                        + "\n3) Reload/reconfigure any affected plugins."
                        + "\n\n Ignore conflict and continue? (you probably can't play multiplayer without fixing this)"
                        , "File incompatibility detected", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;//if user says *no*, abort game launch sequence - otherwise can do nothing and continue
                    }
                }
                //different message depending on whether CBP is loaded or not
                else if (pluginFileProblem && (Properties.Settings.Default.CBPLoaded == false))
                {
                    if (MessageBox.Show("One or more plugins has created an unsupported conflict with game files. To resolve this:"
                        + "\n1) Unload any plugins that are marked as NOT CBP compatible AND/OR not multiplayer compatible by default (see each plugin's info)."
                        + "\n2) Load and then unload CBP."
                        + "\n3) Reload/reconfigure any affected plugins."
                        + "\n\n Ignore error and continue? (you probably can't play multiplayer without fixing this)"
                        , "File incompatibility detected", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;//if user says *no*, abort game launch sequence - otherwise can do nothing and continue
                    }
                }

                // thought of a more elegant way to implement this below!
                /*if (CheckPluginCompatbilityIssue() && CheckMultiplayerIssue())
                {
                    CBPLogger.GetInstance.Warning("One or more loaded plugins not compatible with CBP and are also not default-multiplayer compatible...");

                    if (MessageBox.Show("One or more of the plugins currently loaded is not compatible with CBP,"
                        + " and is only multiplayer compatible if loaded and configured the same for ALL players in your game lobby. Continue anyway?", "Plugin warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                        CBPLogger.GetInstance.Warning("..but continuing anyway.");
                }

                else if (CheckPluginCompatbilityIssue())
                {
                    CBPLogger.GetInstance.Warning("One or more loaded plugins not compatible with CBP...");

                    if (MessageBox.Show("One or more of the plugins currently loaded is not compatible with CBP. Continue anyway?", "Plugin warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                        CBPLogger.GetInstance.Warning("..but continuing anyway.");
                }

                else if (CheckMultiplayerIssue())
                {
                    CBPLogger.GetInstance.Warning("One or more loaded plugins are not default-multiplayer compatible...");

                    if (MessageBox.Show("One or more of the plugins currently loaded is only multiplayer-compatible if loaded and configured the same for ALL players in your game lobby."
                        + " Continue anyway?", "Plugin warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                        CBPLogger.GetInstance.Warning("..but continuing anyway.");
                }*/

                //this sequence is probably faster because it only runs each of the two checks a single time each!
                int guraCuteShark = 0;//https://twitter.com/FluffyBlanket_/status/1417443845344616450

                if (CheckPluginCompatbilityIssue())
                    guraCuteShark += 1;
                if (CheckMultiplayerIssue())
                    guraCuteShark += 2;

                //only plugin compatibility issue
                if (guraCuteShark == 1)
                {
                    CBPLogger.GetInstance.Warning("One or more loaded plugins not compatible with CBP...");

                    if (MessageBox.Show("One or more of the plugins currently loaded is not compatible with CBP. Continue anyway?", "Plugin warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                        CBPLogger.GetInstance.Warning("..but continuing anyway.");
                }

                //only multiplayer compatibility issue
                else if (guraCuteShark == 2)
                {
                    CBPLogger.GetInstance.Warning("One or more loaded plugins are not default-multiplayer compatible...");

                    if (MessageBox.Show("One or more of the plugins currently loaded is only multiplayer-compatible if loaded and configured the same for ALL players in your game lobby."
                        + " Continue anyway?", "Plugin warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                        CBPLogger.GetInstance.Warning("..but continuing anyway.");
                }

                //both plugin compatibiliy and multiplay compatibility issues
                else if (guraCuteShark == 3)
                {
                    CBPLogger.GetInstance.Warning("One or more loaded plugins not compatible with CBP and are also not default-multiplayer compatible...");

                    if (MessageBox.Show("One or more of the plugins currently loaded is not compatible with CBP,"
                        + " and is only multiplayer compatible if loaded and configured the same for ALL players in your game lobby. Continue anyway?", "Plugin warning", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                        CBPLogger.GetInstance.Warning("..but continuing anyway.");
                }
                // if 0, there was no issue
            }

            if (File.Exists(gameExe) && (Status == LauncherStatus.readyCBPEnabled || Status == LauncherStatus.readyCBPDisabled)) // make sure all "launch" button options are included here
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

                CBPLogger.GetInstance.Info($"RoN launched. CBP Launcher closing...");
                LogManager.Shutdown();
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

        // section for the #ICON169 / #ICON170 (CBP icon) XML editing
        private string appDataRoN;
        private string playerProfileFolder;
        private string currentUserXml;
        private string playerProfile;
        private string gameName;

        // add the icon to game names (function is called when CBP is loaded)
        private async Task AddIconGameName()
        {
            try
            {
                if (CheckForFile())
                {
                    FindProfile();
                    ReadGameName();
                    await AddCBPXml();
                }
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error("Error adding CBP icon to game name" + ex);
                MessageBox.Show("Error adding CBP icon to game name" + ex);
            }
        }

        // remove the icon from game names (function is called when CBP is unloaded)
        private async Task RemoveIconGameName()
        {
            try
            {
                if (CheckForFile())
                {
                    FindProfile();
                    ReadGameName();
                    await RemoveCBPXml();
                }
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error("Error removing CBP icon from game name" + ex);
                MessageBox.Show("Error removing CBP icon from game name" + ex);
            }
        }

        private bool CheckForFile()
        {
            appDataRoN = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft Games\Rise of Nations");
            playerProfileFolder = Path.Combine(appDataRoN, "PlayerProfile");
            currentUserXml = Path.Combine(playerProfileFolder, "current_user.xml");

            if (File.Exists(currentUserXml))
            {
                CBPLogger.GetInstance.Info("Found file: " + currentUserXml);
                return true;
            }
            else
            {
                CBPLogger.GetInstance.Info("Unable to find current user xml file");
                return false;
            }
        }

        private void FindProfile() // logic to find current user + their .dat file
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(currentUserXml);
            string ronName = doc.SelectSingleNode("ROOT/CURRENT_USER/@name").Value;

            playerProfile = Path.Combine(playerProfileFolder, (ronName + ".dat"));

            CBPLogger.GetInstance.Info("RoN username: " + ronName);
        }

        private void ReadGameName() // reads the last game name (mostly as a building block for later functions + troubleshooting rather than to use itself)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(playerProfile);
            XmlNode xmlNode = doc.SelectSingleNode("ROOT/GAMESPY/LAST_GAME_NAME");
            gameName = xmlNode.InnerText;

            CBPLogger.GetInstance.Info("Last game name: " + gameName);
        }

        private bool CheckCBPXml() // checks if #ICON169 is already present in last game name
        {
            if (Properties.Settings.Default.UsePrerelease)
            {
                if (gameName.Contains("#ICON170") == true)
                {
                    CBPLogger.GetInstance.Info("Last game name already contains CBP-PR icon.");
                    return true;
                }
                else
                {
                    CBPLogger.GetInstance.Info("CBP-PR icon not found in last game name.");
                    return false;
                }
            }
            else
            {
                if (gameName.Contains("#ICON169") == true)
                {
                    CBPLogger.GetInstance.Info("Last game name already contains CBP (non-PR) icon.");
                    return true;
                }
                else
                {
                    CBPLogger.GetInstance.Info("CBP (non-PR) icon not found in last game name.");
                    return false;
                }
            }
        }

        private async Task AddCBPXml() // updates last game name to prefix with #ICON169
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(playerProfile);
            XmlNode xmlNode = doc.SelectSingleNode("ROOT/GAMESPY/LAST_GAME_NAME");

            if ((CheckCBPXml() == false) && (Properties.Settings.Default.UsePrerelease == false))
            {
                //remove PR icon
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON170 ", "");
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON170", "");

                //add non-PR icon
                xmlNode.InnerText = "#ICON169" + xmlNode.InnerText;
                doc.Save(playerProfile);

                CBPLogger.GetInstance.Info("#ICON169 added. Game name: " + xmlNode.InnerText);
            }
            else if ((CheckCBPXml() == false) && (Properties.Settings.Default.UsePrerelease == true))
            {
                //remove non-PR icon
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON169 ", "");
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON169", "");

                //add PR icon
                xmlNode.InnerText = "#ICON170" + xmlNode.InnerText;
                doc.Save(playerProfile);

                CBPLogger.GetInstance.Info("#ICON170 added. Game name: " + xmlNode.InnerText);
            }
        }

        private async Task RemoveCBPXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(playerProfile);
            XmlNode xmlNode = doc.SelectSingleNode("ROOT/GAMESPY/LAST_GAME_NAME");

            if (CheckCBPXml() == true)
            {
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON169 ", "");
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON169", "");//(slightly reduced chance of affecting user's spacing by doing it this way?)
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON170 ", "");
                xmlNode.InnerText = xmlNode.InnerText.Replace("#ICON170", "");
                doc.Save(playerProfile);
            }

            CBPLogger.GetInstance.Info("CBP icons removed from " + playerProfile);
        }

        // section for the dynamic help.xml text
        private async Task GenerateDynamicHelpText()
        {
            if (Properties.Settings.Default.UseSecondaryFileList && CheckIfCBPFile(helpXMLOrig))//helpXMLOrig is just /Rise of Nations/Data/help.xml, not necessarily *actually* original ever since bark/trireme changes
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(helpXMLOrig);

                    XmlNode node = doc.SelectSingleNode("ROOT/TOPMENU/ENTRY[@name='cbp_status']");//main menu
                    node.ChildNodes[0].InnerText = GenerateMainMenuText();

                    XmlNode node2 = doc.SelectSingleNode("ROOT/SETUPWIN/BUTTON/ENTRY[@name='CBP_STATUS']");//actual game lobby (pick nations, change rules)
                    node2.ChildNodes[0].InnerText = GenerateOtherMenuText();

                    XmlNode node3 = doc.SelectSingleNode("ROOT/GAMESPYTITLE/BUTTON/ENTRY[@name='CBP_STATUS']");//general multiplayer lobby (see list of open lobbies)
                    node3.ChildNodes[0].InnerText = GenerateOtherMenuText();// we use the same string as node2

                    // log the strings
                    CBPLogger.GetInstance.Debug("Main menu readout: " + node.ChildNodes[0].InnerText);
                    CBPLogger.GetInstance.Debug("Other readout (1): " + node2.ChildNodes[0].InnerText);
                    CBPLogger.GetInstance.Debug("Other readout (2): " + node3.ChildNodes[0].InnerText);

                    doc.Save(helpXMLOrig);
                }
                catch (Exception ex)
                {
                    CBPLogger.GetInstance.Error("Error updating menu tooltips: " + ex);
                    MessageBox.Show("Error updating menu tooltips: " + ex);
                }
            }
        }

        private string GenerateMainMenuText()
        {
            string config = "CBP configuration: ";
            string config2 = TooltipConfig();
            string primary = " ============================================ Primary files: ";
            string primary2 = TooltipPrimary();
            string secondary = " --------------------------------------------------------------------------------- Secondary files: ";
            string secondary2 = TooltipSecondary();
            string optional = " --------------------------------------------------------------------------------- Optional changes: ";
            string optional2 = TooltipOptional();
            string plugin = " ============================================ Loaded plugins: ";
            string plugin2 = TooltipPlugins();

            // the true is a placeholder (and probably the primary2 as well), but what was this for? maybe placeholder for plugins text? (which is intentionally not currently supported)
            /*if (true)
            {
                primary2 = "All primary files loaded";
            }*/

            return config + config2 + primary + primary2 + secondary + secondary2 + optional + optional2 + plugin + plugin2;
        }

        private string GenerateOtherMenuText()
        {
            return "CBP is enabled. Configuration: " + TooltipConfig() + ". See main menu for more details.";
        }

        private string TooltipConfig()
        {
            string configFirst = "undefined";
            string configSecond = "undefined";
            bool primary = Properties.Settings.Default.UsePrimaryFileList;
            bool secondary = Properties.Settings.Default.UseSecondaryFileList;

            if (primary && secondary)
            {
                configFirst = "Standard";
                configSecond = "(default)";
            }
            else if ((primary == true) && (secondary == false))
            {
                configFirst = "Minimal";
            }
            else
            {
                configFirst = "Custom";
            }
            if (Properties.Settings.Default.OptionalAsianHeli || Properties.Settings.Default.OptionalEmotes || Properties.Settings.Default.OptionalRadarJam || Properties.Settings.Default.OptionalAsianSpy)
            {
                configSecond = "(with optional change(s))";
            }

            return configFirst + " " + configSecond;
        }

        private string TooltipPrimary()
        {
            if (Properties.Settings.Default.UsePrimaryFileList)
                return "All primary files loaded";
            else
                return "Unknown configuration";
        }

        private string TooltipSecondary()
        {
            if (Properties.Settings.Default.UseSecondaryFileList)
                return "All secondary files loaded";
            else
                return "Secondary files not loaded";//later on when individual files can be selected, this will be more relevant (and will need expansion)
        }

        private string TooltipOptional()
        {
            string optList = "";
            if (Properties.Settings.Default.OptionalAsianHeli)
                optList += "Asian Attack Helicopter, ";
            if (Properties.Settings.Default.OptionalEmotes)
                optList += "Modernised Emotes, ";
            if (Properties.Settings.Default.OptionalRadarJam)
                optList += "Reduced Radar Jam Effect, ";
            if (Properties.Settings.Default.OptionalAsianSpy)
                optList += "Modern Asian Spy";

            if (string.IsNullOrEmpty(optList))
                optList = "None";

            return optList;
        }

        private string TooltipPlugins()
        {
            pluginList = ReadExtensions();
            int pluginCounter = 0;

            foreach (IPluginCBP plugin in pluginList)
            {
                plugin.DoSomething(workshopPath, localMods);
                
                // if plugin is loaded, add title to string for later display in menu status readout
                if (plugin.CheckIfLoaded())
                {
                    if (pluginCounter == 0)
                        pluginTitles += plugin.PluginTitle;
                    else
                        pluginTitles += ", " + plugin.PluginTitle;

                    pluginCounter++;
                }
            }
            if (string.IsNullOrEmpty(pluginTitles))
                pluginTitles = "None";

            return pluginTitles;
        }

        // section for the optional changes configuration
        private async Task ConfigureOptionalChanges()//the UI button is wired to this function; counter: 0
                                                     //might need to disable the load/unload buttons while this is active? uncommon edge case but it's possible someone will press it
        {
            CheckCurrentPath();

            await OptionalAsianHeli();
            new Skins.OptionalChangeWindow().Show();
        }

        private void CheckCurrentPath()
        {
            // old paths from before fixing general RoN-mod-files OoS problem
            /*if (Properties.Settings.Default.CBPLoaded)
            {
                currentPathCBP = localPathCBP;
                currentPathOpt = Path.Combine(localPathCBP, "Optional changes");
            }
            else
            {
                currentPathCBP = Path.Combine(unloadedModsPath, "Community Balance Patch");
                currentPathOpt = Path.Combine(unloadedModsPath, "Community Balance Patch", "Optional changes");
            }*/

            // updated paths
            currentPathCBP = folderCBPmodded;

            if (Properties.Settings.Default.CBPLoaded)
            {
                currentPathOpt = Path.Combine(localPathCBP, "Optional changes");
            }
            else
            {
                currentPathOpt = Path.Combine(unloadedModsPath, "Community Balance Patch", "Optional changes");
            }
        }

        private async Task<BitmapSource> GetTGA(string filepath)
        {
            if (File.Exists(filepath))
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    TgaImage tga = new TgaImage(reader);
                    return tga.GetBitmap();
                }
            }
            else
            {
                CBPLogger.GetInstance.Warning("Unable to find image: " + filepath);
                return null;
            }
        }

        private async Task OptionalChangeUseExisting()
        {
            // no file IO required, but still need to prepare next the configuration for the next optional change
            // since no external function (doing file I/O) is run that will increment the counter, it needs to be incremented here instead for this option
            if (optCounter == 0)
            {
                CBPLogger.GetInstance.Warning("I sent you a radio report, a helicopter, and a guy in a rowboat. What the hell are you doing here?");
            }
            if (optCounter == 1) //asian heli
            {
                CBPLogger.GetInstance.Info("First optional change: use existing.");

                optCounter++;
                await OptionalEmotes();
                return;
            }
            if (optCounter == 2) //modern emotes
            {
                CBPLogger.GetInstance.Info("Second optional change: use existing.");

                optCounter++;
                await OptionalJamRadar();
                return;
            }
            if (optCounter == 3) //radar jam
            {
                CBPLogger.GetInstance.Info("Third optional change: use existing.");

                optCounter++;
                await OptionalAsianSpy();
                return;
            }
            if (optCounter == 4) //asian spy
            {
                CBPLogger.GetInstance.Info("Fourth optional change: use existing.");

                optCounter = 0;
                await OptionalCompleted();
                return;
            }
        }

        private async Task OptionalChangeUseDefault()// called by relaycommand?
        {
            CheckCurrentPath();

            // quick research suggests switch is not higher performance at low counts (5~10ish)
            if (optCounter == 0)
            {
                CBPLogger.GetInstance.Warning("I sent you a radio report, a helicopter, and a guy in a rowboat. What the hell are you doing here?");
            }
            if (optCounter == 1) //asian heli
            {
                CBPLogger.GetInstance.Info("First optional change: use default.");

                // overwrite the current asian heli texture with default texture
                string currentHeli = Path.Combine(currentPathCBP, @"art/attackchopper_asian.tga");
                string defaultHeli = Path.Combine(currentPathOpt, @"art/Original/attackchopper_asian.tga");
                File.Copy(defaultHeli, currentHeli, true);

                Properties.Settings.Default.OptionalAsianHeli = false;
                SaveSettings();

                // prepare modern emotes
                optCounter++;
                await OptionalEmotes();
                return;
            }
            if (optCounter == 2) //modern emotes
            {
                CBPLogger.GetInstance.Info("Second optional change: use default.");

                // overwrite the current emotes texture with default texture
                string currentEmotes = Path.Combine(currentPathCBP, @"art/iface_resources2_CBP.tga");
                string defaultEmotes = Path.Combine(currentPathOpt, @"art/Original/iface_resources2_CBP.tga");
                File.Copy(defaultEmotes, currentEmotes, true);

                Properties.Settings.Default.OptionalEmotes = false;
                SaveSettings();

                // prepare radar jam
                optCounter++;
                await OptionalJamRadar();
                return;
            }
            if (optCounter == 3) //radar jam
            {
                CBPLogger.GetInstance.Info("Third optional change: use default.");

                // overwrite the current radar jam texture with default texture
                string currentJam = Path.Combine(currentPathCBP, @"art/jamradar.tga");
                string defaultJam = Path.Combine(currentPathOpt, @"art/Original/jamradar.tga");
                File.Copy(defaultJam, currentJam, true);

                Properties.Settings.Default.OptionalRadarJam = false;
                SaveSettings();

                // prepare asian spy
                optCounter++;
                await OptionalAsianSpy();
                return;
            }
            if (optCounter == 4) //asian spy
            {
                CBPLogger.GetInstance.Info("Fourth optional change: use default.");

                // overwrite the current modern asian spy model AND texture with default of each
                string currentSpyTex = Path.Combine(currentPathCBP, @"art/Spy_6_asian.tga");
                string defaultSpyTex = Path.Combine(currentPathOpt, @"art/Original/Spy_6_asian.tga");
                File.Copy(defaultSpyTex, currentSpyTex, true);

                string currentSpyModel = Path.Combine(currentPathCBP, @"art/Spy_6_asian.BH3");
                string defaultSpyModel = Path.Combine(currentPathOpt, @"art/Original/Spy_6_asian.BH3");
                File.Copy(defaultSpyModel, currentSpyModel, true);

                Properties.Settings.Default.OptionalAsianSpy = false;
                SaveSettings();

                //the last optional change to configure should reset the counter to guarantee no persistence issues
                //because of how I set things up (simpler), it also can't easily close the window lol
                optCounter = 0;
                await OptionalCompleted();
                return;
            }
        }

        private async Task OptionalChangeUseReplacement()// called by relaycommand?
        {
            CheckCurrentPath();

            // quick research suggests switch is not higher performance at low counts (5~10ish)
            if (optCounter == 0)
            {
                CBPLogger.GetInstance.Warning("I sent you a radio report, a helicopter, and a guy in a rowboat. What the hell are you doing here?");
            }
            if (optCounter == 1) //asian heli
            {
                CBPLogger.GetInstance.Info("First optional change: use replacement.");

                // overwrite the current asian heli texture with default texture
                string currentHeli = Path.Combine(currentPathCBP, @"art/attackchopper_asian.tga");
                string replacementHeli = Path.Combine(currentPathOpt, @"art/attackchopper_asian.tga");
                File.Copy(replacementHeli, currentHeli, true);

                Properties.Settings.Default.OptionalAsianHeli = true;
                SaveSettings();

                // prepare modern emotes
                optCounter++;
                await OptionalEmotes();
                return;
            }
            if (optCounter == 2) //modern emotes
            {
                CBPLogger.GetInstance.Info("Second optional change: use replacement.");

                // overwrite the current emotes texture with default texture
                string currentEmotes = Path.Combine(currentPathCBP, @"art/iface_resources2_CBP.tga");
                string replacementEmotes = Path.Combine(currentPathOpt, @"art/iface_resources2_CBP.tga");
                File.Copy(replacementEmotes, currentEmotes, true);

                Properties.Settings.Default.OptionalEmotes = true;
                SaveSettings();

                // prepare radar jam
                optCounter++;
                await OptionalJamRadar();
                return;
            }
            if (optCounter == 3) //radar jam
            {
                CBPLogger.GetInstance.Info("Third optional change: use replacement.");

                // overwrite the current radar jam texture with default texture
                string currentJam = Path.Combine(currentPathCBP, @"art/jamradar.tga");
                string replacementJam = Path.Combine(currentPathOpt, @"art/jamradar.tga");
                File.Copy(replacementJam, currentJam, true);

                Properties.Settings.Default.OptionalRadarJam = true;
                SaveSettings();

                // prepare asian spy
                optCounter++;
                await OptionalAsianSpy();
                return;
            }
            if (optCounter == 4) //asian spy
            {
                CBPLogger.GetInstance.Info("Fourth optional change: use replacement.");

                // overwrite the current modern asian spy model AND texture with default of each
                string currentSpyTex = Path.Combine(currentPathCBP, @"art/Spy_6_asian.tga");
                string replacementSpyTex = Path.Combine(currentPathOpt, @"art/Spy_6_asian.tga");
                File.Copy(replacementSpyTex, currentSpyTex, true);

                string currentSpyModel = Path.Combine(currentPathCBP, @"art/Spy_6_asian.BH3");
                string replacementSpyModel = Path.Combine(currentPathOpt, @"art/Spy_6_asian.BH3");
                File.Copy(replacementSpyModel, currentSpyModel, true);

                Properties.Settings.Default.OptionalAsianSpy = true;
                SaveSettings();

                //the last optional change to configure should reset the counter to guarantee no persistence issues
                //because of how I set things up (simpler), it also can't easily close the window lol
                optCounter = 0;
                await OptionalCompleted();
                return;
            }
        }

        private async Task OptionalAsianHeli()//counter: 1
        {
            CheckCurrentPath();

            // title, description, compatibility (optional changes are currently all compatible)
            OptTitle = "Asian Attack Helicopter (texture)";
            OptDescription = "In the original 2003 RoN release, Asian Attack Helicopters used a black texture. In T&P, this graphics entry was replaced with a new skin for a Russian version of the unit, "
                + "so East-Asian nations ended up using the default unit skin. The game supports having both the Russian and Asian unit skins at the same time, so it's unclear why the Asian skin was removed. "
                + "This change restores the removed unit skin for Asian nations. Other nations are not affected.";
            OptCompatibility = "This change is fully multiplayer-compatible.";

            // preview image (PNG/JPG)
            string previewPath = Path.Combine(currentPathOpt, @"art/Preview/RoN - CBP optional change asian attack helicopter preview small.png");
            if (File.Exists(previewPath))
            {
                //OptPreview = new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                await PreparePreview(previewPath);
            }
            else
                CBPLogger.GetInstance.Warning("Unable to find preview image.");
            {
                // existing image (TGA)
                string currentPath = Path.Combine(currentPathCBP, @"art/attackchopper_asian.tga");
                OptCurrent = await GetTGA(currentPath);

                // default image (TGA)
                string originalPath = Path.Combine(currentPathOpt, @"art/Original/attackchopper_asian.tga");
                OptOriginal = await GetTGA(originalPath);

                // replacement image (TGA)
                string replacementPath = Path.Combine(currentPathOpt, @"art/attackchopper_asian.tga");
                OptReplacement = await GetTGA(replacementPath);
            }

            optCounter++;
        }

        private async Task OptionalEmotes()//counter: 2
        {
            CheckCurrentPath();

            // title, description, compatibility (optional changes are currently all compatible)
            OptTitle = "Modernised Emotes (texture)";
            OptDescription = "In the original 2003 RoN release, a number of emotes/emoticons are available. These were removed in T&P for unknown reasons, but are restored by default in CBP. "
                + "This change replaces those original emotes/emoticons with more modern equivalents from the Twitter emoji set.";
            OptCompatibility = "This change is fully multiplayer-compatible.";

            // preview image (PNG/JPG)
            string previewPath = Path.Combine(currentPathOpt, @"art/Preview/RoN - CBP optional change emotes classic vs modern preview.png");
            if (File.Exists(previewPath))
            {
                //OptPreview = new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                await PreparePreview(previewPath);
            }
            else
                CBPLogger.GetInstance.Warning("Unable to find preview image.");
            {
                // existing image (TGA)
                string currentPath = Path.Combine(currentPathCBP, @"art/iface_resources2_CBP.tga");
                OptCurrent = await GetTGA(currentPath);

                // default image (TGA)
                string originalPath = Path.Combine(currentPathOpt, @"art/Original/iface_resources2_CBP.tga");
                OptOriginal = await GetTGA(originalPath);

                // replacement image (TGA)
                string replacementPath = Path.Combine(currentPathOpt, @"art/iface_resources2_CBP.tga");
                OptReplacement = await GetTGA(replacementPath);
            }

            if (Properties.Settings.Default.CBPLoaded)
                await GenerateDynamicHelpText();
        }

        private async Task OptionalJamRadar()//counter: 3
        {
            CheckCurrentPath();

            // title, description, compatibility (optional changes are currently all compatible)
            OptTitle = "Reduce Visual Intensity of Radar Jamming Effect (texture)";
            OptDescription = "Despite the radar jamming effect using identical textures in both EE and non-EE versions of RoN, there seems to be a graphics bug (I think it's a bug with EE's shaders) "
                + "which causes some effects such as radar jamming and nuke detonations to be pure white and substantially more prominent than originally intended. "
                + "This change modifies the jamming texture to somewhat reduce (but not remove) its intensity because of this bug.";
            OptCompatibility = "This change is fully multiplayer-compatible.";

            // preview image (PNG/JPG)
            string previewPath = Path.Combine(currentPathOpt, @"art/Preview/RoN - CBP optional change radar jam preview.png");
            if (File.Exists(previewPath))
            {
                //OptPreview = new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                await PreparePreview(previewPath);
            }
            else
                CBPLogger.GetInstance.Warning("Unable to find preview image.");
            {
                // existing image (TGA)
                string currentPath = Path.Combine(currentPathCBP, @"art/jamradar.tga");
                OptCurrent = await GetTGA(currentPath);

                // default image (TGA)
                string originalPath = Path.Combine(currentPathOpt, @"art/Original/jamradar.tga");
                OptOriginal = await GetTGA(originalPath);

                // replacement image (TGA)
                string replacementPath = Path.Combine(currentPathOpt, @"art/jamradar.tga");
                OptReplacement = await GetTGA(replacementPath);
            }

            if (Properties.Settings.Default.CBPLoaded)
                await GenerateDynamicHelpText();
        }

        private async Task OptionalAsianSpy()//counter: 4
        {
            CheckCurrentPath();

            // title, description, compatibility (optional changes are currently all compatible)
            OptTitle = "Modern Asian Spy (model + texture)";
            OptDescription = "In the original 2003 RoN release, all Spies gain a new look in the later ages. In T&P this was removed for the modern Asian Spy. "
                + "That original texture has significant visibility issues, particularly in snowy terrain, and so this may have been intentionally removed for gameplay reasons. "
                + "The original (removed) texture has been enhanced with lots of contrast and visbility so that Asian nations can have their own modern Spy again (if you want it!)."
                + "\n(preview: left is new texture, right is early-game texture)";
            OptCompatibility = "This change is fully multiplayer-compatible.";

            // preview image (PNG/JPG)
            string previewPath = Path.Combine(currentPathOpt, @"art/Preview/RoN - CBP optional change modern asian spy preview.png");
            if (File.Exists(previewPath))
            {
                //OptPreview = new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                await PreparePreview(previewPath);
            }
            else
                CBPLogger.GetInstance.Warning("Unable to find preview image.");
            {
                // existing image (TGA)
                string currentPath = Path.Combine(currentPathCBP, @"art/Spy_6_asian.tga");
                OptCurrent = await GetTGA(currentPath);

                // default image (TGA)
                string originalPath = Path.Combine(currentPathOpt, @"art/Original/Spy_6_asian.tga");
                OptOriginal = await GetTGA(originalPath);

                // replacement image (TGA)
                string replacementPath = Path.Combine(currentPathOpt, @"art/Spy_6_asian.tga");
                OptReplacement = await GetTGA(replacementPath);
            }

            if (Properties.Settings.Default.CBPLoaded)
                await GenerateDynamicHelpText();
        }

        private async Task OptionalCompleted()
        {
            CheckCurrentPath();

            OptTitle = "All Optional Changes Configured!";
            OptDescription = "";
            OptCompatibility = "";

            // preview image (PNG/JPG)
            string previewPath = Path.Combine(currentPathOpt, @"art/Preview/RoN victory 01.png");
            if (File.Exists(previewPath))
            {
                //OptPreview = new BitmapImage(new Uri(previewPath, UriKind.RelativeOrAbsolute));
                await PreparePreview(previewPath);
            }
            else
                CBPLogger.GetInstance.Warning("Unable to find preview image.");

            OptCurrent = null;
            OptOriginal = null;
            OptReplacement = null;

            if (Properties.Settings.Default.CBPLoaded)
                await GenerateDynamicHelpText();
        }

        private async Task PreparePreview(string previewPath)
        {
            // ported from vb.net https://stackoverflow.com/questions/6430299/bitmapimage-in-wpf-does-lock-file
            // doing it this way instead prevents a file lock (from the preview image of all things lol)
            // I'm sure there are other ways to accomplish this such as just sourcing the image differently, but this works fine too
            BitmapImage bmi = new BitmapImage();
            bmi.BeginInit();
            bmi.CacheOption = BitmapCacheOption.OnLoad;
            bmi.UriSource = new Uri(previewPath, UriKind.RelativeOrAbsolute);
            bmi.EndInit();

            OptPreview = bmi;
        }

        //quite high code redundancy, but I'm exhausted and this works and isn't that hard to read
        //this should also have its own try/catch (right now the place where it's referenced has it instead)
        private async Task OptionalMaintainSelection()
        {
            try
            {
                CheckCurrentPath();

                if (Properties.Settings.Default.OptionalAsianHeli)
                {
                    string currentHeli = Path.Combine(currentPathCBP, @"art/attackchopper_asian.tga");
                    string replacementHeli = Path.Combine(currentPathOpt, @"art/attackchopper_asian.tga");
                    File.Copy(replacementHeli, currentHeli, true);
                }
                if (Properties.Settings.Default.OptionalEmotes)
                {
                    string currentEmotes = Path.Combine(currentPathCBP, @"art/iface_resources2_CBP.tga");
                    string replacementEmotes = Path.Combine(currentPathOpt, @"art/iface_resources2_CBP.tga");
                    File.Copy(replacementEmotes, currentEmotes, true);
                }
                if (Properties.Settings.Default.OptionalRadarJam)
                {
                    string currentJam = Path.Combine(currentPathCBP, @"art/jamradar.tga");
                    string replacementJam = Path.Combine(currentPathOpt, @"art/jamradar.tga");
                    File.Copy(replacementJam, currentJam, true);
                }
                if (Properties.Settings.Default.OptionalAsianSpy)
                {
                    string currentSpyTex = Path.Combine(currentPathCBP, @"art/Spy_6_asian.tga");
                    string replacementSpyTex = Path.Combine(currentPathOpt, @"art/Spy_6_asian.tga");
                    File.Copy(replacementSpyTex, currentSpyTex, true);

                    string currentSpyModel = Path.Combine(currentPathCBP, @"art/Spy_6_asian.BH3");
                    string replacementSpyModel = Path.Combine(currentPathOpt, @"art/Spy_6_asian.BH3");
                    File.Copy(replacementSpyModel, currentSpyModel, true);
                }
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error($"Error maintaining optional changes: {ex}");
                MessageBox.Show($"Error maintaining optional changes: {ex}");
            }
            
        }

        // plugins section (but not all of it, some of it is in codebehind of modmanager tabs lol)
        // tells user if plugins are incompatible with CBP
        private bool CheckPluginCompatbilityIssue()
        {
            if ((Properties.Settings.Default.PluginCompatibilityIssue == true) && (Properties.Settings.Default.CBPLoaded == true))
            {
                return true;
            }
            else return false;
        }

        private bool CheckMultiplayerIssue()
        {
            if ((Properties.Settings.Default.MultiplayerCompatibilityIssue == true) && (Properties.Settings.Default.CBPLoaded == true))
            {
                return true;
            }
            else return false;
        }

        private void LoadPlugins()
        {
            try
            {//can use plugin.LoadResult for logging
                if (!File.Exists(Path.Combine(localMods, @"..\", "riseofnations.exe")))
                {
                    CBPLogger.GetInstance.Warning("Not running in expected folder; mod loading aborted.");
                    return;
                }

                pluginList = ReadExtensions();
                CBPLogger.GetInstance.Info($"{pluginList.Count} plugin(s) found");
                int pluginCounter = 0;

                foreach (IPluginCBP plugin in pluginList)
                {
                    plugin.DoSomething(workshopPath, localMods);
                    plugin.UpdatePlugin(workshopPath, localMods);
                    CBPLogger.GetInstance.Info(plugin.LoadResult);
                    CBPLogger.GetInstance.Info($"{plugin.PluginTitle} {plugin.PluginVersion} ({plugin.CBPCompatible}) by {plugin.PluginAuthor} | {plugin.PluginDescription}");
                    CBPLogger.GetInstance.Info("\nPlugin location: " + pluginsPathList[pluginCounter]);
                    pluginCounter++;
                    CBPLogger.GetInstance.Info("====================");
                }

                CheckPluginCompatibility();

                if (pluginList != null)
                {
                    Properties.Settings.Default.AnyPluginsLoaded = true;
                    SaveSettings();

                    CBPLogger.GetInstance.Info("Any plugins with auto-updating logic have been given a chance to run their logic.");
                }
                else
                {
                    Properties.Settings.Default.AnyPluginsLoaded = false;
                    SaveSettings();

                    CBPLogger.GetInstance.Info("No plugins detected.");
                }
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error("Error loading plugins: " + ex);
                MessageBox.Show("Error loading plugins: " + ex);
            }
        }

        private List<IPluginCBP> ReadExtensions()
        {
            // 0 we don't have all plugins in a single directory, they're actually distributed across multiple subfolders of a known folder location
            // (e.g. we know it's D:\Example, but it could be D:\Example\Arb or D:\Example\Arbitrary or both or neither)
            List<IPluginCBP> pluginsList = new List<IPluginCBP>();

            DirectoryInfo di = new DirectoryInfo(workshopPath);
            DirectoryInfo[] diArr = di.GetDirectories();
            foreach (DirectoryInfo dri in diArr)
            {
                // 1) Read dll files from the specified location (a mod folder in our case)

                CBPLogger.GetInstance.Debug("Plugin folder: " + dri.FullName);
                string pluginFolder = dri.FullName;
                string[] files = Directory.GetFiles(pluginFolder, "*.dll", SearchOption.TopDirectoryOnly);

                // 2) Read from those files
                foreach (string file in files)
                {
                    Assembly assembly = Assembly.LoadFile(Path.Combine(pluginFolder, file));

                    // 3) Extract all the types that implement PluginCBP
                    try
                    {
                        Type[] pluginTypes = assembly.GetTypes().Where(t => typeof(IPluginCBP).IsAssignableFrom(t) && !t.IsInterface).ToArray();

                        foreach (Type pluginType in pluginTypes)
                        {
                            // 4) Creates new instance of the extracted type (PluginCBP?)
                            object pluginInstance = Activator.CreateInstance(pluginType) as IPluginCBP;
                            pluginsList.Add((IPluginCBP)pluginInstance);
                            pluginsPathList.Add(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        CBPLogger.GetInstance.Error("Error loading plugins (RE): " + ex);
                        MessageBox.Show("Error loading plugins (RE): " + ex);
                    }
                }
            }
            return pluginsList;
        }

        private void CheckPluginCompatibility()
        {
            Properties.Settings.Default.PluginCompatibilityIssue = false;
            Properties.Settings.Default.MultiplayerCompatibilityIssue = false;

            foreach (IPluginCBP plugin in pluginList)
            {
                if ((plugin.CheckIfLoaded() == true))
                {
                    // we want these two compares to be independent so that we can end up with 4 different states (to present user with more specific message(s))
                    if (plugin.CBPCompatible == false)
                        Properties.Settings.Default.PluginCompatibilityIssue = true;

                    if (plugin.DefaultMultiplayerCompatible == false)
                        Properties.Settings.Default.MultiplayerCompatibilityIssue = true;
                }
            }
            SaveSettings();
        }

        private void PluginSecurityWarning()
        {
            if (Properties.Settings.Default.FirstTimePlugins)
            {
                MessageBox.Show("Plugins can potentially be a security risk, so you should only use plugins that you trust.", "Plugin Security Warning", MessageBoxButton.OK, MessageBoxImage.Information);

                Properties.Settings.Default.FirstTimePlugins = false;
                SaveSettings();
            }
        }

        private async Task RecheckFileVersions()//unfortunately needed because user might load plugin in one CBP state, but unload it in another (or vice versa)
        {
            try
            {
                Version localVersion = new Version(File.ReadAllText(versionFileCBPLocal));//at some point this should definitely be spun out into a less-localised variable so that it can be used in multiple places
                int fileProblems = 0;

                // check each file in the modded list and make sure it's an up-to-date CBP file
                foreach (string filename in CBPFileListModded)
                {
                    if (CheckIfCBPFile(filename) == true)
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is a CBP file (just as we want)...");

                        //it's a CBP file (but not necessarily the right version, so deal with that too)
                        string text = File.ReadLines(Path.Combine(RoNDataPath, filename)).Skip(2).Take(1).First();
                        Version fileVersion = new Version(text.Substring(9, 11));

                        if (fileVersion.IsDifferentThan(localVersion))// I assume it's faster to check this than straight up always-write files
                        {
                            CBPLogger.GetInstance.Debug("..but isn't up to date (no action taken).");
                            fileProblems++;
                        }
                        //else no action required
                    }
                    else
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is not a CBP file, but should be (no action taken)");
                        fileProblems++;
                    }
                }

                // check each file in the original list and make sure it's **NOT** a CBP file (we don't care if it's user-modded, since they did that themselves)
                foreach (string filename in CBPFileListOriginal)
                {
                    if (CheckIfCBPFile(filename) == true)
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is a CBP file, but shouldn't be (no action taken).");
                        fileProblems++;
                    }
                    else
                    {
                        CBPLogger.GetInstance.Debug($"{filename} is not a CBP file (just as we want) - no action taken.");
                    }
                }

                if (fileProblems > 0)
                {
                    pluginFileProblem = true;
                }
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error($"Error checking data files (plugin compatibility): {ex}");
                MessageBox.Show("Error checking data files (plugin compatibility):\n\n" + ex);
                //LogManager.Shutdown();
                //Environment.Exit(-1);
                //don't want to necessarily shut down just for this error
            }
        }

        //settings section
        private void ResetSettings(bool showMessage)
        {
            //Properties.Settings.Default.Reset();
            WriteDefaultSettings();

            Properties.Settings.Default.JustReset = true;
            SaveSettings();

            CBPLogger.GetInstance.Info("Settings reset.");
            if (showMessage)
            {
                MessageBox.Show($"Settings reset. Please restart program to load default settings.");
            }    
        }

        //dumb way to avoid having to distribute the config file (people are already having trouble following directions for two exes, I don't want to complicate it further)
        private void WriteDefaultSettings()
        {
            Properties.Settings.Default.DefaultCBP = true;
            Properties.Settings.Default.CBPUnloaded = false;
            Properties.Settings.Default.UseWorkshopFiles = true;
            Properties.Settings.Default.CBPLoaded = false;
            Properties.Settings.Default.UpgradeRequired = true;
            Properties.Settings.Default.NoWorkshopFiles = false;
            Properties.Settings.Default.RoNPathSetting = "no path";
            Properties.Settings.Default.UnloadWorkshopToo = false;
            Properties.Settings.Default.CBPArchive = true;
            Properties.Settings.Default.UsePrerelease = false;
            Properties.Settings.Default.OldFilesRenamed = false;
            Properties.Settings.Default.UseDefaultLauncher = true;
            Properties.Settings.Default.SkinSpV1 = false;
            Properties.Settings.Default.DefaultLauncherAnswered = false;
            Properties.Settings.Default.UsePrimaryFileList = true;
            Properties.Settings.Default.UseSecondaryFileList = true;
            Properties.Settings.Default.FilesBackedUp = false;
            Properties.Settings.Default.DetectBullshit = false;
            Properties.Settings.Default.FirstTimeRun = true;
            Properties.Settings.Default.NonDataFilesBackedUp = false;
            Properties.Settings.Default.ArtFilesCopied = false;
            Properties.Settings.Default.OptionalAsianHeli = false;
            Properties.Settings.Default.OptionalEmotes = false;
            Properties.Settings.Default.OptionalRadarJam = false;
            Properties.Settings.Default.OptionalAsianSpy = false;
            Properties.Settings.Default.OptionalMaintain = true;
            Properties.Settings.Default.AddIconGameName = true;
            Properties.Settings.Default.PluginCompatibilityIssue = false;
            Properties.Settings.Default.FirstTimePlugins = true;
            Properties.Settings.Default.AnyPluginsLoaded = false;
            Properties.Settings.Default.JustReset = false;
            Properties.Settings.Default.UseFancyLogging = false;
            Properties.Settings.Default.MultiplayerCompatibilityIssue = false;
            Properties.Settings.Default.WarnCompatibility = true;
            Properties.Settings.Default.DisablePluginLoading = false;
            Properties.Settings.Default.FuckStopTellingMe = false;
            Properties.Settings.Default.MicroSkin = false;
            Properties.Settings.Default.ArchiveDelete = false;

            SaveSettings();

            CBPLogger.GetInstance.Info("Default settings manually written.");
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

        private void OptionalMaintain_Inversion()
        {
            Properties.Settings.Default.OptionalMaintain = !Properties.Settings.Default.OptionalMaintain;
            SaveSettings();
        }

        private void AddIconGameName_Inversion()
        {
            Properties.Settings.Default.AddIconGameName = !Properties.Settings.Default.AddIconGameName;
            SaveSettings();
        }

        private void UseFancyLogging_Inversion()
        {
            Properties.Settings.Default.UseFancyLogging = !Properties.Settings.Default.UseFancyLogging;
            SaveSettings();
        }

        private void WarnCompatibility_Inversion()
        {
            Properties.Settings.Default.WarnCompatibility = !Properties.Settings.Default.WarnCompatibility;
            SaveSettings();
        }

        private void DisablePluginLoading_Inversion()
        {
            Properties.Settings.Default.DisablePluginLoading = !Properties.Settings.Default.DisablePluginLoading;
            SaveSettings();
        }

        private void ArchiveDelete_Inversion()
        {
            Properties.Settings.Default.ArchiveDelete = !Properties.Settings.Default.ArchiveDelete;
            SaveSettings();
        }

        private void OverridePathPopup()
        {
            //clunky (should probably use a while loop?) but functional
            OverridePathStart:
            
            string newPath = Interaction.InputBox("This will allow you to override an automatically-detected RoN path with a new path. If you've reinstalled your game but in a new location, this may be required."
                + "\n\nNote that this path will need to be input again if settings are reset.", "Override RoN Path");

            if (File.Exists(Path.Combine(newPath, "riseofnations.exe")))
            {
                // directly save the path
                Properties.Settings.Default.RoNPathSetting = newPath;
                SaveSettings();

                MessageBox.Show("New path saved.\n\nPlease restart CBP Launcher to ensure the new path is loaded correctly.");
            }
            else
            {
                // tell user invalid path, ask if they want to try again
                string message = $"Rise of Nations install not detected in that location. "
                               + "The path needs to be the folder that riseofnations.exe is located in (but not including the executable itself in that path)."
                               + "\n\n Would you like to try entering a path again?";

                if (MessageBox.Show(message, "Invalid Path", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    goto OverridePathStart;
                }
                else
                {
                    CBPLogger.GetInstance.Warning("RoN path override provided by user was not valid.");
                }
            }
        }

        private async Task ReplaceRestoreDefaultLauncher()
        {
            //IMPLEMENTATION INCOMPLETE(?)
            
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

                        CBPLogger.GetInstance.Info("Have attempted to restore original launcher.");
                        MessageBox.Show("Have attempted to restore original launcher - it should be active next time RoN is started. To use CBP Launcher again re-check this box or re-run first time setup and then choose the appropriate option(s).");
                    }
                    else
                    {
                        CBPLogger.GetInstance.Warning("CBP Setup GUIwas still running when restoring launcher, so no action has been taken.");
                        MessageBox.Show("Minor error: CBP Setup GUI seems to still be running so no action has been taken (but this might make the checkbox seem wonky until CBP Launcher is restarted).");
                    }
                }
                catch (Exception ex)
                {
                    CBPLogger.GetInstance.Error("Error restoring original launcher: " + ex);
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

                        CBPLogger.GetInstance.Info("Have attempted to replace original launcher.");
                        MessageBox.Show("Have attempted to replace original launcher - CBP Launcher should be active when RoN is started.");
                    }
                    else
                    {
                        CBPLogger.GetInstance.Warning("CBP Setup GUIwas still running when replacing launcher, so no action has been taken.");
                        MessageBox.Show("Minor error: CBP Setup GUI seems to still be running so no action has been taken (but this might make the checkbox seem wonky until CBP Launcher is restarted).");
                    }
                }
                catch (Exception ex)
                {
                    CBPLogger.GetInstance.Error("Error replacing original launcher: " + ex);
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
            // don't want to import settings if they were just reset (otherwise that defeats the purpose)
            if (Properties.Settings.Default.JustReset == false)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                SaveSettings();

                // removed this message to reduce popups for first-timer users
                //MessageBox.Show("Have attempted to import settings from previous version of CBP Launcher (if these settings exist).");
                CBPLogger.GetInstance.Info("Settings upgraded (if old settings exist).");
            }
        }

        private async Task AskDefaultLauncher()
        {
            if (Properties.Settings.Default.DefaultLauncherAnswered == false)
            {
                string message = $"CBP Launcher can quickly patch and unpatch RoN:EE, so that you can switch between versions easily if you want to. Do you want CBP Launcher to replace the default launcher?\n\n(This option can be changed at any time)";

                if (MessageBox.Show(message, "Default to CBP Launcher?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Properties.Settings.Default.DefaultLauncherAnswered = true;
                    Properties.Settings.Default.UseDefaultLauncher = false;
                    UseDefaultLauncherCheckbox = false;
                    SaveSettings();
                    await ReplaceRestoreDefaultLauncher();
                    CBPLogger.GetInstance.Info("Using CBP Launcher.");
                }
                else
                {
                    Properties.Settings.Default.DefaultLauncherAnswered = true;
                    Properties.Settings.Default.UseDefaultLauncher = true;
                    UseDefaultLauncherCheckbox = true;
                    SaveSettings();
                    CBPLogger.GetInstance.Info("Using default launcher.");
                }
            }
        }

        private async Task AskDefaultCBP()
        {
            if (Properties.Settings.Default.FirstTimeRun == true)//this is a different variable (than some of the existing ones) right now almost purely because of implementation timing regarding bark/trireme etc
            {
                string message = $"Do you want CBP to be loaded by default when CBP Launcher starts?"
                               + "\n\n(CBP can be manually loaded or unloaded freely regardless of this answer, and this setting can be changed later)";

                if (MessageBox.Show(message, "Default to CBP?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Properties.Settings.Default.FirstTimeRun = false;
                    Properties.Settings.Default.DefaultCBP = true;
                    CBPDefaultCheckbox = true;
                    SaveSettings();
                    CBPLogger.GetInstance.Info("Defaulting to CBP.");
                }
                else
                {
                    Properties.Settings.Default.FirstTimeRun = false;
                    Properties.Settings.Default.DefaultCBP = false;
                    CBPDefaultCheckbox = false;
                    SaveSettings();

                    // we want to ensure CBP files are "on hand" even if the person doesn't want to initially use CBP
                    // (because I don't want to continously handle the edge case of someone who has no CBP files on hand when they're expected by code)
                    // it's rather clunky and friction-y for first-time user, but it's functional
                    await CheckForUpdates();
                    await UnloadCBP();
                    CBPLogger.GetInstance.Info("Defaulting to non-CBP.");
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

            CBPLogger.GetInstance.Debug("RoN path found: " + RoNPathCheck);
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

                if ((versionExists == false) && (abortArchive == false))
                {
                    //get the version BEFORE moving it so that we can rename it on the move action, rather than two separate actions (so that if it errors partway through, we don't get stuck with a no-ID archived CBP folder)
                    Version archiveVersion = new Version(File.ReadAllText(Path.Combine(versionFileCBPLocal)));

                    string archiveVersionNew = VersionArray.versionStart[archiveVersion.major]
                                             + VersionArray.versionMiddle[archiveVersion.minor]
                                             + VersionArray.versionEnd[archiveVersion.subMinor]
                                             + VersionArray.versionHotfix[archiveVersion.hotfix];

                    Directory.Move(Path.Combine(localPathCBP), Path.Combine(archiveCBP, "Community Balance Patch " + "(" + archiveVersionNew + ")"));
                    CBPLogger.GetInstance.Info(archiveVersionNew + " has been archived.");
                    MessageBox.Show(archiveVersionNew + " has been archived.");
                }
                else if ((versionExists == true) && (abortArchive == false))
                {
                    if (MessageBox.Show("An archive was found with the same name as the current version. Do you want to make a new archive of this version but with a (2) at the end? Otherwise no archive will be made, and the current version will not be changed.", "Existing Archive Found", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Version archiveVersion = new Version(File.ReadAllText(Path.Combine(versionFileCBPLocal)));

                        string archiveVersionNew = VersionArray.versionStart[archiveVersion.major]
                                                 + VersionArray.versionMiddle[archiveVersion.minor]
                                                 + VersionArray.versionEnd[archiveVersion.subMinor]
                                                 + VersionArray.versionHotfix[archiveVersion.hotfix];

                        Directory.Move(Path.Combine(localPathCBP), Path.Combine(archiveCBP, "Community Balance Patch " + "(" + archiveVersionNew + ") (2)"));
                        CBPLogger.GetInstance.Info(archiveVersionNew + " has been archived.");
                        MessageBox.Show(archiveVersionNew + " has been archived.");
                    }
                    else
                    {
                        //log
                        CBPLogger.GetInstance.Info("It looks like the version to archive already exists. No action has been taken.");
                        abortWorkshopCopyCBP = true;
                    }
                }
                else if ((versionExists == true) && (abortArchive == true))
                {
                    //log
                    CBPLogger.GetInstance.Info("It looks like the version to archive already exists, so no action has been taken.");
                    abortWorkshopCopyCBP = true;
                }
                else//which means this covers the single use case of versionExists == false && abortArchive == true
                {
                    CBPLogger.GetInstance.Info("Archiving aborted due to abortArchive flag.");
                    abortWorkshopCopyCBP = true;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                CBPLogger.GetInstance.Error($"Error archiving previous CBP version: {ex}");
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
                CBPLogger.GetInstance.Info("Alpha 6c has been archived.");
                MessageBox.Show("Alpha 6c has been archived.");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                CBPLogger.GetInstance.Error($"Error archiving previous CBP version (compatibility for a6c): {ex}");
                MessageBox.Show($"Error archiving previous CBP version (compatibility for a6c): {ex}");
            }
        }

        private async Task DeleteOldVersion()
        {
            try
            {
                MessageBox.Show("localpathCBP = " + localPathCBP);
                //Directory.Delete(localPathCBP);
                //CBPLogger.GetInstance.Info("Old/existing local CBP folder deleted.");
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.loadFailed;
                CBPLogger.GetInstance.Error("Error deleting old CBP version: " + ex);
                MessageBox.Show("Error deleting old CBP version:\n\n" + ex);
            }
        }

        private void GetLauncherVersion()
        {
            //LauncherVersion = "CBP Launcher v" + Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(2);//this is cutting off the first two (rather than last two) numbers
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LauncherVersion = "CBP Launcher v" + assemblyVersion.Remove(assemblyVersion.Length - 2);
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
                CBPLogger.GetInstance.Error("Error while generating lists: " + ex);
                LogManager.Shutdown();
                Environment.Exit(-1);
            }
        }

        private void ConfigureNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            //targets
            
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "${basedir}/CBP/logs/cbplauncher.${shortdate}.log",
                ArchiveFileName = "cbplauncher.log.{#}.txt",
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveDateFormat = "yyyyMMdd",
                Layout = "${time} [${uppercase:${level}}] ${message}"
            };
            var logviewer = new NLog.Targets.ConsoleTarget("logviewer")
            {
                Name = "logviewer",
                Layout = "${time} [${uppercase:${level}}] ${message}"
            };

            //var test = new NLog.Targets.Wrappers.AsyncTargetWrapper("test");

            //rules for targets
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logviewer);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // loading extensions: https://stackoverflow.com/questions/30340414/nlog-extensions-add-assembly-programmatically
            // fancier logging on logging tab (just displays the same things that are already saved to file)
            if (Properties.Settings.Default.UseFancyLogging)
            {
                var assembly = Assembly.Load("NLogViewer");
                NLog.Config.ConfigurationItemFactory.Default.RegisterItemsFromAssembly(assembly);

                DJ.Targets.CacheTarget CacheTarget = new DJ.Targets.CacheTarget();//increases memory footprint by >10MB, seems a little insane for a glorified block of text

                config.AddRule(LogLevel.Debug, LogLevel.Fatal, CacheTarget);
            }

            //apply config
            LogManager.Configuration = config;
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
        public static string[] versionHotfix = new string[21] { "", " (hotfix 1)", " (hotfix 2)", " (hotfix 3)", " (hotfix 4)", " (hotfix 5)", " (hotfix 6)", " (hotfix 7)", " (hotfix 8)", " (hotfix 9)" // 0-9 respectively
                                                              , " (special)" // 10
                                                              , " (PR1)", " (PR2)", " (PR3)", " (PR4)", " (PR5)", " (PR6)", " (PR7)", " (PR8)", " (PR9)", " (PR10+)" }; // 11-19 respectively, then 20 for 10+ because oh god why are there so many PRs
    }
}
