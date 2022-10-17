﻿using LabelVoice.Views;
using ReactiveUI;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;
using Spectrogram;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Setting = LabelVoice.Views.WavPlotWindow;

namespace LabelVoice.ViewModels;

public class WavPlotWindowViewModel : ViewModelBase
{
    double[] audio = new double[0];
    
    int sampleRate;

    #region wav plot fields and properties

    AvaPlot wavPlot;

    SignalPlot? wavImg;

    public SignalPlot? WavImg { get => wavImg; }

    private double? nowTotalSec;

    private Color userColor = Setting.defaultIntensityColor[0];
    public Color UserColor
    {
        get => userColor;
        set => this.RaiseAndSetIfChanged(ref userColor, value);
    }

    private bool autoStretchYAxis = true;
    public bool AutoStretchYAxis
    {
        get => autoStretchYAxis;
        set => this.RaiseAndSetIfChanged(ref autoStretchYAxis, value);
    }

    #endregion

    #region spec plot fields and properties

    SpectrogramGenerator? mySpec;

    AvaPlot specPlot;

    ScottPlot.Plottable.Image? specImg;
    public ScottPlot.Plottable.Image? SpecImg { get => specImg; }
    public Bitmap? mySpecBitmap => mySpec?.GetBitmap(intensity: 5, dB: true); // mel spec: sg!.GetBitmapMel(melBinCount: 80, intensity: 5, dB: true))

    private Colormap userColormap = Setting.defaultColormap;
    public Colormap UserColormap
    {
        get => userColormap;
        set
        {
            if (mySpec != null)
            {
                mySpec.Colormap = value;
                ReplaceSpecImg(mySpecBitmap);
                this.RaiseAndSetIfChanged(ref userColormap, value);
            }
        }
    }

    private int nowMinFreq = Setting.DefaultMinFreq, nowMaxFreq = Setting.DefaultMaxFreq;
    public int NowMinFreq
    {
        get => nowMinFreq;
        set
        {
            if (value < Setting.DefaultMinFreq || value >= nowMaxFreq) { return; }
            ChangeLockedYAxisLimit(specPlot, min: value, max: nowMaxFreq);
            this.RaiseAndSetIfChanged(ref nowMinFreq, value);
        }
    }
    public int NowMaxFreq
    {
        get => nowMaxFreq;
        set
        {
            if (value > Setting.DefaultMaxFreq || value <= nowMinFreq) { return; }
            this.RaiseAndSetIfChanged(ref nowMaxFreq, value);
        }
    }

    #endregion
    public WavPlotWindowViewModel(AvaPlot wavPlot, AvaPlot specPlot)
    {
        this.wavPlot = wavPlot;
        this.specPlot = specPlot;
    }

    /// <summary>
    /// replace the spectrogram image currently displayed, keep other things unchanged
    /// </summary>
    private void ReplaceSpecImg(Bitmap? specMap)
    {
        if (specImg == null || specMap == null || specMap.Width == 0 || specMap.Height == 0) { return; }

        double _imgWidth = (double)(specImg.WidthInAxisUnits!);
        double _imgHeight = (double)(specImg.HeightInAxisUnits!);

        var plt = specPlot.Plot;
        plt.Clear();

        specImg = plt.AddImage(specMap, x: 0, y: Setting.DefaultMaxFreq);
        specImg.WidthInAxisUnits = _imgWidth;
        specImg.HeightInAxisUnits = _imgHeight;

        specPlot.Refresh();
    }

    /// <summary>
    /// unlock y-axis, change its limit and re-lock it.
    /// the purpose of this is to disentangle y-axis zooming from x-axis zooming / panning,
    /// since direct y-axis zooming / panning is prohibited.
    /// </summary>
    public void ChangeLockedYAxisLimit(AvaPlot avaPlot, double min, double max)
    {
        if (nowTotalSec == null || min >= max) { return; } // warning: does not check against all boundaries
        var plt = avaPlot.Plot;
        plt.SetOuterViewLimits(xMin: 0, xMax: (double)nowTotalSec, yMin: min, yMax: max);
        plt.SetAxisLimitsY(min, max);
        avaPlot.Refresh();
    }

    /// <summary>
    /// plot a waveform on wavPlot using downsampled audio
    /// </summary>
    public void PlotNewWav()
    {
        double totalSec = audio.Length / sampleRate;
        if (totalSec <= 0) { return; }
        nowTotalSec = totalSec;

        double clipSec = (0 < Setting.DefaultClipSec && Setting.DefaultClipSec < totalSec) ? (double)Setting.DefaultClipSec : totalSec; // initally only display 0 ~ clipSec of spectrogram

        var plt = wavPlot.Plot;
        plt.Clear();

        // Signal plots require a data array and a sample rate (points per unit)
        wavImg = plt.AddSignal(audio, sampleRate);
        // wavImg.DensityColors = defaultIntensityColor;
        // wavImg.Color = defaultIntensityColor[0];
        wavImg.Color = userColor;

        if (autoStretchYAxis)
        {
            plt.SetOuterViewLimits(); // needed, otherwise AxisAuto won't work
            plt.AxisAutoY();
            ChangeLockedYAxisLimit(wavPlot, plt.GetAxisLimits().YMin, plt.GetAxisLimits().YMax);
        }
        else { ChangeLockedYAxisLimit(wavPlot, -1, 1); }
        plt.SetAxisLimitsX(xMin: 0, xMax: clipSec);

        wavPlot.Refresh();
    }

    /// <summary>
    /// plot a spectrogram on specPlot using bitmap generated by mySpec.
    /// need to first set nowTotalSec equal to length of the spectrogram
    /// </summary>
    public void PlotNewSpec()
    {
        Bitmap? specMap = mySpecBitmap;
        if (nowTotalSec == null || nowTotalSec <= 0 || specMap == null || specMap.Width == 0 || specMap.Height == 0) { return; }

        double clipSec = (0 < Setting.DefaultClipSec && Setting.DefaultClipSec < nowTotalSec) ? (double)Setting.DefaultClipSec : (double)nowTotalSec; // initally only display 0 ~ clipSec of spectrogram

        var plt = specPlot.Plot;
        plt.Clear();

        specImg = plt.AddImage(specMap, x: 0, y: Setting.DefaultMaxFreq);
        specImg.WidthInAxisUnits = nowTotalSec;
        specImg.HeightInAxisUnits = Setting.DefaultMaxFreq - Setting.DefaultMinFreq;

        ChangeLockedYAxisLimit(specPlot, min: nowMinFreq, max: nowMaxFreq);
        plt.SetAxisLimitsX(xMin: 0, xMax: clipSec);

        specPlot.Refresh();
    }

    /// <summary>
    /// stream-read wav file, meanwhile process it:
    /// 1. generates spectrogram, which is stored in mySpec / mySpecBitmap;
    /// 2. downsamples audio to peaks and modify sample rate;
    /// </summary>
    public void ReadWavWhileProcess(string filePath)
    {
        using var afr = new NAudio.Wave.AudioFileReader(filePath);
        int sampleRate = afr.WaveFormat.SampleRate;
        mySpec = new SpectrogramGenerator(sampleRate, Setting.defaultFftSize, Setting.defaultStepSize, minFreq: Setting.DefaultMinFreq, maxFreq: Setting.DefaultMaxFreq)
        {
            Colormap = userColormap
        };

        int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample * 8);
        int channelCount = afr.WaveFormat.Channels;

        var audio = new List<double>(((sampleCount - 1) / Setting.DefaultSamplePeakRatio + 1) * 2); // HACK: store peaks instead of samples, reducing memory

        var buffer = new float[sampleRate * channelCount];
        int samplesRead = 0;
        while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0) // stream-read wav, every time read 1 sec
        {
            mySpec.Add(buffer.Take(samplesRead).Select(x => (double)x * 16000)); // draw spectrogram
            audio.AddRange(buffer.Take(samplesRead)
                                 .Select((x, i) => new { x = (double)x, i })
                                 .GroupBy(g => g.i / Setting.DefaultSamplePeakRatio, i => i.x) // assume sampleRate % defaultSamplePeakRatio == 0
                                 .SelectMany(x => new double[] { x.Max(), x.Min() }));
        }

        this.audio = audio.ToArray();
        this.sampleRate = sampleRate * channelCount * 2 / Setting.DefaultSamplePeakRatio;
    }
}
