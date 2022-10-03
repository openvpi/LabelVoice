using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelVoice.ViewModels;
using ReactiveUI;

namespace LabelVoice.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            btnGetProjectRoot.Click += async (sender, e) => await GetProjectRoot();
        }

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem)((ComboBox)sender).SelectedItem;
            var lang = item.Content.ToString();
            App.SetCulture(lang);
        }

        private async Task GetProjectRoot()
        {
            var dlg = new OpenFolderDialog();

            var window = this.GetVisualRoot() as Window;
            if (window is null)
            {
                return;
            }

            var result = await dlg.ShowAsync(window);
            if (result != null)
            {
                string strFolder = result;

                ((MainWindowViewModel)DataContext!).OpenProjectRoot(strFolder);
            }
        }
    }
}
