using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScottPlot.Control;

namespace LabelVoice.Views
{
    public partial class WavPlotWindow: Window
    {
        public WavPlotWindow()
        {
            InitializeComponent();

            btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();
        }

        private async Task GetWavHeader()
        {
            var dlg = new OpenFileDialog();
            dlg.Filters!.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } });
            dlg.Filters!.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
            dlg.AllowMultiple = false; // TODO: support multiple files (or folder) importing

            var window = this.GetVisualRoot() as Window;
            if (window is null)
            {
                return;
            }

            var result = await dlg.ShowAsync(window);
            if (result != null)
            {
                (double[] audio, int sampleRate) = ReadWav(result[0]);

                avaPlot1.Plot.Clear();
               
                // Signal plots require a data array and a sample rate (points per unit)
                avaPlot1.Plot.AddSignal(audio, sampleRate);
                //avaPlot1.Plot.Title("WAV File Data");
                //avaPlot1.Plot.XLabel("Time (seconds)");
                //avaPlot1.Plot.YLabel("Audio Value");
                
                //avaPlot1.Plot.SetCulture(...); // TODO: support different culture
                avaPlot1.Plot.AxisAuto(0);

                // disallow extra zooming / panning            
                avaPlot1.Configuration.LockVerticalAxis = true;
                var limit = avaPlot1.Plot.GetAxisLimits(0, 0);
                avaPlot1.Plot.SetOuterViewLimits(xMin: limit.XMin, xMax: limit.XMax);
                
                avaPlot1.Refresh();
            }
        }

        static (double[] audio, int sampleRate) ReadWav(string filePath)
        {
            using var afr = new NAudio.Wave.AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample * 8);
            int channelCount = afr.WaveFormat.Channels;

            int samplePeakRatio = 100;
            var audio = new List<double>(((sampleCount - 1) / samplePeakRatio + 1) * 2); // HACK: store peaks instead of samples, reducing memory

            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0) // stream-read wav, every time read 1 sec
                audio.AddRange(buffer.Take(samplesRead)
                                     .Select((x, i) => new {x = (double)x, i})
                                     .GroupBy(g => g.i / samplePeakRatio, i => i.x) // assume sampleRate % samplePeakRatio == 0
                                     .SelectMany(x => new double[] { x.Max(), x.Min() }));

            return (audio.ToArray(), sampleRate * channelCount * 2 / samplePeakRatio);
        }
    }
}