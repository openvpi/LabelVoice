using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace LabelVoice.Widgets.Views;

public partial class DockCard : Button, IStyleable
{
    Type IStyleable.StyleKey => typeof(Button);

    // public static readonly DirectProperty<TextBlock, string> TextProperty =
    //     AvaloniaProperty.RegisterDirect<TextBlock, string>(
    //         nameof(Text),
    //         o => o.Text,
    //         (o, v) => o.Text = v);

    private string _text;

    public DockCard()
    {
        _text = string.Empty;
    }

    public string Text
    {
        get => _text;
        set => _text = value;
        // set => SetAndRaise(TextProperty, ref _text, value);
    }
}