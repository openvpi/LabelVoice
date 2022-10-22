using ReactiveUI;
using System.Collections.ObjectModel;
using System.IO;

namespace LabelVoice.ViewModels
{
    public class ItemsTreeItemViewModel : ViewModelBase
    {
        #region Fields

        private string? _icon;

        private string? _title = "Title";

        public string? strFullPath;

        private string? _language;

        private ObservableCollection<ItemsTreeItemViewModel>? _items;

        #endregion Fields

        #region Constructors

        public ItemsTreeItemViewModel(string _strFullPath)
        {
            strFullPath = _strFullPath;
            _title = Path.GetFileName(_strFullPath);
        }

        public ItemsTreeItemViewModel()
        {
            _items = new ObservableCollection<ItemsTreeItemViewModel>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// ItemResource 的 ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// ItemResource 的 Speaker ID，不是歌手名字
        /// </summary>
        public string? Speaker { get; set; }

        /// <summary>
        /// 项目类型。普通项目或文件夹
        /// </summary>
        public TreeItemType ItemType { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon
        {
            get => _icon;
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }

        /// <summary>
        /// 项目显示的名称
        /// </summary>
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        /// <summary>
        /// 语言
        /// </summary>
        public string? Language
        {
            get => _language;
            set => this.RaiseAndSetIfChanged(ref _language, value);
        }

        public bool? HasLanguageTag => !string.IsNullOrEmpty(_language);

        /// <summary>
        /// 子项目
        /// </summary>
        public ObservableCollection<ItemsTreeItemViewModel>? Subfolders
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        #endregion Properties
    }
}