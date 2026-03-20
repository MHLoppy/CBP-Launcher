using CBPLauncher.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace CBPLauncher
{
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // normalize args
            var startupArgs = e.Args == null ? Array.Empty<string>() : (string[])e.Args.Clone();
            ArgsHolder.SetStartupArgs(startupArgs);

            // do NOT create/show MainWindow here — StartupUri in App.xaml already does it
        }
    }
}
