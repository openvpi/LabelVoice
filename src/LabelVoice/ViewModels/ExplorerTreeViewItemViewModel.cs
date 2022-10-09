using ReactiveUI;
using System.Collections.ObjectModel;
using System.IO;

namespace LabelVoice.ViewModels
{
    public class ExplorerTreeViewItemViewModel : ViewModelBase
    {
        #region Fields

        private string? _icon;

        private string? _title;

        public string? strFullPath;

        private string? _language;

        private ObservableCollection<ExplorerTreeViewItemViewModel>? _items;

        #endregion Fields

        #region Constructors

        public ExplorerTreeViewItemViewModel(string _strFullPath)
        {
            strFullPath = _strFullPath;
            _title = Path.GetFileName(_strFullPath);
        }

        public ExplorerTreeViewItemViewModel()
        {
            _items = new ObservableCollection<ExplorerTreeViewItemViewModel>();
        }

        #endregion Constructors

        #region Properties

        public string? Icon
        {
            get => _icon;
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }

        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public string? Language
        {
            get => _language;
            set => this.RaiseAndSetIfChanged(ref _language, value);
        }

        public bool? HasLanguageTag => !string.IsNullOrEmpty(_language);

        public ObservableCollection<ExplorerTreeViewItemViewModel>? Subfolders
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        #endregion Properties

    }
}