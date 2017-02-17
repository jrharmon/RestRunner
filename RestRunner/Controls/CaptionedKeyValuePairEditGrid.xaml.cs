using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RestRunner.Models;

namespace RestRunner.Controls
{
    /// <summary>
    /// Interaction logic for CaptionedKeyValuePairEditGrid.xaml
    /// </summary>
    public partial class CaptionedKeyValuePairEditGrid : EditGridControlBase<CaptionedKeyValuePair>
    {
        protected override ListView MainListView { get; }
        
        public CaptionedKeyValuePairEditGrid()
        {
            InitializeComponent();

            MainListView = lsvMain;
            grdMain.DataContext = this;
            NewItem = new CaptionedKeyValuePair("", "");
        }

        /// <summary>
        /// The list of items to be displayed in the edit grid.
        /// Any inheriting class must add a depency property for this as well
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<CaptionedKeyValuePair>), typeof(CaptionedKeyValuePairEditGrid));
        public override ObservableCollection<CaptionedKeyValuePair> Items
        {
            get { return (ObservableCollection<CaptionedKeyValuePair>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly DependencyProperty KeyWatermarkProperty = DependencyProperty.Register("KeyWatermark", typeof(string), typeof(CaptionedKeyValuePairEditGrid));
        public string KeyWatermark
        {
            get { return (string)GetValue(KeyWatermarkProperty); }
            set { SetValue(KeyWatermarkProperty, value); }
        }

        public static readonly DependencyProperty NewItemProperty = DependencyProperty.Register(nameof(NewItem), typeof(CaptionedKeyValuePair), typeof(CaptionedKeyValuePairEditGrid));
        public CaptionedKeyValuePair NewItem
        {
            get { return (CaptionedKeyValuePair)GetValue(NewItemProperty); }
            set { SetValue(NewItemProperty, value); }
        }

        public static readonly DependencyProperty ValueWatermarkProperty = DependencyProperty.Register("ValueWatermark", typeof(string), typeof(CaptionedKeyValuePairEditGrid));
        public string ValueWatermark
        {
            get { return (string)GetValue(ValueWatermarkProperty); }
            set { SetValue(ValueWatermarkProperty, value); }
        }

        protected override CaptionedKeyValuePair CreateNewItem(bool readNewRecordText)
        {
            return new CaptionedKeyValuePair(NewItem.Key, NewItem.Value);
        }

        protected override string GetMatchingTemplateTextBoxName(TextBox textBox)
        {
            return (textBox == txtNewKey) ? "txtKey" : "txtValue";
        }
    }
}
