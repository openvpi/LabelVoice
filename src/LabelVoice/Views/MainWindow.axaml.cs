using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelVoice.Core.Managers;
using LabelVoice.Core.Utils;
using LabelVoice.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVoice.Views
{
    public partial class MainWindow : Window
    {
        #region Fields

        private MainWindowViewModel _viewModel = new();

        private HexCodeGenerator _hexCodeGenerator = new();

        #endregion Fields

        #region Constructors

        public MainWindow()
        {
            DataContext = _viewModel;
            InitializeComponent();
            itemsPanel.SetWindow(this);
            slicesPanel.SetWindow(this);
            btnGetProjectRoot.Click += async (sender, e) => await GetProjectRoot();
            _viewModel.NewProject(true);
            //_viewModel.LoadProject(@"D:\测试\测试工程.lvproj");
        }

        #endregion Constructors

        #region Methods

        public void OnNewProject(object? sender, RoutedEventArgs e) => _viewModel.NewProject(true);

        public void OnOpenProject(object? sender, RoutedEventArgs e) => OpenFileAsync();

        private async void OpenFileAsync()
        {
            List<FileDialogFilter> filters = new()
            {
                new FileDialogFilter()
                {
                    Name = "LabelVoice 工程文件",
                    Extensions = new List<string>() { "lvproj" }
                }
            };
            OpenFileDialog dialog = new()
            {
                AllowMultiple = false,
                Filters = filters
            };
            if (this.GetVisualRoot() is not Window window)
            {
                return;
            }
            var result = await dialog.ShowAsync(window);
            if (result != null)
            {
                string strFolder = result.First();
                _viewModel.LoadProject(strFolder);
            }
        }

        public void OnSaveProject(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ProjectManager.Instance.ProjectFilePath))
                _viewModel.SaveProject();
            else
            {
                SaveAsAsync();
            }
        }

        public void OnSaveProjectAs(object sender, RoutedEventArgs args) => SaveAsAsync();

        private async void SaveAsAsync()
        {
            SaveFileDialog dialog = new()
            {
                DefaultExtension = "lvproj",
                Filters = new List<FileDialogFilter>()
                {
                    new FileDialogFilter()
                    {
                        Name = "LabelVoice 工程文件",
                        Extensions = new List<string>() { "lvproj" }
                    }
                },
                InitialFileName = _viewModel.ProjectFileName ?? "新工程"
            };
            if (this.GetVisualRoot() is not Window window)
            {
                return;
            }
            var result = await dialog.ShowAsync(window);
            if (result != null)
            {
                _viewModel.SaveProject(result);
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

        public void OnRemoveItem()
        {
            //TODO: Show confirm dialog before removing selected items.
            _viewModel.RemoveItems();
        }

        public void OnCreateNewSpeaker()
        {
            //if (_viewModel?.SelectedItems == null)
            //    return;
            //if (_viewModel.SelectedItems.Count == 0)
            //    return;
            //if (_viewModel.SelectedItems[0] is not ItemsTreeItemViewModel item)
            //    return;
            //if (item.ItemType == TreeItemType.Item)
            //    return;
            //var dialog = new TypeInDialog("新建文件夹")
            //{
            //    Title = $"为新文件夹命名",
            //    onFinish = name => CreateNewSpeaker(name, item),
            //};
            //dialog.ShowDialog(this);
        }

        private void CreateNewSpeaker(string name, ItemsTreeItemViewModel item)
        {
            var speakerId = _hexCodeGenerator.Generate(4);
            item.Subfolders?.Add(new ItemsTreeItemViewModel
            {
                ItemType = TreeItemType.Folder,
                Title = name,
                Speaker = speakerId
            });
        }

        public void OnCreateNewFolder()
        {
            if (_viewModel?.SelectedItems == null)
                return;
            if (_viewModel.SelectedItems.Count == 0)
                return;
            if (_viewModel.SelectedItems[0] is not ItemsTreeItemViewModel item)
                return;
            if (item.ItemType == TreeItemType.Item)
                return;
            var dialog = new TypeInDialog("新建文件夹")
            {
                Title = $"为新文件夹命名",
                onFinish = name => CreateNewFolder(name, item),
            };
            dialog.ShowDialog(this);
        }

        private void CreateNewFolder(string name, ItemsTreeItemViewModel item)
        {
            item.Subfolders?.Add(new ItemsTreeItemViewModel
            {
                ItemType = TreeItemType.Folder,
                Title = name,
                Speaker = item.Speaker//文件夹的说话人继承自上一级
            });
        }

        public void OnAddAudioFiles()
        {
            if (_viewModel?.SelectedItems == null ||_viewModel.SelectedItems.Count != 1)
                return;
            if (_viewModel.SelectedItems[0] is not ItemsTreeItemViewModel item)
                return;
            if (item.ItemType == TreeItemType.Item)
                return;
            OpenAudioFilesSelectDialogAsync(item);
        }

        private async void OpenAudioFilesSelectDialogAsync(ItemsTreeItemViewModel selectedItem)
        {
            List<FileDialogFilter> filters = new()
            {
                new FileDialogFilter()
                {
                    Name = "音频文件",
                    Extensions = new List<string>() { "wav", "flac", "aiff", "mp3" }
                }
            };
            OpenFileDialog dialog = new()
            {
                AllowMultiple = true,
                Filters = filters
            };
            if (this.GetVisualRoot() is not Window window)
            {
                return;
            }
            var result = await dialog.ShowAsync(window);
            if (result != null)
            {
                foreach (var path in result)
                {
                    selectedItem.Subfolders?.Add(new()
                    {
                        Id = _hexCodeGenerator.Generate(8),
                        Speaker = selectedItem.Speaker,
                        Title = Path.GetFileNameWithoutExtension(path),
                        Language = selectedItem.Language,
                        ItemType = TreeItemType.Item
                    });
                }
            }
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