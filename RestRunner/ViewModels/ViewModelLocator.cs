/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:RestRunner"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Practices.ServiceLocation;
using RestRunner.Services;
using RestRunner.ViewModels.Pages;

namespace RestRunner.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            //register services first, since they may be needed by the view models
            ServiceManager.RegisterServices();

            //register view models
            SimpleIoc.Default.Register<CommandPageViewModel>();
            SimpleIoc.Default.Register<CommandChainPageViewModel>();

            SimpleIoc.Default.Register<MainViewModel>();
        }

        //The root pages displayed in MainView
        private static List<PageViewModel> _pageViewModels;
        public static List<PageViewModel> PageViewModels => _pageViewModels ?? (_pageViewModels = new List<PageViewModel> {Command, CommandChain});

        public static CommandPageViewModel Command => ServiceLocator.Current.GetInstance<CommandPageViewModel>();

        public static CommandChainPageViewModel CommandChain => ServiceLocator.Current.GetInstance<CommandChainPageViewModel>();

        public static MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        private static bool _isForceShutdown; //set to true when manually closing the app inside Cleanup(), so that it doesn't re-prompt the user
        public static async void Cleanup(CancelEventArgs eventArgs)
        {
            if ((!Command.HasUnsavedChanges()) && (!CommandChain.HasUnsavedChanges()) && (!Main.HasUnsavedChanges())
                || (_isForceShutdown))
                return;

            //once the await kicks in, the app will close if not canceled, so always cancel, and then manually close if the user requests it
            eventArgs.Cancel = true;

            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Save",
                NegativeButtonText = "Don't Save",
                FirstAuxiliaryButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = false
            };

            var result = await Main.ShowMessageAsync("Save Changes?",
                "There are unsaved changes.  Would you like to save them?",
                MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, mySettings);

            if (result == MessageDialogResult.Affirmative)
                await Main.SaveAsync(500);

            //unless the user clicked Cancel, you should close
            if (result != MessageDialogResult.FirstAuxiliary)
            {
                _isForceShutdown = true;
                Application.Current.Shutdown();
            }
        }
    }
}