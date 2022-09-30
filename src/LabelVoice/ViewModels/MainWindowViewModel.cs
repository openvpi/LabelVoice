using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;

namespace LabelVoice.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => Application.Current!.FindResource("mainwindow.greeting")?.ToString() ?? string.Empty;
}
