using AvaloniaEdit.Utils;
using DynamicData;
using FluentAvalonia.Core;
using LabelVoice.Core.Managers;
using LabelVoice.Core.Utils;
using LabelVoice.Models;
using LabelVoice.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static LabelVoice.ViewModels.MainWindowViewModel;

namespace LabelVoice.Utils
{
    public static class ProjectUtils
    {
        private static HexCodeGenerator _hexCodeGenerator = new();
        /// <summary>
        /// 从 ProjectModel 中读取 Items，将它们按说话人分组，并转换成树形结构。
        /// </summary>
        /// <param name="model"></param>
        /// <returns>按说话人分组的树形结构 ViewModel。</returns>
        public static ObservableCollection<ItemsTreeItemViewModel> LoadTreeItemsFrom(ProjectModel model)
        {
            ObservableCollection<ItemsTreeItemViewModel> resultItems = new();
            //根据 Speaker ID 分组工程文件里平铺的 ItemResources
            var groupedItemsResources = model.ItemResources.GroupBy(x => x.Value.Speaker)
                .Select(x => new GroupedItems
                {
                    SpeakerId = x.Key,
                    Items = x.Select(x => x.Value).ToList()
                });
            var speakers = model.Speakers;
            //将分好组的 ItemsResources 依次添加到 Items 树状图
            foreach (var groupedItemResources in groupedItemsResources)//对于每一个 Speaker 和它所包含的项目
            {
                if (groupedItemResources.Items == null)
                    continue;
                var speakerId = groupedItemResources.SpeakerId;
                var speakerName = speakers.Where(s => s.Id == speakerId).First().Name;
                //将一个说话人下的 ItemResources 转换成 ViewModel 树状结构
                var speakerNode = new ItemsTreeItemViewModel
                {
                    //Speaker 节点不需要 ItemId
                    Speaker = speakerId,
                    ItemType = TreeItemType.Folder,//Speaker 节点属于文件夹
                    Title = speakerName,//展示给用户看的是说话人的名称
                };
                ListToTreeViewModel(groupedItemResources.Items, speakerNode);
                resultItems.Add(speakerNode);
            }
            //对于可能没有项目的说话人，也要添加到树状图
            foreach (var speaker in speakers)
            {
                var containsSpeaker = resultItems.Where(m => m.Speaker == speaker.Id).Any();
                if (!containsSpeaker)
                    resultItems.Add(new()
                    {
                        Speaker = speaker.Id,
                        ItemType = TreeItemType.Folder,
                        Title = speaker.Name,
                    });
            }
            return resultItems;
        }

        /// <summary>
        /// 把列表转换成树形结构
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="speakerNode"></param>
        private static void ListToTreeViewModel(List<ItemResource> resources,ItemsTreeItemViewModel speakerNode)
        {
            //ObservableCollection<ItemsTreeItemViewModel> subfolders = new();
            foreach (var itemResource in resources)
            {
                //对于每个 ItemResource，都要把它和根节点传进去处理
                if (itemResource != null)
                    FindAndCreateFolder(itemResource, speakerNode);
            }
            //return subfolders;
        }

        private static void FindAndCreateFolder(ItemResource itemResource, ItemsTreeItemViewModel parentNode)
        {
            var virtualPath = itemResource.VirtualPath;
            var subfolders = virtualPath.Split('/');//先对路径进行分割成层
            var currentFolder = subfolders.First();//当前层级
            var currentNode = parentNode;//当前节点。从父结点开始。
                                         //不断往深层查找
            bool flag = true;
            var treeItem = new ItemsTreeItemViewModel
            {
                Id = itemResource.Id,
                Speaker = itemResource.Speaker
            };
            switch (itemResource)
            {
                case ItemDefinition definition:
                    treeItem.Id = definition.Id;
                    treeItem.ItemType = TreeItemType.Item;
                    treeItem.Title = definition.Name;
                    treeItem.Language = definition.Language;
                    //对于 ItemDefinition，如果 VirtualPath 为空，说明它在根节点，直接添加
                    if (string.IsNullOrEmpty(virtualPath))
                        parentNode.Subfolders?.Add(treeItem);
                    else//如果不是空的，说明装在文件夹内，往深层查找
                    {
                        //不断往深层查找
                        while (flag)
                        {
                            if (subfolders.Length < 1)//当前达到最后一层
                                flag = false;
                            else//继续深入
                            {
                                //如果当前节点不包含目标文件夹，先新建一个
                                if (!currentNode.ContainsFolder(currentFolder))
                                    currentNode.Subfolders?.Add(new()
                                    {
                                        ItemType = TreeItemType.Folder,
                                        Title = currentFolder,
                                        Speaker = itemResource.Speaker
                                    });
                                //在子项目中查找目标文件夹并进入
                                subfolders = subfolders.Skip(1).ToArray();
                                currentNode = currentNode.Subfolders?.Where(m =>
                                {
                                    return m.ItemType == TreeItemType.Folder
                                           && m.Title == currentFolder;
                                }).First();
                                if (subfolders.Length > 0)
                                    currentFolder = subfolders.First();
                            }
                        }
                        //已经没有子目录了，说明来到了目标层级，可以添加
                        currentNode.Subfolders?.Add(treeItem);
                    }
                    break;
                case Placeholder placeholder:
                    if (string.IsNullOrEmpty(virtualPath))
                        break;//如果 VirtualPath 为空，直接忽略
                    treeItem.ItemType = TreeItemType.Folder;//占位符作为文件夹显示
                    treeItem.Title = subfolders.Last();
                    while (flag)
                    {
                        if (subfolders.Length == 1)//只剩下一个，即文件夹本身
                            flag = false;
                        else//继续深入
                        {
                            //如果当前节点不包含目标文件夹，先新建一个
                            if (!currentNode.ContainsFolder(currentFolder))
                                currentNode.Subfolders?.Add(new()
                                {
                                    ItemType = TreeItemType.Folder,
                                    Title = currentFolder,
                                    Speaker = itemResource.Speaker
                                });
                            //在子项目中查找目标文件夹并进入
                            subfolders = subfolders.Skip(1).ToArray();
                            currentNode = currentNode.Subfolders?.Where(m =>
                            {
                                return m.ItemType == TreeItemType.Folder
                                       && m.Title == currentFolder;
                            }).First();
                            if (subfolders.Length > 1)
                                currentFolder = subfolders.First();
                        }
                    }
                    currentNode.Subfolders?.Add(treeItem);
                    break;
            }
        }

        /// <summary>
        /// 是否包含目标文件夹
        /// </summary>
        /// <param name="viewModel"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private static bool ContainsFolder(this ItemsTreeItemViewModel viewModel, string folderName)
        {
            if (viewModel.ItemType == TreeItemType.Item)//如果是项目，必定不包含子项
                return false;
            var subItems = viewModel.Subfolders;
            if (subItems == null || subItems.Count == 0)
                return false;
            foreach (var item in subItems)
            {
                if (item.Title == folderName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 把树形结构的 ViewModel 转换成列表，并写入工程 Model。
        /// </summary>
        /// <param name="model"></param>
        public static void SaveTreeItemsTo(ObservableCollection<ItemsTreeItemViewModel>? tree, ProjectModel model)
        {
            if (tree == null)
                return;
            List<string> virtualPathLayer = new();
            var resultItemResources = new Dictionary<string, ItemResource>();
            foreach (var groupedItem in tree)
            {
                resultItemResources.AddRange(GetItemResources(groupedItem.Subfolders, virtualPathLayer));
            }
            model.ItemResources = resultItemResources;
        }

        private static Dictionary<string, ItemResource> GetItemResources(ObservableCollection<ItemsTreeItemViewModel> tree, List<string> virtualPathLayer)
        {
            var resultList = new Dictionary<string, ItemResource>();
            foreach (var item in tree)
            {
                switch (item.ItemType)
                {
                    case TreeItemType.Folder://对于文件夹，递归查找内部项目并添加
                        if (item.Subfolders == null || item.Subfolders.Count == 0)//对于空文件夹，作为 placeholder 写入
                        {
                            virtualPathLayer.Add(item.Title);
                            var id = item.Id ?? _hexCodeGenerator.Generate(8);
                            resultList.Add(id, new Placeholder
                            {
                                Id = id,
                                Speaker = item.Speaker,
                                VirtualPath = string.Join('/', virtualPathLayer)
                            });
                            virtualPathLayer.Remove(virtualPathLayer.Last());
                        }
                        else//对于非空文件夹，进入内部层级
                        {
                            virtualPathLayer.Add(item.Title);
                            resultList.AddRange(GetItemResources(item.Subfolders, virtualPathLayer));
                            virtualPathLayer.Remove(virtualPathLayer.Last());
                        }
                        break;
                    case TreeItemType.Item://对于普通项目直接添加
                        resultList.Add(item.Id, new ItemDefinition
                        {
                            Id = item.Id,
                            Name = item.Title ?? "",
                            Speaker = item.Speaker ?? "",
                            Language = item.Language ?? "",
                            VirtualPath = string.Join('/', virtualPathLayer)
                        });
                        break;
                }
            }
            return resultList;
        }
    }
}
