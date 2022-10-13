using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelVoice.ViewModels;
using System.IO;
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
            btnGetProjectRoot.Click += async (sender, e) => await GetProjectRoot();
            slicesListBox.AddHandler(DragDrop.DropEvent, OnDrop);
            itemsTreeView.SelectionChanged += ItemsTreeView_SelectionChanged;
        }

        private void ItemsTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel.SelectedItems == null)
                return;
            if (_viewModel.SelectedItems?.Count == 0)
                return;
            var item = _viewModel.SelectedItems?[0] as ItemsTreeItemViewModel;
            if (item?.Subfolders?.Count > 0)
                return;
            _viewModel.ActiveItem = item;
        }

        #endregion Constructors

        #region Methods

        private void OnDrop(object? sender, DragEventArgs args)
        {
            if (!args.Data.Contains(DataFormats.FileNames))
            {
                return;
            }
            var filePaths = args.Data.GetFileNames();
            if (filePaths == null)
                return;
            foreach (var file in filePaths)
            {
                if (string.IsNullOrEmpty(file))
                    continue;
                _viewModel.Slices?.Add(
                new SlicesListItemViewModel
                {
                    Title = Path.GetFileNameWithoutExtension(file)
                });
            }
        }

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

        public void OnRenameSlice(object sender, RoutedEventArgs e)
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

        private void RenameSlice(string newName, SlicesListItemViewModel slice)
        {
            if (slice == null)
                return;
            slice.Title = newName;
        }

        public void OnRemoveSlice(object sender, RoutedEventArgs e)
        {
            //TODO:Remove selected slices from slices.
        }

        public void OnMoveSlice(object sender, RoutedEventArgs e)
        {
        }

        private void Item_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is not ItemsTreeItem item)
                return;
            if (item.Parent is not TreeViewItem parent)
                return;
            if (parent.ItemCount == 0)
                return;
            if (parent.IsExpanded)
                parent.IsExpanded = false;
            else
                parent.IsExpanded = true;
        }

        public void OnRenameItem(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItems == null)
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

        public void OnRemoveItem(object sender, RoutedEventArgs e)
        {
            //TODO: Remove selected items from items.
        }

        private static void RenameItem(string newName, ItemsTreeItemViewModel item)
        {
            if (item == null)
                return;
            item.Title = newName;
        }

        public void OnMoveItem(object sender, RoutedEventArgs e)
        {
        }

        public void OnCreateNewFolder(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItems == null)
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

        #endregion Methods
    }
}