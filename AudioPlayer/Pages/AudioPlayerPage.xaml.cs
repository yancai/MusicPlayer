using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MediaDemo;

namespace WPFAudioPlayer.Pages
{
    /// <summary>
    /// Interaction logic for AudioPlayerPage.xaml
    /// </summary>
    public partial class AudioPlayerPage : Page
    {
        public AudioPlayerPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(AudioPlayerPage_Loaded);
        }

        private static AudioPlayer player;
        private string _filePath;
        private bool isDragging = false;
        private DispatcherTimer timer;
        private TimeSpan totalTime;


        void AudioPlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            
        }


        public static readonly DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof (float), typeof (AudioPlayerPage), new PropertyMetadata(0.75f, VolumeChangedCallback));

        private static void VolumeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (player != null)
            {
                player.Volume = (float)e.NewValue;                
            }
        }

        public float Volume
        {
            get { return (float) GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty TempoProperty =
            DependencyProperty.Register("Tempo", typeof (float), typeof (AudioPlayerPage), new PropertyMetadata(1.0f, TempoChangedCallback));

        private static void TempoChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (player != null)
            {
                player.Tempo = (float)e.NewValue;                
            }
        }

        public float Tempo
        {
            get { return (float) GetValue(TempoProperty); }
            set { SetValue(TempoProperty, value); }
        }



        #region UI Handlers

        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {
            if (player != null && player.State != AudioState.Stopped)
            {
                //player.Dispose();
                player.Pause();
                Button_PlayPause.Content = "播放";
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            if (player != null)
            {
                player.Dispose();
            }
            _filePath = openFileDialog.FileName;

            try
            {
                player = new AudioPlayer(_filePath);
                TextBlock_FileName.Text = player.FileName;
                InitTime();
                timer.Start();
            }
            catch (Exception exception)
            {
                Console.WriteLine("初始化player出错！ " + exception);
            }
        }

        private void Button_PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (player == null)
            {
                return;
            }

            if (player.State == AudioState.Pausing || player.State == AudioState.Prepared || player.State == AudioState.Stopped)
            {
                if (player.State == AudioState.Stopped)
                {
                    timer.Start();
                }

                player.Play();
                Dispatcher.Invoke(new Action(() =>
                                                 {
                                                     player.Volume = Volume;
                                                     player.Tempo = Tempo;
                                                 }));
                Slider_Position.IsEnabled = true;
                Button_PlayPause.Content = "暂停";
            }
            else if (player.State == AudioState.Playing)
            {
                player.Pause();
                Button_PlayPause.Content = "播放";
            }
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (player == null)
            {
                return;
            }
            if (player.State == AudioState.Stopped)
            {
                return;
            }
            player.Stop();
            Slider_Position.Value = 0;
            timer.Stop();
            Button_PlayPause.Content = "播放";
        } 

        #endregion


        private void InitTime()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += new EventHandler(timer_Tick);

            totalTime = player.TotalTime;

            string totalTimeString = string.Format("{0:00}:{1:00}:{2:00}",
                totalTime.Hours, totalTime.Minutes, totalTime.Seconds);
            Run_TotalTime.Text = totalTimeString;
            Run_CurrentTime.Text = "00:00:00";
            Slider_Position.Maximum = totalTime.TotalSeconds;
            Slider_Position.SmallChange = 0.1;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                TimeSpan curTime = TimeSpan.FromSeconds(Slider_Position.Value);
                Slider_Position.Value = player.CurrentPlayTime.TotalSeconds;
                if (curTime == TimeSpan.FromSeconds(0) && player.State == AudioState.Stopped)
                {
                    Button_PlayPause.Content = "播放";
                    timer.Stop();
                }
            }
        }

        private void Slider_Position_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan curTime = new TimeSpan(0, 0, 0, Convert.ToInt32(Slider_Position.Value));
            string currentTimeString = string.Format("{0:00}:{1:00}:{2:00}",
                curTime.Hours, curTime.Minutes, curTime.Seconds);
            Run_CurrentTime.Text = currentTimeString;
        }

        private void Slider_Position_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isDragging = true;
        }

        private void Slider_Position_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            isDragging = false;
            player.CurrentPlayTime = TimeSpan.FromSeconds(Slider_Position.Value);
        }


        internal void Dispose()
        {
            if (player != null)
            {
                player.Dispose();                
            }
        }

    }
}
