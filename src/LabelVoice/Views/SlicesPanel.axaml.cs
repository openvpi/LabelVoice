using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using LabelVoice.ViewModels;
using System.IO;

namespace LabelVoice.Views
{
    public partial class SlicesPanel : UserControl
    {
        #region Fields

        private MainWindow? _window;

        #endregion Fields

        #region Constructors

        public SlicesPanel()
        {
            InitializeComponent();
            slicesListBox.AddHandler(DragDrop.DropEvent, OnDrop);
        }

        #endregion Constructors

        #region Methods

        private void OnDrop(object? sender, DragEventArgs args)
        {
            if (!args.Data.Contains(DataFormats.FileNames))
            {
                return;
            }
            var filePaths = args.Data.GetFileNames();
            if (filePaths == null)
                return;
            foreach (var file in filePaths)
            {
                if (string.IsNullOrEmpty(file))
                    continue;
                ((MainWindowViewModel)DataContext!).Slices?.Add(
                new SlicesListItemViewModel
                {
                    Title = Path.GetFileNameWithoutExtension(file)
                });
            }
        }

        public void SetWindow(MainWindow window)
        {
            _window = window;
        }

        private void OnRenameSlice(object sender, RoutedEventArgs e)
        {
            _window?.OnRenameSlice();
        }

        #endregion Methods
    }
}