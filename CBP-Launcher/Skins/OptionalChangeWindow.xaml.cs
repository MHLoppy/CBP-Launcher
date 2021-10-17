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
using System.Windows.Shapes;

namespace CBPLauncher.Skins
{
    /// <summary>
    /// Interaction logic for OptionalChangeWindow.xaml
    /// </summary>
    public partial class OptionalChangeWindow : Window
    {
        public OptionalChangeWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OptWindowExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
