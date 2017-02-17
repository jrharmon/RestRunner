using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;

namespace RestRunner.ViewModels.Pages
{
    public abstract class PageViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _busyTimer = new DispatcherTimer();
        private DateTime _busyStart;

        protected PageViewModel()
        {
            _busyTimer.Tick += (sender, args) => BusyDuration = DateTime.Now - _busyStart;
            _busyTimer.Interval = TimeSpan.FromMilliseconds(100);
        }

        #region Properties

        private TimeSpan _busyDuration = TimeSpan.Zero;
        public TimeSpan BusyDuration
        {
            get { return _busyDuration; }
            set { Set(ref _busyDuration, value); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                Set(ref _isBusy, value);
                CommandManager.InvalidateRequerySuggested(); //force the enabled status of all commands to re-evalute (sometimes the submit button will stay disabled until the user clicks somewhere if this isn't called)

                if (value)
                    _busyStart = DateTime.Now;
                _busyTimer.IsEnabled = IsBusy;
            }
        }

        public abstract string Subtitle { get; }

        public abstract string Title { get; }

        //Allows a tab control to display a constant UserControl for a page, so that it doesn't lose its state when switching tabs
        private ContentPresenter _view = null;
        public ContentPresenter View => _view ?? (_view = new ContentPresenter() {Content = this});

        #endregion Properties

        #region Methods

        public abstract bool HasUnsavedChanges();

        #endregion Methods
    }
}
