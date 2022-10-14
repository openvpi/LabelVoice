using Avalonia.Controls;
using Avalonia.VisualTree;
using LabelVoice.ViewModels;
using System.Threading.Tasks;

namespace LabelVoice.Views
{
    public partial class MainWindow : Window
    {
        #region Fields

        private MainWindowViewModel _viewModel = new();

        #endregion Fields

        #region Constructors

        public MainWindow()
        {
            DataContext = _viewModel;
            InitializeComponent();
            itemsPanel.SetWindow(this);
            slicesPanel.SetWindow(this);
            btnGetProjectRoot.Click += async (sender, e) => await GetProjectRoot();
        }

        #endregion Constructors

        #region Methods

        private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = (ComboBoxItem)((ComboBox)sender!).SelectedItem!;
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

                _viewModel.OpenProjectRoot(strFolder);
            }
        }

        public void OnRenameItem()
        {
            if (_viewModel?.SelectedItems == null)
                return;
            if (_viewModel.SelectedItems.Count == 0)
                return;
            if (_viewModel.SelectedItems[0] is not ItemsTreeItemViewModel item)
                return;
            var dialog = new TypeInDialog(item.Title)
            {
                Title = $"Rename \"{item.Title}\"",
                onFinish = name => RenameItem(name, item),
            };
            dialog.ShowDialog(this);
        }

        private static void RenameItem(string newName, ItemsTreeItemViewModel item)
        {
            if (item == null)
                return;
            item.Title = newName;
        }

        public void OnCreateNewFolder()
        {
            if (_viewModel?.SelectedItems == null)
                return;
            if (_viewModel.SelectedItems.Count == 0)
                return;
            if (_viewModel.SelectedItems[0] is not ItemsTreeItemViewModel item)
                return;
            var dialog = new TypeInDialog("新建文件夹")
            {
                Title = $"为新文件夹命名",
                onFinish = name => CreateNewFolder(name, item),
            };
            dialog.ShowDialog(this);
        }

        private static void CreateNewFolder(string name, ItemsTreeItemViewModel item)
        {
            item.Subfolders?.Add(new ItemsTreeItemViewModel
            {
                Title = name
            });
        }

        public void OnRenameSlice()
        {
            if (_viewModel.SelectedSlices == null)
                return;
            if (_viewModel.SelectedSlices.Count == 0)
                return;
            if (_viewModel.SelectedSlices[0] is not SlicesListItemViewModel slice)
                return;
            var dialog = new TypeInDialog(slice.Title)
            {
                Title = $"Rename \"{slice.Title}\"",
                onFinish = name => RenameSlice(name, slice),
            };
            dialog.ShowDialog(this);
        }

        private static void RenameSlice(string newName, SlicesListItemViewModel slice)
        {
            if (slice == null)
                return;
            slice.Title = newName;
        }

        #endregion Methods
    }
}