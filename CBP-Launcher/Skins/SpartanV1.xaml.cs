using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for SpartanV1.xaml
    /// </summary>
    public partial class SpartanV1 : UserControl
    {
        public SpartanV1()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = Application.Current.Resources["MainCodeInstance"];
            }
        }
    }
}
