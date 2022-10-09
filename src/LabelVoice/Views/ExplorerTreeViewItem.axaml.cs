using Avalonia.Controls;
using Avalonia.Styling;
using System;
using System.Drawing;

namespace LabelVoice.Views
{
    public partial class ExplorerTreeViewItem : UserControl, IStyleable
    {
        public ExplorerTreeViewItem()
        {
            InitializeComponent();
        }

        //public void SetStyle(Color? tickMarkColor, Color? tickFontColor)
        //{
        //    throw new System.NotImplementedException();
        //}

        //Type IStyleable.StyleKey => typeof(TreeViewItem);
    }
}
