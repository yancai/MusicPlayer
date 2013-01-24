using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFAudioPlayer.Pages;

namespace WPFAudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        AudioPlayerPage page = new AudioPlayerPage();

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Content = page;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            page.Dispose();
            base.OnClosing(e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
