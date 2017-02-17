using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight;

namespace RestRunner.ViewModels.Dialogs
{
    public class DialogViewModel : ViewModelBase
    {
        private Action _closeAction;
        public Action CloseAction
        {
            get { return _closeAction; }
            set { Set(ref _closeAction, value); }
        }

        private DialogResult _dialogResult;
        public DialogResult DialogResult
        {
            get { return _dialogResult; }
            set { Set(ref _dialogResult, value); }
        }

        private string _doneCaption = "Done";
        public string DoneCaption
        {
            get { return _doneCaption; }
            set { Set(ref _doneCaption, value); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { Set(ref _title, value); }
        }
    }
}
