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
    /// Interaction logic for ClassicPlus.xaml
    /// </summary>
    public partial class ClassicPlus : UserControl
    {
        public ClassicPlus()
        {
            InitializeComponent();

            /*if (Properties.Settings.Default.SkinSpV1 == false)
            {
                // select patch notes tab
                CPPN.IsChecked = true;
            }
            else
            {
                // select options tab
                CPO.IsChecked = true;
            }*/
        }
    }
}
