using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVoice.Views
{
    public partial class WavPlotWindow: Window
    {
        private Button _btnGetWavHeader;
        public WavPlotWindow()
        {
            InitializeComponent();

            _btnGetWavHeader = this.FindControl<Button>("btnGetWavHeader");
            _btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();
        }

        private async Task GetWavHeader()
        {
            var dlg = new OpenFileDialog();
            dlg.Filters!.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } });
            dlg.Filters!.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
            dlg.AllowMultiple = false; // TODO: support multiple files (or foler) importing

            var window = this.GetVisualRoot() as Window;
            if (window is null)
            {
                return;
            }

            var result = await dlg.ShowAsync(window);
            if (result != null)
            {
                (double[] audio, int sampleRate) = ReadWav(result[0]);

                AvaPlot avaPlot1 = this.Find<AvaPlot>("avaPlot1");
               

                // Signal plots require a data array and a sample rate (points per unit)
                avaPlot1.Plot.AddSignal(audio, sampleRate);
                //avaPlot1.Plot.Title("WAV File Data");
                //avaPlot1.Plot.XLabel("Time (seconds)");
                //avaPlot1.Plot.YLabel("Audio Value");
                avaPlot1.Plot.AxisAuto(0);
                avaPlot1.Refresh();
            }
        }

        static (double[] audio, int sampleRate) ReadWav(string filePath)
        {
            using var afr = new NAudio.Wave.AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample / 8);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => (double)x));
            return (audio.ToArray(), sampleRate);
        }
    }
}