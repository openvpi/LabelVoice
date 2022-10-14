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
using Spectrogram;
using System.Drawing;
using System.Diagnostics.CodeAnalysis;
using LabelVoice.ViewModels;
using System.Drawing.Imaging;
using ScottPlot.Plottable;
using ScottPlot.Drawing.Colormaps;
using Window = Avalonia.Controls.Window;
using FftSharp;

namespace LabelVoice.Views;

/// <summary>
/// handles wav-related plots, such as waveform and spectrogram
/// </summary>
public partial class WavPlotWindow : Window
{
    #region wav plot default settings

    static int defaultSamplePeakRatio = 100;

    static string[] defaultIntensityColorCode = { "#440154", "#39568C", "#1F968B", "#73D055" };
    static Color[] defaultIntensityColor = defaultIntensityColorCode.Select(x => ColorTranslator.FromHtml(x)).ToArray();

    #endregion

    #region spec plot default settings

    static int defaultFftSize = 4096, defaultStepSize = 500;
    static int defaultMinFreq = 0, defaultMaxFreq = 7000; // TODO: 1. auto control maxFreq, 2. choose between linear and mel scale

    #endregion

    SpectrogramGenerator? mySpec;
    public WavPlotWindow()
    {
        InitializeComponent();

        btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();

        //wavPlot.Plot.Title("Waveform");
        //wavPlot.Plot.XLabel("sec");
        //wavPlot.Plot.YLabel("dB");
        wavPlot.Plot.YAxis.TickLabelFormat(amp => amp == 0 ? "-∞" : String.Format("{0:0.##}dB", 20 * Math.Log10(Math.Abs(amp))));
        //wavPlot.Plot.SetCulture(...); // TODO: support different culture decimals
        wavPlot.Plot.SetAxisLimits(xMin: 0, xMax: 10, yMin: -1, yMax: 1);

        //specPlot.Plot.Title("Spectrogram");
        //specPlot.Plot.XLabel("sec");
        //specPlot.Plot.YLabel("freq");
        specPlot.Plot.YAxis.TickLabelFormat(freq => String.Format("{0}Hz", freq));
        //wavPlot.Plot.SetCulture(...); // TODO: support different culture decimals
        specPlot.Plot.SetAxisLimits(xMin: 0, xMax: 10, yMin: defaultMinFreq, yMax: defaultMaxFreq);

        audioPlotCommonSetting(wavPlot);
        audioPlotCommonSetting(specPlot);
    }
    private static void audioPlotCommonSetting(AvaPlot avaPlot)
    {
        var plt = avaPlot.Plot;
        plt.Style(Style.Gray1);
        plt.Benchmark(enable: false);
        avaPlot.Configuration.LockVerticalAxis = true;
        plt.XAxis.TickLabelFormat(timeSec => new TimeSpan(ticks: (long)(timeSec * 10000000)).ToString(@"mm\:ss\.ff"));
    }

    private async Task GetWavHeader()
    {
        var dlg = new OpenFileDialog();
        dlg.Filters!.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } }); // disallow any other formats
        dlg.AllowMultiple = false; // TODO: support multiple files (or folder) importing

        var window = this.GetVisualRoot() as Window;
        if (window is null)
        {
            return;
        }

        var result = await dlg.ShowAsync(window);
        if (result != null)
        {
            (double[] audio, int sampleRate) = ReadWavWhileProcess(result[0]);

            PlotNewWav(wavPlot, audio, sampleRate);

            PlotNewSpec(specPlot, mySpec!.GetBitmap(intensity: 5, dB: true), defaultMinFreq, defaultMaxFreq, totalSec: audio.Length / sampleRate);
            // mel spec: sg!.GetBitmapMel(melBinCount: 80, intensity: 5, dB: true))
        }
    }

    /// <summary>
    /// plot a waveform on wavPlot using audio, initally display 0 ~ clipSec along x-axis
    /// </summary>
    public void PlotNewWav(AvaPlot wavPlot, double[] audio, int sampleRate, double? clipSec = null)
    {
        double totalSec = audio.Length / sampleRate;
        if (totalSec <= 0) { return; }

        clipSec = (0 < clipSec && clipSec < totalSec) ? clipSec :  totalSec; // initally only display 0 ~ clipSec of audio, default is all
        
        var plt = wavPlot.Plot;
        plt.Clear();

        // Signal plots require a data array and a sample rate (points per unit)
        var img = plt.AddSignal(audio, sampleRate);
        img.DensityColors = defaultIntensityColor;
        img.Color = defaultIntensityColor[0];

        // disallow extra zooming / panning
        plt.SetOuterViewLimits(xMin: 0, xMax: totalSec, yMin: -1, yMax: 1);
        plt.SetAxisLimits(xMin: 0, xMax: clipSec, yMin: -1, yMax: 1);

        wavPlot.Refresh();
    }
    /// <summary>
    /// plot a spectrogram on specPlot using specMap, initally display 0 ~ clipSec along x-axis
    /// </summary>
    public void PlotNewSpec(AvaPlot specPlot, Bitmap specMap, double minFreq, double maxFreq, double totalSec, double? clipSec = null)
    {
        if (totalSec <= 0 || minFreq < 0 || minFreq >= maxFreq) { return; }

        clipSec = (0 < clipSec && clipSec < totalSec) ? clipSec : totalSec; // initally only display 0 ~ clipSec of spectrogram, default is all

        var plt = specPlot.Plot;
        plt.Clear();

        var img = plt.AddImage(specMap, 0, maxFreq);
        img.WidthInAxisUnits = totalSec;
        img.HeightInAxisUnits = maxFreq - minFreq;

        // disallow extra zooming / panning
        plt.SetOuterViewLimits(xMin: 0, xMax: totalSec, yMin: minFreq, yMax: maxFreq);
        plt.SetAxisLimits(xMin: 0, xMax: clipSec, yMin: minFreq, yMax: maxFreq);

        specPlot.Refresh();
    }

    /// <summary>
    /// stream-read wav file, meanwhile process it:
    /// 1. generates spectrogram, which is stored in mySpec;
    /// 2. returns downsampled audio (audio peaks);
    /// </summary>
    private (double[] audio, int sampleRate) ReadWavWhileProcess(string filePath)
    {
        using var afr = new NAudio.Wave.AudioFileReader(filePath);
        int sampleRate = afr.WaveFormat.SampleRate;
        mySpec = new SpectrogramGenerator(sampleRate, defaultFftSize, defaultStepSize, minFreq: defaultMinFreq, maxFreq: defaultMaxFreq);

        int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample * 8);
        int channelCount = afr.WaveFormat.Channels;

        var audio = new List<double>(((sampleCount - 1) / defaultSamplePeakRatio + 1) * 2); // HACK: store peaks instead of samples, reducing memory

        var buffer = new float[sampleRate * channelCount];
        int samplesRead = 0;
        while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0) // stream-read wav, every time read 1 sec
        {
            mySpec.Add(buffer.Take(samplesRead).Select(x => (double)x * 16000)); // draw spectrogram
            audio.AddRange(buffer.Take(samplesRead)
                                 .Select((x, i) => new { x = (double)x, i })
                                 .GroupBy(g => g.i / defaultSamplePeakRatio, i => i.x) // assume sampleRate % defaultSamplePeakRatio == 0
                                 .SelectMany(x => new double[] { x.Max(), x.Min() }));
        }

        return (audio.ToArray(), sampleRate * channelCount * 2 / defaultSamplePeakRatio);
    }
}