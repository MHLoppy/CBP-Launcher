// sometimes comments will refer to a "reference [program]" which refers to https://github.com/tom-weiland/csharp-game-launcher

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Media;     // used for selecting brushes (used for coloring in e.g. textboxes)
using Microsoft.VisualBasic;    // used for the current (temporary?) popup user text input for manual path; I doubt it's efficient but it doesn't seem to be *too* resource intensive pending a replacement
using CBPLauncher.Logic;
using static CBPLauncher.Logic.BasicIOLogic;
using System.Windows.Input;
using static CBPLauncher.Logic.MainCode;

namespace CBPLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
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

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            var mc = (MainCode)this.DataContext;

            // possible todo: temp loading UI for better UX?

            _ = mc.InitializeAsync();
        }
    }
}
