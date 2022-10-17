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
using Colormap = Spectrogram.Colormap;
using Window = Avalonia.Controls.Window;
using FftSharp;
using YamlDotNet.Core.Tokens;
using FluentAvalonia.Core;

namespace LabelVoice.Views;

/// <summary>
/// handles wav-related plots, such as waveform and spectrogram
/// </summary>
public partial class WavPlotWindow : Window
{
    #region common default settings

    static int? defaultClipSec = null;
    public static int? DefaultClipSec
    {
        get => defaultClipSec;
        set
        {
            if (value <= 0) { return; }
            defaultClipSec = value;
        }
    }

    #endregion

    #region wav plot default settings

    public readonly static string[] defaultIntensityColorCode = { "#440154", "#39568C", "#1F968B", "#73D055" };
    public readonly static Color[] defaultIntensityColor = defaultIntensityColorCode.Select(x => ColorTranslator.FromHtml(x)).ToArray();
    
    static int defaultSamplePeakRatio = 100;
    public static int DefaultSamplePeakRatio
    {
        get => defaultSamplePeakRatio;
        set
        {
            if (value <= 0) { return; }
            defaultSamplePeakRatio = value;
        }
    }

    #endregion

    #region spec plot default settings

    public readonly static Colormap defaultColormap = Colormap.Viridis;
    public readonly static int defaultFftSize = 4096, defaultStepSize = 500;
    
    static int defaultMinFreq = 0, defaultMaxFreq = 7000; // TODO: 1. auto control maxFreq, 2. choose between linear and mel scale
    public static int DefaultMinFreq
    {
        get => defaultMinFreq;
        set
        {
            if (value < 0 || value >= defaultMaxFreq) { return; }
            defaultMinFreq = value;
        }
    }
    public static int DefaultMaxFreq
    {
        get => defaultMaxFreq;
        set
        {
            if (value <= defaultMinFreq) { return; }
            defaultMaxFreq = value;
        }
    }

    #endregion

    private WavPlotWindowViewModel _viewModel;
    public WavPlotWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new WavPlotWindowViewModel(wavPlot, specPlot);

        btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();
        btnSwitchColor.Click += async (sender, e) => await SwitchColor();
        btnSwitchMaxFreq.Click += (sender, e) => SwitchMaxFreq();

        Setup(wavPlot, specPlot);
    }
    public static void Setup(AvaPlot wavPlot, AvaPlot specPlot)
    {
        //wavPlot.Plot.Title("Waveform");
        //wavPlot.Plot.YLabel("dB");
        wavPlot.Plot.YAxis.TickLabelFormat(amp => amp == 0 ? "-∞" : String.Format("{0:0.##}dB", 20 * Math.Log10(Math.Abs(amp))));
        wavPlot.Plot.SetAxisLimitsY(yMin: -1, yMax: 1);

        //specPlot.Plot.Title("Spectrogram");
        //specPlot.Plot.YLabel("freq");
        specPlot.Plot.YAxis.TickLabelFormat(freq => String.Format("{0}Hz", freq));
        specPlot.Plot.SetAxisLimitsY(yMin: DefaultMinFreq, yMax: DefaultMaxFreq);

        AudioPlotCommonSetup(wavPlot);
        AudioPlotCommonSetup(specPlot);
    }
    private static void AudioPlotCommonSetup(AvaPlot avaPlot)
    {
        var plt = avaPlot.Plot;
        
        plt.Style(Style.Gray1);
        // plt.XLabel("sec");
        // plt.SetCulture(...); // TODO: support different culture decimals
        // plt.Grid(false);
        plt.Benchmark(enable: false);
        avaPlot.Configuration.DoubleClickBenchmark = false;

        avaPlot.Configuration.LockVerticalAxis = true;
        plt.SetAxisLimitsX(xMin: 0, xMax: 10);
     
        plt.XAxis.TickLabelFormat(timeSec => new TimeSpan(ticks: (long)(timeSec * 10000000)).ToString(@"mm\:ss\.ff"));
        // plt.XAxis.TickLabelFormat(timeSec => new TimeSpan(0, 0, (int)timeSec).ToString(@"mm\:ss"));
    }

    private async Task GetWavHeader()
    {
        var dlg = new OpenFileDialog();
        dlg.Filters!.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } }); // disallow any other formats
        dlg.AllowMultiple = false; // TODO: support multiple files (or folder) importing

        var window = this.GetVisualRoot() as Window;
        if (window is null) { return; }

        var selectedWav = await dlg.ShowAsync(window);
        if (selectedWav != null)
        {
            await Task.Run(() => _viewModel.ReadWavWhileProcess(selectedWav[0]));
            _viewModel.PlotNewWav();
            _viewModel.PlotNewSpec();
        }
    }

    private async Task SwitchColor()
    {
        if (_viewModel.WavImg != null)
        {
            #region pretending: await user choose color
            var colors = defaultIntensityColor;
            _viewModel.UserColor = colors[(colors.IndexOf(_viewModel.UserColor) + 1) % colors.Length];
            #endregion

            _viewModel.WavImg.Color = _viewModel.UserColor;
            wavPlot?.Refresh();
        }
    }

    private async Task SwitchMaxFreq()
    {
        if (_viewModel.SpecImg != null)
        {
            #region pretending: await user choose max freq
            _viewModel.NowMaxFreq = DefaultMaxFreq / ((DefaultMaxFreq / _viewModel.NowMaxFreq) % 7 + 1);
            #endregion
            
            _viewModel.ChangeLockedYAxisLimit(specPlot, min: _viewModel.NowMinFreq, max: _viewModel.NowMaxFreq);
        }
    }
}