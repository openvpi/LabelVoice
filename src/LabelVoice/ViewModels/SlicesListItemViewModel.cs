using ReactiveUI;

namespace LabelVoice.ViewModels
{
    public class SlicesListItemViewModel : ViewModelBase
    {
        #region Fields

        private string? _icon;

        private string? _title;

        public string? strFullPath;

        private string? _language;

        #endregion Fields

        #region Constructors

        public SlicesListItemViewModel()
        {
            Title = "Title";
            Language = "CN";
        }

        public SlicesListItemViewModel(string title, string language)
        {
            Title = title;
            Language = language;
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

        //public bool? HasLanguageTag => !string.IsNullOrEmpty(_language);

        #endregion Properties

    }
}
