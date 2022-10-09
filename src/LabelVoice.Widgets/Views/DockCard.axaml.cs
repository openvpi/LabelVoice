using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LabelVoice.Widgets.Views;

public partial class DockCard : Button
{
    public DockCard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}