using System;
using System.Collections.Generic;
using System.IO;
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
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Wpf;
using Microsoft.Win32;
using System.Windows.Threading;
using NAudio.Wave;
using System.Windows.Media.Animation;

namespace DuoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VlcMediaPlayer _mediaPlayer;
        private bool isDragging = false;
        private DispatcherTimer hideControlsTimer;
        private bool isThumbDrag = false;
        private bool isFullScreen = false;
        private MediaPlayer mp3Player = new MediaPlayer();
        private TimeSpan mp3Duration;
        private List<DirectSoundDeviceInfo> audioDevices;
        private NAudio.Wave.WaveOutEvent mp3WaveOut;
        private NAudio.Wave.AudioFileReader audioFileReader;
        private bool wasDragged = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            this.MouseMove += MainWindow_MouseMove;
            hideControlsTimer = new DispatcherTimer();
            hideControlsTimer.Interval = TimeSpan.FromSeconds(3);
            hideControlsTimer.Tick += HideControlsTimer_Tick;
            timelineSlider.AddHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(timelineSlider_MouseLeftButtonDown), true);
            timelineSlider.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(timelineSlider_MouseLeftButtonUp), true);
            mp3Player.MediaOpened += Mp3Player_MediaOpened;
            audioDevices = NAudio.Wave.DirectSoundOut.Devices.ToList();
            audioDeviceComboBox.ItemsSource = audioDevices.Select(d => d.Description).ToList();
            audioDeviceComboBox.SelectedIndex = 0;
            btnPlay.IsEnabled = false;
            btnPause.IsEnabled = false;
            btnSelectFile.IsEnabled = false;
            btnToggleFullScreen.IsEnabled = false;
            timelineSlider.IsEnabled = false;
            btnSelectMP3.IsEnabled = true;
            audioDeviceComboBox.IsEnabled = true;
            Console.WriteLine(NAudio.Wave.DirectSoundOut.Devices.ToList());
        }
        

        private void Mp3Player_MediaOpened(object sender, EventArgs e)
        {
            mp3Duration = mp3Player.NaturalDuration.TimeSpan;
        }

        private void btnSelectMP3_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP3 Files|*.mp3";
            if (openFileDialog.ShowDialog() == true)
            {
                // Stop previous audio if any
                mp3Player.Stop();

                // Using NAudio to control output device
                audioFileReader?.Dispose();
                mp3WaveOut?.Dispose();

                audioFileReader = new NAudio.Wave.AudioFileReader(openFileDialog.FileName);
                mp3WaveOut = new NAudio.Wave.WaveOutEvent();
                mp3WaveOut.Init(audioFileReader);
                btnPlay.IsEnabled = false;
                btnPause.IsEnabled = false;
                btnSelectFile.IsEnabled = true;
                btnToggleFullScreen.IsEnabled = false;
                timelineSlider.IsEnabled = false;
                btnSelectMP3.IsEnabled = true;
                audioDeviceComboBox.IsEnabled = true;
            }
        }
        private void audioDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (audioFileReader != null && mp3WaveOut != null)
            {
                mp3WaveOut.Stop();
                mp3WaveOut.Dispose();

                mp3WaveOut = new NAudio.Wave.WaveOutEvent
                {
                    DeviceNumber = audioDeviceComboBox.SelectedIndex
                };
                mp3WaveOut.Init(audioFileReader);
                mp3WaveOut.Play();
            }
        }
        private void HideControlsTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer and hide the controls
            hideControlsTimer.Stop();
            HideControls();
        }
        private void timelineSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Shapes.Rectangle)
            {
                // This is a hacky way to know if the thumb is clicked.
                // The thumb internally contains a Rectangle. 
                // If the original source is a Rectangle, we can assume the thumb is clicked.
                isThumbDrag = true;
                timelineSlider.CaptureMouse();
            }
        }
        private void timelineSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isThumbDrag)
            {
                isThumbDrag = false;
                timelineSlider.ReleaseMouseCapture();
            }
        }
        private void timelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isDragging && isThumbDrag && vlcPlayer.SourceProvider.MediaPlayer != null)
            {
                vlcPlayer.SourceProvider.MediaPlayer.Position = (float)timelineSlider.Value;
            }
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            ShowControls();

            // Reset and start the timer
            hideControlsTimer.Stop();
            hideControlsTimer.Start();
        }

        private void HideControls()
        {
            btnPlay.Visibility = Visibility.Collapsed;
            btnPause.Visibility = Visibility.Collapsed;
            btnSelectFile.Visibility = Visibility.Collapsed;
            btnToggleFullScreen.Visibility = Visibility.Collapsed;
            timelineSlider.Visibility = Visibility.Collapsed;
            btnSelectMP3.Visibility = Visibility.Collapsed;
            audioDeviceComboBox.Visibility = Visibility.Collapsed;
        }

        private void ShowControls()
        {
            btnPlay.Visibility = Visibility.Visible;
            btnPause.Visibility = Visibility.Visible;
            btnSelectFile.Visibility = Visibility.Visible;
            btnToggleFullScreen.Visibility = Visibility.Visible;
            timelineSlider.Visibility = Visibility.Visible;
            btnSelectMP3.Visibility = Visibility.Visible;
            audioDeviceComboBox.Visibility = Visibility.Visible;
        }
        private void timelineSlider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            PlayBoth();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            PauseBoth();
        }


        private void timelineSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            if (vlcPlayer.SourceProvider.MediaPlayer != null)
            {
                vlcPlayer.SourceProvider.MediaPlayer.Position = (float)timelineSlider.Value;
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize the VLC player but don't play anything yet.
            try
            {
                vlcPlayer.SourceProvider.CreatePlayer(new DirectoryInfo(@"C:\Program Files\VideoLAN\VLC\"));
            }
            catch {
                MessageBox.Show("VLC player needs to be installed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            
        }

        private void MediaPlayer_PositionChanged(object sender, Vlc.DotNet.Core.VlcMediaPlayerPositionChangedEventArgs e)
        {
            
            if (wasDragged) // Only update when user is not dragging the slider
            {
                Dispatcher.Invoke(() => {
                    timelineSlider.Value = e.NewPosition;
                    if (audioFileReader != null && mp3WaveOut != null)
                    {
                        
                        var newTime = audioFileReader.TotalTime.TotalSeconds * e.NewPosition;
                        Console.WriteLine(newTime);
                        audioFileReader.CurrentTime = TimeSpan.FromSeconds(newTime);
                        wasDragged = false;
                    }
                });
            }
            if (!isDragging)
            {

            }
            else
            {
                wasDragged = true;
            }
            
        }
        private void PlayBoth()
        {
            vlcPlayer.SourceProvider.MediaPlayer.Play();
            mp3WaveOut?.Play();
        }

        private void PauseBoth()
        {
            vlcPlayer.SourceProvider.MediaPlayer.Pause();
            mp3WaveOut?.Pause();
        }

        private void StopBoth()
        {
            vlcPlayer.SourceProvider.MediaPlayer.Stop();
            mp3WaveOut?.Stop();
        }
        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP4 Files|*.mp4";
            if (openFileDialog.ShowDialog() == true)
            {
                PlayVideo(openFileDialog.FileName);
            }
        }

        private void PlayVideo(string filePath)
        {
            if (vlcPlayer.SourceProvider.MediaPlayer == null)
            {
                vlcPlayer.SourceProvider.CreatePlayer(new DirectoryInfo(@"C:\Program Files\VideoLAN\VLC\"));
                
            }
            vlcPlayer.SourceProvider.MediaPlayer.Play(new FileInfo(filePath));
            vlcPlayer.SourceProvider.MediaPlayer.PositionChanged += MediaPlayer_PositionChanged;
            vlcPlayer.SourceProvider.MediaPlayer.SetPause(true);
            btnPlay.IsEnabled = true;
            
            btnPause.IsEnabled = true;
            btnSelectFile.IsEnabled = true;
            btnToggleFullScreen.IsEnabled = true;
            timelineSlider.IsEnabled = true;
            btnSelectMP3.IsEnabled = true;
            audioDeviceComboBox.IsEnabled = true;

        }

        private void btnToggleFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
            }
        }


    }
}
