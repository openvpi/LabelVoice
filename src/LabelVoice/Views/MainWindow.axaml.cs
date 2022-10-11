using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelVoice.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LabelVoice.Views
{
    public partial class MainWindow : Window
    {
        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
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

                ((MainWindowViewModel)DataContext!).OpenProjectRoot(strFolder);
            }
        }

        public void OnRenameSlice(object sender, RoutedEventArgs e)
        {
            if (((MainWindowViewModel)DataContext!).SelectedSlices == null)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedSlices.Count == 0)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedSlices[0] is not SlicesListItemViewModel slice)
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
            if (((MainWindowViewModel)DataContext!).SelectedItems == null)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedItems.Count == 0)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedItems[0] is not ItemsTreeItemViewModel item)
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
            if (((MainWindowViewModel)DataContext!).SelectedItems == null)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedItems.Count == 0)
                return;
            if (((MainWindowViewModel)DataContext!).SelectedItems[0] is not ItemsTreeItemViewModel item)
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