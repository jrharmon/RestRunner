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
    /// Interaction logic for RestParameterEditGrid.xaml
    /// </summary>
    public partial class RestParameterEditGrid : EditGridControlBase<RestParameter>
    {
        protected override ListView MainListView { get; }

        public RestParameterEditGrid()
        {
            InitializeComponent();

            MainListView = lsvMain;
            grdMain.DataContext = this;
            NewItem = new RestParameter("", "");
        }

        /// <summary>
        /// The list of items to be displayed in the edit grid.
        /// Any inheriting class must add a depency property for this as well
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<RestParameter>), typeof(RestParameterEditGrid));
        public override ObservableCollection<RestParameter> Items
        {
            get { return (ObservableCollection<RestParameter>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly DependencyProperty NewItemProperty = DependencyProperty.Register(nameof(NewItem), typeof(RestParameter), typeof(RestParameterEditGrid));
        public RestParameter NewItem
        {
            get { return (RestParameter)GetValue(NewItemProperty); }
            set { SetValue(NewItemProperty, value); }
        }

        protected override RestParameter CreateNewItem(bool readNewRecordText)
        {
            return new RestParameter(NewItem.Name, NewItem.Value);
        }

        protected override string GetMatchingTemplateTextBoxName(TextBox textBox)
        {
            return (textBox == txtNewName) ? "txtName" : "cmbValue";
        }
    }
}
