using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelVoice.Core.Managers;
using LabelVoice.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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
            ComboBoxAudioBackends.SelectionChanged += ComboBoxAudioBackends_SelectionChanged;
            ComboBoxAudioDecoders.SelectionChanged += ComboBoxAudioDecoders_SelectionChanged;
            slider.AddHandler(Thumb.DragStartedEvent, Thumb_DragStarted);
            slider.AddHandler(Thumb.DragCompletedEvent, Thumb_DragCompleted);
            PlaybackManager.Instance.SwitchAudioBackend(Core.Audio.AudioBackend.SDL);
            UpdateComboBoxAudioDevices();
        }

        private void Thumb_DragCompleted(object? sender, Avalonia.Input.VectorEventArgs e)
        {
            PlaybackManager.Instance.SetCurrentTime(TimeSpan.FromMilliseconds(((AudioPlayerWindowViewModel)DataContext!).Progress));
            ((AudioPlayerWindowViewModel)DataContext!).StartUpdateProgress();
        }

        private void Thumb_DragStarted(object? sender, Avalonia.Input.VectorEventArgs e)
        {
            ((AudioPlayerWindowViewModel)DataContext!).StopUpdateProgress();
        }

        private void UpdateComboBoxAudioDevices()
        {
            var deviceNames = PlaybackManager.Instance.GetDevices().Select(d => d.name);
            ComboBoxAudioDevices.Items = deviceNames;
            ComboBoxAudioDevices.SelectedIndex = 0;
        }

        private void ComboBoxAudioBackends_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = ComboBoxAudioBackends.SelectedItem as ComboBoxItem;
            switch (item?.Content)
            {
                case "NAudio":
                    PlaybackManager.Instance.SwitchAudioBackend(Core.Audio.AudioBackend.NAudio);
                    break;

                case "SDL":
                    PlaybackManager.Instance.SwitchAudioBackend(Core.Audio.AudioBackend.SDL);
                    break;

                default:
                    break;
            }
            UpdateComboBoxAudioDevices();
        }

        private void ComboBoxAudioDecoders_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = ComboBoxAudioDecoders.SelectedItem as ComboBoxItem;
            switch (item?.Content)
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
            int index = ComboBoxAudioDevices.SelectedIndex;
            if (index >= 0)
                PlaybackManager.Instance.SwitchDevice(index);
            else
                PlaybackManager.Instance.SwitchDevice(0);
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
                //progressBar.Maximum = (int)PlaybackManager.Instance.GetTotalTime().TotalMilliseconds;
                slider.Maximum = (int)PlaybackManager.Instance.GetTotalTime().TotalMilliseconds;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            PlaybackManager.Instance.Dispose();
            base.OnClosing(e);
        }
    }
}