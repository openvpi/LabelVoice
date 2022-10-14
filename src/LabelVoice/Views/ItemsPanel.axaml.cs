using Avalonia.Controls;
using Avalonia.Interactivity;
using LabelVoice.ViewModels;

namespace LabelVoice.Views
{
    public partial class ItemsPanel : UserControl
    {
        #region Fields

        private MainWindow? _window;

        #endregion Fields

        #region Constructors

        public ItemsPanel()
        {
            InitializeComponent();
            itemsTreeView.SelectionChanged += ItemsTreeView_SelectionChanged;
        }

        #endregion Constructors

        #region Methods

        private void ItemsTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (((MainWindowViewModel)DataContext!).SelectedItems == null)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedItems?.Count == 0)
                return;
            var item = ((MainWindowViewModel)DataContext!).SelectedItems?[0] as ItemsTreeItemViewModel;
            if (item?.Subfolders?.Count > 0)
                return;
            ((MainWindowViewModel)DataContext!).ActiveItem = item;
        }

        public void SetWindow(MainWindow window)
        {
            _window = window;
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

        private void OnCreateNewFolder(object sender, RoutedEventArgs e)
        {
            _window?.OnCreateNewFolder();
        }

        private void OnRenameItem(object sender, RoutedEventArgs e)
        {
            _window?.OnRenameItem();
        }

        #endregion Methods
    }
}