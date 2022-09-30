using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LabelVoice.ViewModels;
using ReactiveUI;

namespace LabelVoice.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem)LanguageBox?.SelectedItem;
            var lang = item?.Content.ToString();
            if (lang != null)
            {
                App.SetCulture(lang);
            }
        }
    }
}
