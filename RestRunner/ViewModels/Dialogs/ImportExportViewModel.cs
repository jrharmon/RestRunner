using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight.CommandWpf;
using RestRunner.Models;

namespace RestRunner.ViewModels.Dialogs
{
    public class ImportExportViewModel : DialogViewModel
    {
        #region Properties

        private ObservableCollection<ImportExportItem<RestCommandChainCategory>> _chainCategories;
        public ObservableCollection<ImportExportItem<RestCommandChainCategory>> ChainCategories
        {
            get { return _chainCategories; }
            set { Set(ref _chainCategories, value); }
        }

        private ObservableCollection<ImportExportItem<RestCommandCategory>> _commandCategories;
        public ObservableCollection<ImportExportItem<RestCommandCategory>> CommandCategories
        {
            get { return _commandCategories; }
            set { Set(ref _commandCategories, value); }
        }

        public bool ContainsConflicts
        {
            get
            {
                return CommandCategories.Any(c => c.IsConflicting) ||
                       ChainCategories.Any(c => c.IsConflicting) ||
                       Environments.Any(e => e.IsConflicting);
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { Set(ref _description, value); }
        }

        private ObservableCollection<ImportExportItem<RestEnvironment>> _environments;
        public ObservableCollection<ImportExportItem<RestEnvironment>> Environments
        {
            get { return _environments; }
            set { Set(ref _environments, value); }
        }

        private bool _isImport;
        public bool IsImport
        {
            get { return _isImport; }
            set { Set(ref _isImport, value); }
        }

        #endregion Properties

        #region Private Methods

        private void CheckUncheckAll<T>(ObservableCollection<ImportExportItem<T>> items)
        {
            //if they are all checked, then un-check them all.  otherwise check them all
            var desiredCheckState = !items.All(i => i.IsSelected);

            foreach (var item in items.Where(i => i.IsSelected != desiredCheckState))
                item.IsSelected = desiredCheckState;
        }

        #endregion Private Methods

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

        public RelayCommand CheckUncheckAllChainsCommand => new RelayCommand(() => CheckUncheckAll(_commandCategories));

        public RelayCommand CheckUncheckAllCommandsCommand => new RelayCommand(() => CheckUncheckAll(_commandCategories));

        public RelayCommand CheckUncheckAllEnvironmentsCommand => new RelayCommand(() => CheckUncheckAll(_commandCategories));

        public RelayCommand DoneCommand => new RelayCommand(Done, () => CommandCategories.Any(c => c.IsSelected) ||
            ChainCategories.Any(c => c.IsSelected) ||
            Environments.Any(c => c.IsSelected));

        #endregion Commands
    }
}
