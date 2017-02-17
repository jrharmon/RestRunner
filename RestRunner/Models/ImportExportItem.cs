using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestRunner.ViewModels;

namespace RestRunner.Models
{
    /// <summary>
    /// Used in ImportExportViemModel to store command/chain categories, environments, etc that are being imported or exported,
    /// along with any additional information that needs to be displayed in the view.
    /// </summary>
    public class ImportExportItem<T> : ObservableBase
    {
        #region Properties

        /// <summary>
        /// The number of items contained within this item (such as commands within the category)
        /// </summary>
        private int _childrenCount;
        public int ChildrenCount
        {
            get { return _childrenCount; }
            set { Set(ref _childrenCount, value); }
        }

        /// <summary>
        /// Used during imports, if true, it means that the item will be updating an existing item, and should be highlighted in the UI
        /// </summary>
        private bool _isConflicting;
        public bool IsConflicting
        {
            get { return _isConflicting; }
            set { Set(ref _isConflicting, value); }
        }
        
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        /// <summary>
        /// The actual item being imported (chain/command category, environment, etc)
        /// </summary>
        private T _item;
        public T Item
        {
            get { return _item; }
            set { Set(ref _item, value); }
        }

        /// <summary>
        /// The text to display for the item, usually copied from the item's label
        /// </summary>
        private string _label;
        public string Label
        {
            get { return _label; }
            set { Set(ref _label, value); }
        }

        #endregion Properties

        public ImportExportItem(T item, int childrenCount, bool isSelected, bool isConflicting)
        {
            _item = item;
            _childrenCount = childrenCount;
            _isSelected = isSelected;
            _isConflicting = isConflicting;
        } 
    }
}
