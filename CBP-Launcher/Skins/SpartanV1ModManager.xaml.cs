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

        public RelayCommand CheckboxCommand0 { get; set; }
        public RelayCommand InfoCommand0 { get; set; }

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

            CheckboxCommand0 = new RelayCommand(o =>
            {
                Plugin0();
            });
            InfoCommand0 = new RelayCommand(o =>
            {
                Info0();
            });

            // legacy: was used to generate correct number of checkboxes back when they were inside the listbox
            //PluginCount = new ObservableCollection<int>();

            //lists and executes plugins (because of the dynamic checkboxes, this can't easily be separated)
            int i = 0;
            int topMargin = 25;
            int infoTopMargin = 23;
            Console.WriteLine($"{pluginList.Count} plugin(s) found");
            foreach (IPluginCBP plugin in pluginList)
            {
                plugin.DoSomething(workshopModsPath, localModsPath);

                // each plugin generates a checkbox with a unique name
                /*CheckBox cb = new CheckBox
                {
                    Margin = new Thickness(6, topMargin, 0, 0),
                    Name = "pluginCheckbox" + i.ToString(),
                    //plugin
                    IsChecked = plugin.CheckIfLoaded(),

                    //Command = (System.Windows.Input.ICommand)SetBinding(CheckBox.CommandProperty, new Binding("LoadCommand"))
                    Command = new Binding("LoadCommand")
                };*/

                /*CheckBox cb = new CheckBox();
                cb.Margin = new Thickness(6, topMargin, 0, 0);
                cb.Name = "pluginCheckbox" + i.ToString();
                cb.IsChecked = plugin.CheckIfLoaded();
                cb.SetBinding(CheckBox.CommandProperty, new Binding("LoadCom"));//supports MVVM style but I can't seem to get it working https://stackoverflow.com/questions/3059914/wpf-binding-to-commands-in-code-behind
                cb.Command = LoadCom;//doesn't support MVVM, got at least it fucking works lol*/

                //of the three methods, this bottom one is the most recent
                CheckBox cb = new CheckBox
                {
                    Margin = new Thickness(7, topMargin, 0, 0),
                    Name = "pluginCheckbox" + i.ToString(),
                    //IsChecked = Plugin0Checked,
                    Command = CheckboxCommand0, //doesn't exactly MVVM (even though it's literally a command lol), but at least it fucking works
                    //CommandParameter = CheckBox.IsChecked
                };
                cb.SetBinding(CheckBox.IsCheckedProperty, "Plugin0Checked");//not sure the correct syntax to do this above
                topMargin += 20;
                PluginGrid.Children.Add(cb);

                Button info = new Button
                {
                    Height = 19,
                    Width = 36,
                    Margin = new Thickness(0, infoTopMargin, 0, 0),
                    Name = "infoButton" + i.ToString(),
                    Content = "Info",
                    Command = InfoCommand0,
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(info, 5); //thanks https://stackoverflow.com/questions/7127125/how-to-add-wpf-control-to-particular-grid-row-and-cell-during-runtime
                infoTopMargin += 20;
                PluginGrid.Children.Add(info);

                //PluginCount.Add(i);

                PTitles.Add(plugin.PluginTitle);
                PAuthor.Add(plugin.PluginAuthor);
                PVersion.Add(plugin.PluginVersion);
                PCompat.Add(plugin.CBPCompatible);
                PDescription.Add(plugin.PluginDescription);

                Console.WriteLine($"{plugin.PluginTitle} {plugin.PluginVersion} ({plugin.CBPCompatible}) by {plugin.PluginAuthor} | {plugin.PluginDescription}");
                Console.WriteLine("Plugin location: " + pluginsPathList[i]);
                i++;
                Console.WriteLine("\n");
            }
            Console.Write("==========================\n");

            // updates text in this app's own window
            if (pluginList != null)
                //OtherBlock.Text = $"{pluginList[0].PluginTitle} {pluginList[0].PluginVersion} by {pluginList[0].PluginAuthor} | (CBP compatible: {pluginList[0].CBPCompatible})\n{pluginList[0].PluginDescription}";

                //end
                Console.WriteLine("\nPress any key to continue...");
            //Console.ReadKey();
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

        // the bool that tracks IsChecked, the command, and the function (void) behind the command are the three things I haven't been able to create dynamically (..yet)
        // this means that there's unfortunately a hardcoded limit on how many plugins will work based on how many sets of these three things are actually implemented
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

        private void Plugin0()
        {
            if (Plugin0Checked)
            {
                pluginList[0].UnloadPlugin(workshopModsPath, localModsPath);
                Plugin0Checked = false;
            }
            else
            {
                pluginList[0].LoadPlugin(workshopModsPath, localModsPath);
                Plugin0Checked = true;
            }
        }

        private void Info0()
        {
            // in future I'd like this to be a small window/usercontrol that has a minimal number of settings/options too
            string titleVersionAuthor = pluginList[0].PluginTitle + " " + pluginList[0].PluginVersion + " by " + pluginList[0].PluginAuthor;
            string compat;
            if (pluginList[0].CBPCompatible == true)
                compat = "Yes";
            else
                compat = "No";
            string simple;
            if (pluginList[0].IsSimpleMod)
                simple = "Yes";
            else
                simple = "No";
            string compatSimple = "Compatible with CBP: " + compat + "\n" + "Simple mod loader: " + simple;
            string description = pluginList[0].PluginDescription;

            MessageBox.Show(titleVersionAuthor
                            + "\n" + compatSimple
                            + "\n\n" + description);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
