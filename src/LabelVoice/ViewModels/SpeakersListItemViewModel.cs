using LabelVoice.Core.Managers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVoice.ViewModels
{
    public class SpeakersListItemViewModel : ViewModelBase
    {
        #region Fields

        private string? _icon;

        private string? _title;

        #endregion Fields

        #region Constructors

        public SpeakersListItemViewModel()
        {
            Id = ProjectManager.Instance.HexCodeGenerator.Generate(4);
            Name = "Speaker";
        }

        public SpeakersListItemViewModel(string name)
        {
            Id = ProjectManager.Instance.HexCodeGenerator.Generate(4);
            Name = name;
        }

        #endregion Constructors

        #region Properties

        public string? Id { get; set; }

        public string? Icon
        {
            get => _icon;
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }

        public string? Name
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        #endregion Properties

    }
}
