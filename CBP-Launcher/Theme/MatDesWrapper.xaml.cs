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

namespace CBPLauncher.Theme
{
    /// <summary>
    /// Interaction logic for MatDesWrapper.xaml
    /// </summary>
    public partial class MatDesWrapper : UserControl
    {
        //// alternatively maybe could go into template XAML? not sure
        //public static readonly DependencyProperty ContentProperty =
        //    DependencyProperty.Register("Content", typeof(object), typeof(MatDesWrapper));

        //public object Content
        //{
        //    get { return GetValue(ContentProperty); }
        //    set { SetValue(ContentProperty, value); }
        //}

        public MatDesWrapper()
        {
            InitializeComponent();
        }
    }
}
