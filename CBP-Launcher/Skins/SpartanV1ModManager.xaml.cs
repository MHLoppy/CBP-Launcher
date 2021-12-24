using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CBPLauncher.Core;
using CBPSDK;

namespace CBPLauncher.Skins
{
    public partial class SpartanV1ModManager : INotifyPropertyChanged
    {
        // keep a second list with the path of each plugin
        private List<string> pluginsPathList = new List<string>();
        public ObservableCollection<string> PTitles { get; private set; }
        public ObservableCollection<string> PAuthor { get; private set; }
        public ObservableCollection<string> PVersion { get; private set; }
        public ObservableCollection<bool> PCompat { get; private set; }
        public ObservableCollection<string> PDescription { get; private set; }

        private List<IPluginCBP> pluginList = null;
        public ObservableCollection<int> PluginCount { get; private set; }

        private string workshopModsPath;
        private string localModsPath;

        public SpartanV1ModManager()
        {
            InitializeComponent();
            DataContext = this;

            //we straight up assume we're in the RoN folder
            workshopModsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..", @"workshop\content\287450"));
            localModsPath = Path.Combine(Directory.GetCurrentDirectory(), "mods");

            if (!File.Exists(Path.Combine(localModsPath, @"..\", "riseofnations.exe")))
            {
                ErrorTextBlock.Text = "Not running in expected folder; mod loading aborted.";
                return;
            }

            pluginList = ReadExtensions();

            PTitles = new ObservableCollection<string>();
            PAuthor = new ObservableCollection<string>();
            PVersion = new ObservableCollection<string>();
            PCompat = new ObservableCollection<bool>();
            PDescription = new ObservableCollection<string>();

            int i = 0;
            int topMargin = 25;
            int infoTopMargin = 23;

            try
            {
                foreach (IPluginCBP plugin in pluginList)
                {
                    plugin.DoSomething(workshopModsPath, localModsPath);

                    // if on/off plugin, make checkbox
                    // if not, make a "play" button? - also have to ensure the button wiring will work as expected - a simple if <on/off button or play button> check on the underlying wired-to-button-function should suffice?
                    // the check-if-loaded which is called here but coded in plugins will need to always return FALSE if plugin is "play button" not "on/off button"(?)
                    // or maybe instead of always-false, it actually checks if whatever it's supposed to do has been done (thinking in terms of the Rise of Babel sound fix plugin here)
                    // update: nvm decided to just keep the checkbox; plugins can instead track their state more carefully e.g. by parsing xml to check if loaded
                    CheckBox cb = new CheckBox
                    {
                        Margin = new Thickness(7, topMargin, 0, 0),
                        Name = "pluginCheckbox" + i.ToString(),
                        Tag = i
                    };
                    cb.SetBinding(CheckBox.IsCheckedProperty, "Plugin" + i.ToString() + "Checked");
                    cb.Click += new RoutedEventHandler(PluginClick);
                    topMargin += 20;
                    PluginGrid.Children.Add(cb);

                    Button info = new Button
                    {
                        Height = 19,
                        Width = 36,
                        Margin = new Thickness(0, infoTopMargin, 0, 0),
                        Name = "infoButton" + i.ToString(),
                        Tag = i,
                        Content = "Info",
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    info.Click += new RoutedEventHandler(InfoClick);
                    Grid.SetColumn(info, 5); //thanks https://stackoverflow.com/questions/7127125/how-to-add-wpf-control-to-particular-grid-row-and-cell-during-runtime
                    infoTopMargin += 20;
                    PluginGrid.Children.Add(info);

                    PTitles.Add(plugin.PluginTitle);
                    PAuthor.Add(plugin.PluginAuthor);
                    PVersion.Add(plugin.PluginVersion);
                    PCompat.Add(plugin.CBPCompatible);
                    PDescription.Add(plugin.PluginDescription);

                    CBPLogger.GetInstance.Info(plugin.LoadResult);

                    i++;
                }
            }
            catch (Exception ex)
            {
                CBPLogger.GetInstance.Error("Error with dynamic plugin loading:\n" + ex);
                MessageBox.Show("Error with dynamic plugin loading: " + ex);//this itself will probably crash the whole launcher, but it seems slightly better than silently failing
            }
        }

        private List<IPluginCBP> ReadExtensions()
        {
            // 0 we don't have all plugins in a single directory, they're actually distributed across multiple subfolders of a known folder location
            // (e.g. we know it's D:\Example, but it could be D:\Example\Arb or D:\Example\Arbitrary or both or neither)
            List<IPluginCBP> pluginsList = new List<IPluginCBP>();

            DirectoryInfo di = new DirectoryInfo(workshopModsPath);
            DirectoryInfo[] diArr = di.GetDirectories();
            foreach (DirectoryInfo dri in diArr)
            {
                // 1) Read dll files from the specified location (a mod folder in our case)

                //todo: log each dri.FullName
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
                        MessageBox.Show("Error loading plugins: " + ex);
                    }
                }
            }
            return pluginsList;
        }

        private void CheckPluginCompatibility()
        {
            Properties.Settings.Default.PluginCompatibilityIssue = false;
            
            foreach (IPluginCBP plugin in pluginList)
            {
                if ((plugin.CheckIfLoaded() == true) && (plugin.CBPCompatible == false))
                {
                    if (plugin.CBPCompatible == false)
                        Properties.Settings.Default.PluginCompatibilityIssue = true;
                }
            }
            SaveSettings();
        }

        private void PluginClick(object sender, EventArgs e)
        {
            // based on https://stackoverflow.com/questions/1434282/how-do-i-create-5-buttons-and-assign-individual-click-events-dynamically/1434422#1434422
            if (sender is CheckBox checkbox)
            {
                switch ((int)checkbox.Tag)
                {
                    case 0:// can use resultMessage and pluginList[(int)checkbox.Tag].LoadResult for logging
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin0Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin0Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 1:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin1Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin1Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 2:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin2Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin2Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 3:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin3Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin3Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 4:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin4Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin4Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 5:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin5Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin5Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 6:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin6Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin6Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 7:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin7Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin7Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 8:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin8Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin8Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    case 9:
                        if (pluginList[(int)checkbox.Tag].CheckIfLoaded())
                        {
                            pluginList[(int)checkbox.Tag].UnloadPlugin(workshopModsPath, localModsPath);
                            Plugin9Checked = false;
                            CheckPluginCompatibility();
                        }
                        else
                        {
                            pluginList[(int)checkbox.Tag].LoadPlugin(workshopModsPath, localModsPath);
                            Plugin9Checked = true;
                            CheckPluginCompatibility();
                        }
                        CBPLogger.GetInstance.Info(pluginList[(int)checkbox.Tag].LoadResult);
                        break;
                    default:
                        Console.WriteLine("Default click event registered - no action taken.");
                        break;
                }
            }
        }

        private void InfoClick(object sender, EventArgs e)
        {
            // based on https://stackoverflow.com/questions/1434282/how-do-i-create-5-buttons-and-assign-individual-click-events-dynamically/1434422#1434422
            if (sender is Button button)
            {
                int tag = (int)button.Tag;

                // in future I'd like this to be a small window/usercontrol that has a minimal number of settings/options too
                string titleVersionAuthor = pluginList[tag].PluginTitle + " " + pluginList[tag].PluginVersion + " by " + pluginList[tag].PluginAuthor;
                string compat;
                if (pluginList[tag].CBPCompatible == true)
                    compat = "Yes";
                else
                    compat = "No";
                string multi;
                if (pluginList[tag].DefaultMultiplayerCompatible == true)
                    multi = "Yes";
                else
                    multi = "No";
                string simple;
                if (pluginList[tag].IsSimpleMod)
                    simple = "Yes";
                else
                    simple = "No";
                string compatSimple = "Compatible with CBP: " + compat + "\n"
                    + "Multiplayer compatible if not used by whole lobby: " + multi + "\n"
                    + "Simple mod loader: " + simple;
                string description = pluginList[tag].PluginDescription;

                MessageBox.Show(titleVersionAuthor
                                + "\n" + compatSimple
                                + "\n\n" + description
                                , "Plugin Info");
            }
        }

        private void Info0(int tag)
        {
            
        }

        // I tried really hard to do the bools programatically and it actually looks possible - just so difficult that it's simply not worth doing (eta >1 full day with no guaranteed result)
        // this means that there's unfortunately a hardcoded limit on how many plugins will work based on how many sets of these things are actually implemented
        private bool plugin0Checked => pluginList[0].CheckIfLoaded();
        public bool Plugin0Checked
        {
            get => plugin0Checked;
            set
            {
                bool temp = pluginList[0].CheckIfLoaded();//I don't know the full extent of *how* unnecessary this is
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin1Checked => pluginList[1].CheckIfLoaded();
        public bool Plugin1Checked
        {
            get => plugin1Checked;
            set
            {
                bool temp = pluginList[1].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin2Checked => pluginList[2].CheckIfLoaded();
        public bool Plugin2Checked
        {
            get => plugin2Checked;
            set
            {
                bool temp = pluginList[2].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin3Checked => pluginList[3].CheckIfLoaded();
        public bool Plugin3Checked
        {
            get => plugin3Checked;
            set
            {
                bool temp = pluginList[3].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin4Checked => pluginList[4].CheckIfLoaded();
        public bool Plugin4Checked
        {
            get => plugin4Checked;
            set
            {
                bool temp = pluginList[4].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin5Checked => pluginList[5].CheckIfLoaded();
        public bool Plugin5Checked
        {
            get => plugin5Checked;
            set
            {
                bool temp = pluginList[5].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin6Checked => pluginList[6].CheckIfLoaded();
        public bool Plugin6Checked
        {
            get => plugin6Checked;
            set
            {
                bool temp = pluginList[6].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin7Checked => pluginList[7].CheckIfLoaded();
        public bool Plugin7Checked
        {
            get => plugin7Checked;
            set
            {
                bool temp = pluginList[7].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin8Checked => pluginList[8].CheckIfLoaded();
        public bool Plugin8Checked
        {
            get => plugin8Checked;
            set
            {
                bool temp = pluginList[8].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private bool plugin9Checked => pluginList[9].CheckIfLoaded();
        public bool Plugin9Checked
        {
            get => plugin9Checked;
            set
            {
                bool temp = pluginList[9].CheckIfLoaded();
                temp = value;
                OnPropertyChanged();
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
