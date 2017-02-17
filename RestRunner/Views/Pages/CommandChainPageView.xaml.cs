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
using RestRunner.ViewModels.Pages;

namespace RestRunner.Views.Pages
{
    /// <summary>
    /// Interaction logic for CommandChainView.xaml
    /// </summary>
    public partial class CommandChainView : UserControl
    {
        public CommandChainView()
        {
            InitializeComponent();
        }

        private void List_OnMouseMove(object sender, MouseEventArgs e)
        {
            if ((sender is ListBoxItem) && (e.LeftButton == MouseButtonState.Pressed) && !(e.OriginalSource is Button))
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void List_OnDrop(object sender, DragEventArgs e)
        {
            var droppedData = e.Data.GetData(typeof(RestCommand)) as RestCommand;
            var target = ((ListBoxItem)(sender)).DataContext as RestCommand;

            var list = FindVisualParent<ListView>(sender as ListViewItem);
            int removedIdx = list.Items.IndexOf(droppedData);
            int targetIdx = list.Items.IndexOf(target);

            var commands = list.ItemsSource as ObservableCollection<RestCommand>;
            if (removedIdx < targetIdx)
            {
                commands.Insert(targetIdx + 1, droppedData);
                commands.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (commands.Count + 1 > remIdx)
                {
                    commands.Insert(targetIdx, droppedData);
                    commands.RemoveAt(remIdx);
                }
            }

            //re-selected the command, since the SelectedCommand was set to null when the command was first removed
            var vm = this.DataContext as CommandChainPageViewModel;
            vm.SelectedCommand = droppedData;
        }

        //versions that work on the ListView itself, instead of the ListViewItem

        //private void List_OnMouseMove(object sender, MouseEventArgs e)
        //{
        //    if ((!(sender is ListView)) || (e.LeftButton != MouseButtonState.Pressed))
        //        return;

        //    //find the ListViewItem clicked on
        //    var mousePoint = e.GetPosition(this);
        //    var clickedObject = VisualTreeHelper.HitTest(this, mousePoint).VisualHit; //the physical control clicked on (such as a TextBlock)
        //    var draggedItem = FindVisualParent<ListViewItem>(clickedObject);

        //    //start the drag
        //    DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
        //    draggedItem.IsSelected = true;
        //}

        //private void List_OnDrop(object sender, DragEventArgs e)
        //{
        //    var droppedData = e.Data.GetData(typeof(RestCommand)) as RestCommand; //the Command object that was moved

        //    //find the ListViewItem clicked on
        //    var mousePoint = e.GetPosition(this);
        //    var clickedObject = VisualTreeHelper.HitTest(this, mousePoint).VisualHit; //the physical control clicked on (such as a TextBlock)
        //    var target = FindVisualParent<ListViewItem>(clickedObject);

        //    var list = sender as ListView;
        //    int removedIdx = list.Items.IndexOf(droppedData);
        //    int targetIdx = list.Items.IndexOf(target.Content);

        //    var commands = list.ItemsSource as ObservableCollection<RestCommand>;
        //    if (removedIdx < targetIdx)
        //    {
        //        commands.Insert(targetIdx + 1, droppedData);
        //        commands.RemoveAt(removedIdx);
        //    }
        //    else
        //    {
        //        int remIdx = removedIdx + 1;
        //        if (commands.Count + 1 > remIdx)
        //        {
        //            commands.Insert(targetIdx, droppedData);
        //            commands.RemoveAt(remIdx);
        //        }
        //    }

        //    //re-selected the command, since the SelectedCommand was set to null when the command was first removed
        //    var vm = this.DataContext as CommandChainPageViewModel;
        //    vm.SelectedCommand = droppedData;
        //}

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            return parent ?? FindVisualParent<T>(parentObject);
        }
    }
}
