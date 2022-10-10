using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;

namespace LabelVoice.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => Application.Current!.FindResource("mainwindow.greeting")?.ToString() ?? string.Empty;
    private ObservableCollection<ItemsTreeItemViewModel> _items = new()
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
    };
    private ItemsTreeItemViewModel _selectedItem = new()
    {
        Title = "GuangNianZhiWai",
        Language = "CN"
    };
    private List<SectionsItemContentViewModel> _sections = new()
    {
        new SectionsItemContentViewModel("GanShouTingZaiWoFaDuanDeZhiJian", "CN"),
        new SectionsItemContentViewModel("RuHeShunJianDongJieShiJian", "CN"),
        new SectionsItemContentViewModel("Untitled-3", "Unspecified"),
    };
    public ObservableCollection<ItemsTreeItemViewModel>? SelectedItems { set; get; }
    private string? _strFolder;

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

    public List<SectionsItemContentViewModel> Sections
    {
        get => _sections;
        set => this.RaiseAndSetIfChanged(ref _sections, value);
    }

    public ItemsTreeItemViewModel SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public string SelectedItemTitle
    {
        get => $"({_selectedItem.Title})";
    }

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
}
