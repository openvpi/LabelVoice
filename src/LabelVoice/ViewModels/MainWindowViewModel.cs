using Avalonia;
using Avalonia.Controls;
using LabelVoice.Core.Managers;
using LabelVoice.Models;
using LabelVoice.Utils;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace LabelVoice.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Fields

    private string _projectFileName = "新工程";

    private ObservableCollection<ItemsTreeItemViewModel>? _items;

    private ItemsTreeItemViewModel? _activeItem;

    private IList? _selectedItems;

    private IList? _selectedSlices;

    private IList? _selectedSpeakers;

    private ObservableCollection<SlicesListItemViewModel>? _slices;

    private ObservableCollection<SpeakersListItemViewModel>? _speakers;

    private string? _strFolder;

    private string? _currentProjectFolder;

    #endregion Fields

    #region Constructors

    public MainWindowViewModel(bool isLoadSampleData)
    {
        if (!isLoadSampleData)
            return;
        _items = new()
        {
            new ItemsTreeItemViewModel
            {
                Title = "Speaker 1",
                Subfolders = new ObservableCollection<ItemsTreeItemViewModel>
                {
                    new ItemsTreeItemViewModel
                    {
                        Title = "GuangNianZhiWai",
                        Language = "CN"
                    },
                    new ItemsTreeItemViewModel
                    {
                        Title = "WoHuaiNianDe",
                        Subfolders = new ObservableCollection<ItemsTreeItemViewModel>
                        {
                            new ItemsTreeItemViewModel
                            {
                                Title = "New Folder",
                                Subfolders = new ObservableCollection<ItemsTreeItemViewModel>
                                {
                                    new ItemsTreeItemViewModel
                                    {
                                        Title = "Untitled",
                                        Language = "Unspecified"
                                    }
                                }
                            }
                        }
                    },
                    new ItemsTreeItemViewModel
                    {
                        Title = "PaoMo"
                    },
                    new ItemsTreeItemViewModel
                    {
                        Title = "BuWeiXia"
                    }
                }
            },
            new ItemsTreeItemViewModel
            {
                Title = "说话人 2",
                Subfolders = new ObservableCollection<ItemsTreeItemViewModel>
                {
                    new ItemsTreeItemViewModel
                    {
                        Title = "穷极一生到不了的天堂"
                    },
                    new ItemsTreeItemViewModel
                    {
                        Title = "飞鸟与蝉"
                    }
                }
            }
        };
        _slices = new()
        {
            new SlicesListItemViewModel("GanShouTingZaiWoFaDuanDeZhiJian", "CN"),
            new SlicesListItemViewModel("RuHeShunJianDongJieShiJian", "CN"),
            new SlicesListItemViewModel("Untitled-3", "Unspecified"),
        };
        _speakers = new()
        {
            new SpeakersListItemViewModel("Speaker 01"),
            new SpeakersListItemViewModel("说话人 02")
        };
    }

    public MainWindowViewModel()
    {
    }

    #endregion Constructors

    #region Properties

    public string Greeting => Application.Current!.FindResource("mainwindow.greeting")?.ToString() ?? string.Empty;

    public string ProjectFileName
    {
        get => _projectFileName;
        set => this.RaiseAndSetIfChanged(ref _projectFileName, value);
    }

    public string? strFolder
    {
        get => _strFolder;
        set => this.RaiseAndSetIfChanged(ref _strFolder, value);
    }

    public ObservableCollection<ItemsTreeItemViewModel>? Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    public ObservableCollection<SlicesListItemViewModel>? Slices
    {
        get => _slices;
        set => this.RaiseAndSetIfChanged(ref _slices, value);
    }

    public ObservableCollection<SpeakersListItemViewModel>? Speakers
    {
        get => _speakers;
        set => this.RaiseAndSetIfChanged(ref _speakers, value);
    }

    public ItemsTreeItemViewModel? ActiveItem
    {
        get => _activeItem;
        set => this.RaiseAndSetIfChanged(ref _activeItem, value);
    }

    public IList? SelectedItems
    {
        get => _selectedItems;
        set => this.RaiseAndSetIfChanged(ref _selectedItems, value);
    }

    public IList? SelectedSlices
    {
        get => _selectedSlices;
        set => this.RaiseAndSetIfChanged(ref _selectedSlices, value);
    }
    public IList? SelectedSpeakers
    {
        get => _selectedSpeakers;
        set => this.RaiseAndSetIfChanged(ref _selectedSpeakers, value);
    }

    public string? CurrentProjectFolder
    {
        get => _currentProjectFolder;
        set => _currentProjectFolder = value;
    }

    //public bool CanNewFolder
    //{
    //    get
    //    {
    //        if (SelectedItems == null || SelectedItems.Count == 0 ||SelectedItems[0] is not ItemsTreeItemViewModel item)
    //            return false;
    //        if (item.ItemType == TreeItemType.Item)
    //            return false;
    //        return true;
    //    }
    //}

    #endregion Properties

    #region Methods

    public void OpenProjectRoot(string strFolder)
    {
        this.strFolder = strFolder;

        Items = new ObservableCollection<ItemsTreeItemViewModel>
        {
            new ItemsTreeItemViewModel(strFolder)
            {
                Subfolders = GetSubfolders(strFolder)
            }
        };
    }

    public ObservableCollection<ItemsTreeItemViewModel> GetSubfolders(string strPath)
    {
        ObservableCollection<ItemsTreeItemViewModel> subfolders = new();
        string[] subdirs = Directory.GetDirectories(strPath, "*", SearchOption.TopDirectoryOnly);
        Array.Sort(subdirs, new Comparer(CultureInfo.InstalledUICulture));

        foreach (string dir in subdirs)
        {
            ItemsTreeItemViewModel thisnode = new(dir);

            if (Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly).Length > 0)
            {
                thisnode.Subfolders = new ObservableCollection<ItemsTreeItemViewModel>();

                thisnode.Subfolders = GetSubfolders(dir);
            }

            subfolders.Add(thisnode);
        }

        return subfolders;
    }

    public class GroupedItems
    {
        public string? SpeakerId { get; set; }
        public List<ItemResource>? Items { get; set; }
    }

    public void NewProject(bool initWithData = false)
    {
        ProjectManager.Instance.NewProject(initWithData);
        ProjectFileName = "新工程";
        Items = ProjectUtils.LoadTreeItemsFrom(ProjectManager.Instance.Project);
    }

    public void LoadProject(string path)
    {
        ProjectManager.Instance.LoadProject(path);
        ProjectFileName = Path.GetFileNameWithoutExtension(path);
        Items = ProjectUtils.LoadTreeItemsFrom(ProjectManager.Instance.Project);
    }

    public void SaveProject(string? path = null)
    {
        ProjectUtils.SaveTreeItemsTo(Items, ProjectManager.Instance.Project);
        if (path != null)
            ProjectManager.Instance.SaveProject(path);
        else
            ProjectManager.Instance.SaveProject(ProjectManager.Instance.ProjectFilePath);
        ProjectFileName = Path.GetFileNameWithoutExtension(ProjectManager.Instance.ProjectFilePath);
    }

    public void UpdateActiveItem()
    {
        if (SelectedItems == null || SelectedItems?.Count == 0)
            return;
        var item = SelectedItems?[0] as ItemsTreeItemViewModel;
        if (item?.ItemType == TreeItemType.Folder)
            return;
        ActiveItem = item;
    }

    public void RemoveItems()
    {
        if (SelectedItems == null
            || SelectedItems.Count == 0
            || Items == null)
            return;
        //将已选定的项目添加到列表内
        var selectedItemsList = new List<ItemsTreeItemViewModel>();
        foreach (var selectedObject in SelectedItems)
        {
            if (selectedObject is ItemsTreeItemViewModel selectedItem)
                selectedItemsList.Add(selectedItem);
        }
        foreach (var selectedItem in selectedItemsList)
            foreach (var item in Items)
                RemoveItemFromTree(item, selectedItem);
    }

    private void RemoveItemFromTree(ItemsTreeItemViewModel tree, ItemsTreeItemViewModel item)
    {
        var subItems = tree.Subfolders;
        if (subItems == null || subItems.Count == 0)
            return;
        if (subItems.Contains(item))//当前节点的子项目包含待移除项目
            subItems.Remove(item);//直接移除
        else
        {
            //当前节点的子项目不包含待移除项目，深入到子项目的子项目移除
            foreach (var subItem in subItems)
            {
                RemoveItemFromTree(subItem, item);
            }
        }
    }

    //public class Node
    //{
    //    public ObservableCollection<Node>? Subfolders { get; set; }

    //    public string strNodeText { get; }
    //    public string strFullPath { get; }

    //    public Node(string _strFullPath)
    //    {
    //        strFullPath = _strFullPath;
    //        strNodeText = Path.GetFileName(_strFullPath);
    //    }
    //}

    #endregion Methods
}