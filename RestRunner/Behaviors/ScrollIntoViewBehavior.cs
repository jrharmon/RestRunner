using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RestRunner.Behaviors
{
    public sealed class ScrollIntoViewBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += ScrollIntoView;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= ScrollIntoView;
            base.OnDetaching();
        }

        private void ScrollIntoView(object o, SelectionChangedEventArgs e)
        {
            ListView lstView = (ListView)o;
            if (lstView?.SelectedItem == null) return;

            lstView.ScrollIntoView(lstView.SelectedItem);
        }
    }
}
