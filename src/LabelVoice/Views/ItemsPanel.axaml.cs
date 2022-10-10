using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace LabelVoice.Views
{
    public partial class ItemsPanel : UserControl
    {
        public ItemsPanel()
        {
            InitializeComponent();
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

        private void SearchBoxCopy(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem)
                return;
            textBoxSearch.Copy();
        }
    }
}