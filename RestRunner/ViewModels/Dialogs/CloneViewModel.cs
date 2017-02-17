using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight.CommandWpf;
using RestRunner.Models;

namespace RestRunner.ViewModels.Dialogs
{
    public class CloneViewModel : DialogViewModel
    {
        private RestCommandCategory _category;
        public RestCommandCategory Category
        {
            get { return _category; }
            set { Set(ref _category, value); }
        }

        private string _clonedName;
        public string ClonedName
        {
            get { return _clonedName; }
            set { Set(ref _clonedName, value); }
        }

        //the caption above the textbox for the cloned name
        private string _nameTitle;
        public string NameTitle
        {
            get { return _nameTitle; }
            set { Set(ref _nameTitle, value); }
        }

        #region Public Methods

        public void Cancel()
        {
            DialogResult = DialogResult.Cancel;
            CloseAction();
        }

        public void Done()
        {
            DialogResult = DialogResult.OK;
            CloseAction();
        }

        #endregion Public Methods

        #region Commands

        public RelayCommand CancelCommand => new RelayCommand(Cancel);

        public RelayCommand DoneCommand => new RelayCommand(Done);

        #endregion Commands
    }
}
