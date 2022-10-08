using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelVoice.Core.Managers;
using LabelVoice.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVoice.Views
{
    public partial class AudioPlayerWindow : Window
    {
        private bool _isDeviceLoaded = false;

        public AudioPlayerWindow()
        {
            InitializeComponent();
            buttonOpenAudioFile.Click += async (sender, e) => await OpenFile();
            ButtonPlay.Click += Play;
            ButtonPause.Click += Pause;
            ButtonStop.Click += Stop;
            ButtonSetProgressHalf.Click += SetProgressHalf;
            ButtonPlayTestSound.Click += ButtonPlayTestSound_Click;
            ComboBoxAudioDevices.SelectionChanged += ComboBoxAudioDevices_SelectionChanged;
            ComboBoxAudioDecoders.SelectionChanged += ComboBoxAudioDecoders_SelectionChanged;
            var deviceNames = PlaybackManager.Instance.GetDevices().Select(d => d.name);
            ComboBoxAudioDevices.Items = deviceNames;
            if (!_isDeviceLoaded)
            {
                ComboBoxAudioDevices.SelectedIndex = 0;
                _isDeviceLoaded = true;
            }
        }

        private void ComboBoxAudioDecoders_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = ComboBoxAudioDecoders.SelectedItem as TextBlock;
            switch (item?.Text)
            {
                case "NAudio":
                    PlaybackManager.Instance.SwitchAudioDecoder(Core.Audio.AudioDecoder.NAudio);
                    break;

                case "FFmpeg":
                    PlaybackManager.Instance.SwitchAudioDecoder(Core.Audio.AudioDecoder.FFmpeg);
                    break;

                default:
                    break;
            }
        }

        private void ComboBoxAudioDevices_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            PlaybackManager.Instance.SwitchDevice(ComboBoxAudioDevices.SelectedIndex);
        }

        private void ButtonPlayTestSound_Click(object? sender, RoutedEventArgs e)
        {
            PlaybackManager.Instance.PlayTestSound();
        }

        private void SetProgressHalf(object? sender, RoutedEventArgs e)
        {
            var totalTime = PlaybackManager.Instance.GetTotalTime();
            PlaybackManager.Instance.SetCurrentTime(totalTime / 2);
        }

        private void Stop(object? sender, RoutedEventArgs e)
        {
            PlaybackManager.Instance.Stop();
            ((AudioPlayerWindowViewModel)DataContext!).UpdateProgress();
        }

        private void Pause(object? sender, RoutedEventArgs e)
        {
            PlaybackManager.Instance.Pause();
        }

        private void Play(object? sender, RoutedEventArgs e)
        {
            PlaybackManager.Instance.Play();
            ((AudioPlayerWindowViewModel)DataContext!).StartUpdateProgress();
        }

        private async Task OpenFile()
        {
            List<string> formats = new()
            {
                "wav",
                "mp3",
                "m4a",
                "flac"
            };
            FileDialogFilter filter = new()
            {
                Extensions = formats
            };
            List<FileDialogFilter> filters = new()
            {
                filter
            };
            OpenFileDialog dialog = new()
            {
                AllowMultiple = false,
                Filters = filters
            };
            if (this.GetVisualRoot() is not Window window)
            {
                return;
            }
            var result = await dialog.ShowAsync(window);
            if (result != null)
            {
                string strFolder = result.First();
                ((AudioPlayerWindowViewModel)DataContext!).OpenAudioFile(strFolder);
                progressBar.Maximum = (int)PlaybackManager.Instance.GetTotalTime().TotalMilliseconds;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            PlaybackManager.Instance.Dispose();
            base.OnClosing(e);
        }
    }
}