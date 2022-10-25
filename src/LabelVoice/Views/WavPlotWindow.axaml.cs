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
using Avalonia.Input;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Logging;
using System.Threading;
using ScottPlot.Drawing;

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

    private CancellationTokenSource? _cancellationTokenSource;
    public WavPlotWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new WavPlotWindowViewModel(wavPlot, specPlot);

        btnSelectAndPlotWav.Click += (sender, e) => OpenWav();
        btnSwitchColor.Click += async (sender, e) => await SwitchColor();
        btnSwitchMaxFreq.Click += async (sender, e) => await SwitchMaxFreq();

        Setup(wavPlot, specPlot);
    }
    public static void Setup(AvaPlot wavPlot, AvaPlot specPlot)
    {
        //wavPlot.Plot.Title("Waveform");
        //wavPlot.Plot.YLabel("dB");
        double dBOffset = 20 * Math.Log10(WavPlotWindowViewModel.audioMax);
        wavPlot.Plot.YAxis.TickLabelFormat(amp => amp == 0 ? "-∞" : string.Format("{0:0.##}dB", 20 * Math.Log10(Math.Abs(amp)) - dBOffset));
        wavPlot.Plot.SetAxisLimitsY(yMin: -WavPlotWindowViewModel.audioMax, yMax: WavPlotWindowViewModel.audioMax);

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
        plt.Grid(false);
#if DEBUG
        plt.Benchmark(enable: true);
#else
        avaPlot.Configuration.DoubleClickBenchmark = false;
#endif
        avaPlot.Configuration.Quality = QualityMode.Low;
        
        avaPlot.Configuration.LockVerticalAxis = true; // temporary, should manually control pan and zoom
        plt.SetAxisLimitsX(xMin: 0, xMax: 10);
        avaPlot.Configuration.ScrollWheelZoom = false;

        plt.XAxis.TickLabelFormat(timeSec => new TimeSpan(ticks: (long)(timeSec * 10000000)).ToString(@"mm\:ss\.ff"));
        // plt.XAxis.TickLabelFormat(timeSec => new TimeSpan(0, 0, (int)timeSec).ToString(@"mm\:ss"));
    }

    private void MonitorZoomLimit(object sender, PointerWheelEventArgs e) 
    {
        var ap = (AvaPlot)sender;
#if DEBUG
        Console.WriteLine("e.Delta.Y: {0}, prev XSpan: {1}", e.Delta.Y, ap.Plot.GetAxisLimits().XSpan);
#endif
        if (ap.Plot.GetAxisLimits().XSpan < 5 && e.Delta.Y > 0)
            return;

        ap.Configuration.ScrollWheelZoom = true;
        ap.Plot.AxisZoom(e.Delta.Y > 0 ? 1.15 : 1 / 1.15);
        ap.Refresh();
        ap.Configuration.ScrollWheelZoom = false;
    }

    private void AxesChanged(object sender, EventArgs e)
    {
        AvaPlot changedPlot = (AvaPlot)sender;
        var newAxisLimits = changedPlot.Plot.GetAxisLimits();

        var ap = (changedPlot == wavPlot) ? specPlot : wavPlot;
        if (ap == specPlot && _viewModel.SpecImg is null)
            return;

        // disable this briefly to avoid infinite loop
        ap.Configuration.AxesChangedEventEnabled = false;
        ap.Plot.SetAxisLimitsX(newAxisLimits.XMin, newAxisLimits.XMax);
        ap.Refresh();
        ap.Configuration.AxesChangedEventEnabled = true;
    }

    private async void OpenWav()
    {
        try
        {
            var selectedWav = await GetWavHeader();
            if (selectedWav != null)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                var _renderTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(30)
                };
                _renderTimer.Tick += (object? sender, EventArgs e) => wavPlot.Refresh();
                _renderTimer.Start();

                await Task.Run(() => _viewModel.ReadWavWhileProcess(selectedWav, _cancellationTokenSource.Token));

                _renderTimer?.Stop();
                
                var fontSize = 11 * GDI.GetScaleRatio();
                wavPlot.Configuration.DpiStretch = false;
                wavPlot.Plot.XAxis.TickLabelStyle(fontSize: fontSize);
                wavPlot.Plot.YAxis.TickLabelStyle(fontSize: fontSize);
                wavPlot.Refresh();
                ClientSizeProperty.Changed.Subscribe(size =>
                {
                    wavPlot.Configuration.DpiStretch = true; // temporarily disable DpiStretch when resizing window
                });

                // _viewModel.PlotNewWav(); // wav plot is loaded while reading wav
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _viewModel.PlotNewSpec(refresh: false);
                    specPlot.Configuration.DpiStretch = false;
                    specPlot.Plot.XAxis.TickLabelStyle(fontSize: fontSize);
                    specPlot.Plot.YAxis.TickLabelStyle(fontSize: fontSize);
                    specPlot.Refresh();
                    ClientSizeProperty.Changed.Subscribe(size =>
                    {
                        specPlot.Configuration.DpiStretch = true; // temporarily disable DpiStretch when resizing window
                    });
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task<string?> GetWavHeader()
    {
        var dlg = new OpenFileDialog();
        dlg.Filters!.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } }); // disallow any other formats
        dlg.AllowMultiple = false; // TODO: support multiple files (or folder) importing

        var window = this.GetVisualRoot() as Window;
        if (window is null) { return null; }

        var selectedWavs = await dlg.ShowAsync(window);
        return selectedWavs?[0];
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
            specPlot.Refresh();
        }
    }
}