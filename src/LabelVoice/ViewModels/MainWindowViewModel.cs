using System;
using System.Collections.Generic;
using System.Text;

namespace LabelVoice.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => Locale.Strings.mainwindow_greeting;
}
