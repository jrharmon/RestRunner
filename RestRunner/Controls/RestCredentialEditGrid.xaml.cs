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
    /// Interaction logic for RestCredentialEditGrid.xaml
    /// </summary>
    public partial class RestCredentialEditGrid : EditGridControlBase<RestCredential>
    {
        protected override ListView MainListView { get; }

        public RestCredentialEditGrid()
        {
            InitializeComponent();

            MainListView = lsvMain;
            grdMain.DataContext = this;
            NewItem = new RestCredential("", "", "");
        }

        /// <summary>
        /// The list of items to be displayed in the edit grid.
        /// Any inheriting class must add a depency property for this as well
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<RestCredential>), typeof(RestCredentialEditGrid));
        public override ObservableCollection<RestCredential> Items
        {
            get { return (ObservableCollection<RestCredential>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public static readonly DependencyProperty NewItemProperty = DependencyProperty.Register(nameof(NewItem), typeof(RestCredential), typeof(RestCredentialEditGrid));
        public RestCredential NewItem
        {
            get { return (RestCredential)GetValue(NewItemProperty); }
            set { SetValue(NewItemProperty, value); }
        }

        protected override RestCredential CreateNewItem(bool readNewRecordText)
        {
            return new RestCredential(NewItem.Name, NewItem.Username, NewItem.Password);
        }

        protected override string GetMatchingTemplateTextBoxName(TextBox textBox)
        {
            return (textBox == txtNewName) ? "txtName" : (textBox == txtNewUsername) ? "txtUsername" : null;
        }
    }
}
