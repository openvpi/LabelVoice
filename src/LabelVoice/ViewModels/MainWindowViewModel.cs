using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace LabelVoice.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region Fields

    private ObservableCollection<ItemsTreeItemViewModel>? _items;

    private ItemsTreeItemViewModel? _activeItem;

    private IList? _selectedItems;

    private IList? _selectedSlices;

    private ObservableCollection<SlicesListItemViewModel>? _sections;

    private string? _strFolder;

    #endregion Fields

    #region Constructors

    public MainWindowViewModel()
    {
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
        _sections = new()
        {
            new SlicesListItemViewModel("GanShouTingZaiWoFaDuanDeZhiJian", "CN"),
            new SlicesListItemViewModel("RuHeShunJianDongJieShiJian", "CN"),
            new SlicesListItemViewModel("Untitled-3", "Unspecified"),
        };
    }

    #endregion Constructors

    #region Properties

    public string Greeting => Application.Current!.FindResource("mainwindow.greeting")?.ToString() ?? string.Empty;

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
        get => _sections;
        set => this.RaiseAndSetIfChanged(ref _sections, value);
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