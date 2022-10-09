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
    private ObservableCollection<ExplorerTreeViewItemViewModel>? _Items;
    private List<SectionsItemContentViewModel> _sections = new()
    {
        new SectionsItemContentViewModel("GanShouTingZaiWoFaDuanDeZhiJian", "CN"),
        new SectionsItemContentViewModel("RuHeShunJianDongJieShiJian", "CN"),
        new SectionsItemContentViewModel("Untitled-3", "Unspecified"),
    };
    public ObservableCollection<ExplorerTreeViewItemViewModel>? SelectedItems { set; get; }
    private string? _strFolder;

    public string? strFolder
    {
        get => _strFolder;
        set => this.RaiseAndSetIfChanged(ref _strFolder, value);
    }

    public ObservableCollection<ExplorerTreeViewItemViewModel>? Items
    {
        get => _Items;
        set => this.RaiseAndSetIfChanged(ref _Items, value);
    }

    public List<SectionsItemContentViewModel> Sections
    {
        get => _sections;
        set => this.RaiseAndSetIfChanged(ref _sections, value);
    }

    public void OpenProjectRoot(string strFolder)
    {
        this.strFolder = strFolder;

        Items = new ObservableCollection<ExplorerTreeViewItemViewModel>
        {
            new ExplorerTreeViewItemViewModel(strFolder)
            {
                Subfolders = GetSubfolders(strFolder)
            }
        };
    }

    public ObservableCollection<ExplorerTreeViewItemViewModel> GetSubfolders(string strPath)
    {
        ObservableCollection<ExplorerTreeViewItemViewModel> subfolders = new();
        string[] subdirs = Directory.GetDirectories(strPath, "*", SearchOption.TopDirectoryOnly);
        Array.Sort(subdirs, new Comparer(CultureInfo.InstalledUICulture));

        foreach (string dir in subdirs)
        {
            ExplorerTreeViewItemViewModel thisnode = new(dir);

            if (Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly).Length > 0)
            {
                thisnode.Subfolders = new ObservableCollection<ExplorerTreeViewItemViewModel>();

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
