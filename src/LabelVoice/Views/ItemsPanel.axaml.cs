using Avalonia.Controls;
using FluentAvalonia.Core;

namespace LabelVoice.Views
{
    public partial class ItemsPanel : UserControl
    {
        public ItemsPanel()
        {
            InitializeComponent();
        }

        private void Item_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not ExplorerTreeViewItem item)
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
    }
}