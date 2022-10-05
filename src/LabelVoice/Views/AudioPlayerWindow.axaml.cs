using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using LabelVoice.Core.Managers;
using LabelVoice.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVoice.Views
{
    public partial class AudioPlayerWindow : Window
    {
        public AudioPlayerWindow()
        {
            InitializeComponent();
            buttonOpenAudioFile.Click += async (sender, e) => await OpenFile();
            ButtonPlay.Click += Play;
            ButtonPause.Click += Pause;
            ButtonStop.Click += Stop;
            ButtonSetProgressHalf.Click += SetProgressHalf;
            ButtonPlayTestSound.Click += ButtonPlayTestSound_Click;
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
                "mp3"
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
                sliderProgress.Maximum = (int)PlaybackManager.Instance.GetTotalTime().TotalMilliseconds;
            }
        }
    }
}