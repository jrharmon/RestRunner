using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RestRunner.Controls
{
    public abstract class EditGridControlBase<T> : UserControlBase
    {
        #region Properties

        public abstract ObservableCollection<T> Items { get; set; }

        /// <summary>
        /// The ListView control that holds the grid of data to be edited.  It must be set
        /// in any inheriting class, so that code in this class can reference it.
        /// </summary>
        protected abstract ListView MainListView { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// This method will create a new instance of the generic type of the inherited class.
        /// </summary>
        /// <param name="readNewRecordText">If true, read any data typed into one of the entry
        /// fields in the new-record line, which happens the second a user types there.  If false,
        /// then just create a blank instance, which happens when clicking the Add button.</param>
        /// <returns></returns>
        protected abstract T CreateNewItem(bool readNewRecordText);

        /// <summary>
        /// The ListView has a DataTemplate that contains TextBoxs, which should each be explicitly named.
        /// This method takes a TextBox from the new-record row, and returns the name of the matching
        /// TextBox in the DataTemplate.
        /// </summary>
        /// <param name="textBox"></param>
        /// <returns></returns>
        protected abstract string GetMatchingTemplateTextBoxName(TextBox textBox);

        protected void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)e.Source;
            var lsvItem = FindVisualParent<ListViewItem>(btn);
            var itemIndex = MainListView.ItemContainerGenerator.IndexFromContainer(lsvItem);
            Items.RemoveAt(itemIndex);
            Console.WriteLine(e.Source.GetType());
        }

        /// <summary>
        /// Any change to a TextBox control that links to this method will trigger 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ignore any changes if the text box is empty
            var srcTextBox = (TextBox)e.Source;
            if (srcTextBox.Text.Length <= 0)
                return;

            //get the name of the TextBox control in the DataTemplate that syncs to the TextBox that was just typed into in the new-record row
            var templateTextBoxName = GetMatchingTemplateTextBoxName(srcTextBox);

            //if there is not a specified textbox, then exit
            if (templateTextBoxName == null)
            {
                srcTextBox.Text = "";
                return;
            }

            //add the new item, and make sure it's visual controls are created
            Items.Add(CreateNewItem(true));
            MainListView.UpdateLayout();

            //find the data template of the new ListViewItem
            var listViewItem = (ListViewItem)MainListView.ItemContainerGenerator.ContainerFromIndex(MainListView.Items.Count - 1);
            var contentPresenter = FindVisualChild<ContentPresenter>(listViewItem);
            DataTemplate dataTemplate = contentPresenter.ContentTemplate;


            var newControl = dataTemplate.FindName(templateTextBoxName, contentPresenter);

            //select the text box, or combo box, moving the cursor to the end, so the user can continue typing
            if (newControl is TextBox)
            {
                var newTextBox = (TextBox)newControl;
                newTextBox.Focus();
                newTextBox.CaretIndex = newTextBox.Text.Length;
            }
            else
            {
                var newComboBox = (ComboBox)newControl;
                var innerTextBox = (TextBox)newComboBox.Template.FindName("PART_EditableTextBox", newComboBox);
                innerTextBox.Focus();
                innerTextBox.CaretIndex = innerTextBox.Text.Length;
            }

            //clear out the original TextBox, so it will be ready for the next record to be started from it
            srcTextBox.Text = "";
        }

        #endregion Methods
    }
}
