using DJ.Resolver;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CBPLauncher.Skins
{
    /// <summary>
    /// Interaction logic for SpartanV1Log.xaml
    /// </summary>
    public partial class SpartanV1Log : UserControl
    {
        public SpartanV1Log()
        {
            InitializeComponent();

            if (Properties.Settings.Default.UseFancyLogging)
            {
                fancyLog.Visibility = Visibility.Visible;
                simpleLog.Visibility = Visibility.Hidden;
            }
            else
            {
                fancyLog.Visibility = Visibility.Hidden;
                simpleLog.Visibility = Visibility.Visible;
            }
        }
    }
}
