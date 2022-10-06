using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LabelVoice.ViewModels;
using LabelVoice.Views;
using System.Linq;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Markup.Xaml.Styling;
using DynamicData;
using LabelVoice.Core.Managers;
using LabelVoice.Core.Audio;

namespace LabelVoice;

public class App : Application
{
#pragma warning disable CS8618
    private static Dictionary<string, ResourceInclude[]> _appLocaleResources;
#pragma warning restore CS8618
    private static ResourceInclude _defaultLocaleResource => _appLocaleResources[""][0];
    public static bool IsWin() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMac() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static string GetPlatformName()
    {
        if (IsWin()) return "Win";
        if (IsMac()) return "Mac";
        if (IsLinux()) return "Linux";
        return "";
    }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InitializeCulture();
        if (IsWin())
        {
            PlaybackManager.Instance.SetAudioBackend(AudioBackend.NAudio);
        }
        else
        {
            PlaybackManager.Instance.SetAudioBackend(AudioBackend.SDL);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //String mainWindow = "main";
            //String mainWindow = "wav";
            string mainWindow = "player";
            switch (mainWindow)
            {
                case "main":
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                    break;
                case "wav":
                    desktop.MainWindow = new WavPlotWindow();
                    break;
                case "player":
                    desktop.MainWindow = new AudioPlayerWindow
                    {
                        DataContext = new AudioPlayerWindowViewModel()
                    };
                    break;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void InitializeCulture()
    {
        // Group all cultures by languages.
        _appLocaleResources = Current!.Resources.MergedDictionaries
            .Select(res => (ResourceInclude)res)
            .Where(res => res.Source!.OriginalString
                .Contains("AppResources"))
            .GroupBy(res => Regex
                .Match(res.Source!.OriginalString, "AppResources(\\.(.*))?\\.axaml")
                .Groups[0]
                .Value[13..^5]
                .TrimEnd('.')
                .Split('-')[0]
                .ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.ToArray());
        
        //Only keep the font style of the current operating system.
        for (var i = 0;i<Current.Styles.Count;i++)
        {
            if (Current.Styles[i] is not StyleInclude) continue;
            if (((StyleInclude)Current.Styles[i]).Source.OriginalString == "/Styles/Fonts." + GetPlatformName()+".axaml")continue;
            Current.Styles.Remove(Current.Styles[i]);
            i--;
        }
        
        // Force using InvariantCulture to prevent issues caused by culture dependent string conversion, especially for floating point numbers.
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public static void SetCulture(string? culture)
    {
        // Remove all cultures from MergedDictionaries, except the default culture.
        Current!.Resources.MergedDictionaries
            .Remove(_appLocaleResources.Values
                .Aggregate(Array.Empty<ResourceInclude>().AsEnumerable(), (agg, cur) => agg.Concat(cur))
                .Except(new[] { _defaultLocaleResource }));

        var language = culture?.Split('-')[0].ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(language) || !_appLocaleResources.ContainsKey(language)) return;

        // Add back the default culture of this language.
        Current.Resources.MergedDictionaries.Add(_appLocaleResources[language][0]);
        var resDict = _appLocaleResources[language].FirstOrDefault(d => d.Source!.OriginalString.Contains(culture ?? "", StringComparison.InvariantCultureIgnoreCase));

        // Add back the specific culture if found and different from the default.
        if (resDict != null && resDict != _appLocaleResources[language][0])
        {
            Current.Resources.MergedDictionaries.Add(resDict);
        }
    }
}
