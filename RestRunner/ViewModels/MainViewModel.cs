using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Deployment.Application;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RestRunner.Design;
using RestRunner.Models;
using RestRunner.Properties;
using RestRunner.Services;
using RestRunner.ViewModels.Dialogs;
using RestRunner.ViewModels.Pages;
using RestRunner.Views.Dialogs;
using UserControl = System.Windows.Controls.UserControl;

namespace RestRunner.ViewModels
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private static DateTime LastEnvironmentChange = DateTime.MinValue; //when starting the app, default so that any warning delays will be hit

        private readonly IEnvironmentService _environmentService;
        private readonly IUserStateService _userStateService;
        private MetroWindow _metroWindow;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IEnvironmentService environmentService, IUserStateService userStateService)
        {
            _environmentService = environmentService;
            _userStateService = userStateService;
            _selectedPageViewModel = PageViewModels.FirstOrDefault();
            Initialize();
        }

        #region Properties

        public string ApplicationVersion => ApplicationDeployment.IsNetworkDeployed ? "Version: " + ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() : "";

        public string EnvironmentButtonText => "Environment" + (SelectedEnvironment != null ? $": {SelectedEnvironment.Name}" : "");

        private ObservableCollection<RestEnvironment> _environments;
        public ObservableCollection<RestEnvironment> Environments
        {
            get { return _environments ?? (_environments = new ObservableCollection<RestEnvironment>()); }
            set { Set(ref _environments, value); }
        }

        private RestEnvironment _globalEnvironment;
        public RestEnvironment GlobalEnvironment
        {
            get { return _globalEnvironment ?? (_globalEnvironment = new RestEnvironment("[Globals]")); }
            set { Set(ref _globalEnvironment, value); }
        }

        public bool IgnoreCertificateErrors
        {
            get { return Settings.Default.IgnoreCertificateErrors; }
            set
            {
                Settings.Default.IgnoreCertificateErrors = value;
                Settings.Default.Save();
                RaisePropertyChanged(nameof(IgnoreCertificateErrors));
            }
        }

        private bool _isChainAddCommandsFlyoutOpen;
        public bool IsChainAddCommandsFlyoutOpen
        {
            get { return _isChainAddCommandsFlyoutOpen; }
            set { Set(ref _isChainAddCommandsFlyoutOpen, value); }
        }

        private bool _isChainCategoryEditFlyoutOpen;
        public bool IsChainCategoryEditFlyoutOpen
        {
            get { return _isChainCategoryEditFlyoutOpen; }
            set { Set(ref _isChainCategoryEditFlyoutOpen, value); }
        }

        private bool _isCommandCategoryEditFlyoutOpen;
        public bool IsCommandCategoryEditFlyoutOpen
        {
            get { return _isCommandCategoryEditFlyoutOpen; }
            set { Set(ref _isCommandCategoryEditFlyoutOpen, value); }
        }

        private bool _isEnvironmentFlyoutOpen;
        public bool IsEnvironmentFlyoutOpen
        {
            get { return _isEnvironmentFlyoutOpen; }
            set { Set(ref _isEnvironmentFlyoutOpen, value); }
        }

        private bool _isGlobalsFlyoutOpen;
        public bool IsGlobalsFlyoutOpen
        {
            get { return _isGlobalsFlyoutOpen; }
            set { Set(ref _isGlobalsFlyoutOpen, value); }
        }

        private bool _isSavedNotificationOpen;
        public bool IsSavedNotificationOpen
        {
            get { return _isSavedNotificationOpen; }
            set { Set(ref _isSavedNotificationOpen, value); }
        }

        private bool _isSettingsFlyoutOpen;
        public bool IsSettingsFlyoutOpen
        {
            get { return _isSettingsFlyoutOpen; }
            set { Set(ref _isSettingsFlyoutOpen, value); }
        }

        private string _notificationMessage;
        public string NotificationMessage
        {
            get { return _notificationMessage; }
            set { Set(ref _notificationMessage, value); }
        }

        public List<PageViewModel> PageViewModels => ViewModelLocator.PageViewModels;

        public bool ReplaceLocalHostWithMachine
        {
            get { return Settings.Default.ReplaceLocalHostWithMachine; }
            set
            {
                Settings.Default.ReplaceLocalHostWithMachine = value;
                Settings.Default.Save();
                RaisePropertyChanged(nameof(ReplaceLocalHostWithMachine));
            }
        }

        private RestCommandChainCategory _selectedChainCategory;
        public RestCommandChainCategory SelectedChainCategory
        {
            get { return _selectedChainCategory; }
            set { Set(ref _selectedChainCategory, value); }
        }

        private RestCommandCategory _selectedCommandCategory;
        public RestCommandCategory SelectedCommandCategory
        {
            get { return _selectedCommandCategory; }
            set { Set(ref _selectedCommandCategory, value); }
        }

        private RestEnvironment _selectedEnvironment;
        public RestEnvironment SelectedEnvironment
        {
            get { return _selectedEnvironment; }
            set
            {
                //if these are set when updating SelectedEnvironment, the CredentialName property will be wiped out when the Credentials list changes if the list doesn't contain the current value
                var curCommandCategory = SelectedCommandCategory;
                SelectedCommandCategory = null;
                var selectedCommand = ViewModelLocator.Command.SelectedCommand;
                var curCommandCredentialName = selectedCommand?.CredentialName;
                var selectedChain = ViewModelLocator.CommandChain.SelectedChain;
                var curChainCredentialName = selectedChain?.DefaultCommandCategory?.CredentialName;
                if (selectedChain != null)
                    selectedChain.DefaultCommandCategory.CredentialName = null;
                

                Set(ref _selectedEnvironment, value);
                RaisePropertyChanged(nameof(EnvironmentButtonText));
                LastEnvironmentChange = DateTime.Now;

                SelectedCommandCategory = curCommandCategory;

                if (selectedCommand != null)
                    selectedCommand.CredentialName = curCommandCredentialName;
                if (selectedChain != null)
                    selectedChain.DefaultCommandCategory.CredentialName = curChainCredentialName;

                _userStateService.UserState.SelectedEnvironmentId = value.Id;
                _userStateService.SaveUserStateAsync();
            }
        }
        
        private PageViewModel _selectedPageViewModel;
        public PageViewModel SelectedPageViewModel
        {
            get { return _selectedPageViewModel; }
            set
            {
                Set(ref _selectedPageViewModel, value);
                RaisePropertyChanged(() => SelectedPageViewModel);
            }
        }

        public int SelectedPageIndex => PageViewModels.IndexOf(SelectedPageViewModel);

        public string Title => "Rest Runner";

        #endregion Properties

        #region Private Methods

        private ImportExportViewModel SetupImportExportViewModel(bool isImport, IList<RestCommand> commands, IList<RestCommandChain> chains,
            IList<RestEnvironment> environments, RestEnvironment globals, string description)
        {
            var vm = new ImportExportViewModel();
            vm.Title = isImport ? "Import" : "Export";
            vm.DoneCaption = vm.Title;
            vm.Description = description;
            vm.IsImport = isImport;

            if (commands == null)
                commands = new List<RestCommand>();
            vm.CommandCategories = new ObservableCollection<ImportExportItem<RestCommandCategory>>();
            var commandsByCategory = commands.OrderBy(c => c.Category.Name).GroupBy(c => c.Category).ToList();
            foreach (var cat in commandsByCategory)
            {
                vm.CommandCategories.Add(new ImportExportItem<RestCommandCategory>(cat.Key, cat.Count(),
                    isSelected: isImport && cat.Any(), isConflicting: false) {Label = cat.Key.Name});
            }

            if (chains == null)
                chains = new List<RestCommandChain>();
            vm.ChainCategories = new ObservableCollection<ImportExportItem<RestCommandChainCategory>>();
            var chainsByCategory = chains.OrderBy(c => c.Category.Name).GroupBy(c => c.Category).ToList();
            foreach (var cat in chainsByCategory)
            {
                vm.ChainCategories.Add(new ImportExportItem<RestCommandChainCategory>(cat.Key, cat.Count(),
                    isSelected: isImport && cat.Any(), isConflicting: false) {Label = cat.Key.Name});
            }

            if (environments == null)
                environments = new List<RestEnvironment>();
            vm.Environments = new ObservableCollection<ImportExportItem<RestEnvironment>>();
            if (globals != null)
            {
                vm.Environments.Add(new ImportExportItem<RestEnvironment>(globals, globals.Variables.Count,
                    isSelected: isImport && globals.Variables.Any(), isConflicting: false) {Label = globals.Name});
            }
            foreach (var env in environments)
            {
                vm.Environments.Add(new ImportExportItem<RestEnvironment>(env, env.Variables.Count,
                    isSelected: isImport && env.Variables.Any(), isConflicting: false) {Label = env.Name});
            }

            //for imports, look for conflicts with existing items
            if (isImport)
            {
                var commandCategoriesWithConflicts = (from command in commands where ViewModelLocator.Command.Commands.Contains(command) select command.Category).Distinct().ToList();
                foreach (var cmdCat in vm.CommandCategories.Where(c => commandCategoriesWithConflicts.Contains(c.Item)))
                    cmdCat.IsConflicting = true;

                var chainCategoriesWithConflicts = (from chain in chains where ViewModelLocator.CommandChain.Chains.Contains(chain) select chain.Category).Distinct().ToList();
                foreach (var chainCat in vm.ChainCategories.Where(c => chainCategoriesWithConflicts.Contains(c.Item)))
                    chainCat.IsConflicting = true;

                var environmentsWithConflicts = (from env in environments where Environments.Contains(env) select env).ToList();
                foreach (var env in vm.Environments.Where(c => environmentsWithConflicts.Contains(c.Item)))
                    env.IsConflicting = true;
            }

            return vm;
        }

        #endregion Private Methods

        private async Task Initialize()
        {
            var tempEnvironments = await _environmentService.GetEnvironmentsAsync();

            //if there are no saved environments, then load the default ones to give the user something to start with
            if (tempEnvironments.Count == 0)
                tempEnvironments = await new DesignEnvironmentService().GetEnvironmentsAsync();

            if (tempEnvironments[0].Name == "[Globals]")
            {
                GlobalEnvironment = tempEnvironments[0];
                tempEnvironments = tempEnvironments.Skip(1).ToList();
            }

            foreach (var env in tempEnvironments)
                Environments.Add(env);
            var curEnvironmentId = _userStateService.UserState.SelectedEnvironmentId;
            SelectedEnvironment = Environments.FirstOrDefault(e => e.Id == curEnvironmentId) ?? Environments[0];
            LastEnvironmentChange = DateTime.MinValue; //when starting the app, default so that any warning delays will be hit
        }

        #region Public Methods

        public void AddEnvironment(RestEnvironment origCommand)
        {
            var newEnvironment = new RestEnvironment("New Env");
            Environments.Add(newEnvironment);
            SelectedEnvironment = newEnvironment;
        }

        public void CloneEnvironment(RestEnvironment origEnvironment)
        {
            var newEnvironment = origEnvironment.DeepCopy();
            newEnvironment.Name += " - Copy";
            Environments.Add(newEnvironment);
            SelectedEnvironment = newEnvironment;
        }

        public void EditChainCategory(RestCommandChainCategory category)
        {
            SelectedChainCategory = category;
            IsChainCategoryEditFlyoutOpen = true;
        }

        public void EditCommandCategory(RestCommandCategory category)
        {
            SelectedCommandCategory = category;
            IsCommandCategoryEditFlyoutOpen = true;
        }

        public async Task ExportAsync()
        {
            IsSettingsFlyoutOpen = false;

            //setup the dialog vm
            var vm = SetupImportExportViewModel(false, ViewModelLocator.Command.Commands, ViewModelLocator.CommandChain.Chains, Environments, GlobalEnvironment, "");

            //display the dialog
            var dlgResult = await ViewModelLocator.Main.ShowCustomDialog<ImportExportDialog>(vm);
            if (dlgResult != DialogResult.OK)
                return;

            //setup the items to export
            string defaultFileName = null;
            var itemCount = 0; //total number of items saved, of all types
            var selectedCommandCategories = vm.CommandCategories.Where(c => c.IsSelected).Select(c => c.Item).ToList();
            var commandsByCategory = ViewModelLocator.Command.Commands.GroupBy(c => c.Category).ToList();
            var commands = commandsByCategory.Where(c => selectedCommandCategories.Any(cat => cat.Equals(c.Key))).SelectMany(c => c).ToList();
            itemCount += selectedCommandCategories.Count;
            if (selectedCommandCategories.Count == 1)
                defaultFileName = selectedCommandCategories[0].Name;

            var selectedChainCategories = vm.ChainCategories.Where(c => c.IsSelected).Select(c => c.Item).ToList();
            var chainsByCategory = ViewModelLocator.CommandChain.Chains.GroupBy(c => c.Category).ToList();
            var chains = chainsByCategory.Where(c => selectedChainCategories.Any(cat => cat.Equals(c.Key))).SelectMany(c => c).ToList();
            itemCount += selectedChainCategories.Count;
            if (selectedChainCategories.Count == 1)
                defaultFileName = selectedChainCategories.First().Name;

            var environments = vm.Environments.Skip(1).Where(e => e.IsSelected).Select(e => e.Item).ToList(); //skip the global environment, which is always the first
            itemCount += environments.Count;
            if (environments.Count == 1)
                defaultFileName = environments.First().Name;

            var globals = vm.Environments.Take(1).FirstOrDefault(e => e.IsSelected)?.Item;
            itemCount += globals != null ? 1 : 0;
            if (globals != null)
                defaultFileName = "Globals";

            //only use defaultFileName if a single item was selected
            if (itemCount > 1)
                defaultFileName = null;

            await Task.Delay(300); //give the export dialog a moment to fade away before pulling up the save file dialog

            //ask where to save the file to
            //var outPath = @"c:\users\jharmon\documents\RestRunnerExportTest.json";
            defaultFileName = (defaultFileName ?? "Export") + ".rest_runner";
            var exportFilePath = ShowSaveFileDialog("Export Rest Runner File", defaultFileName);
            if (exportFilePath == null)
                return;

            //export the file
            var exporter = new ImportExportService();
            await exporter.ExportToFile(exportFilePath, commands, chains, environments, globals, vm.Description);
            await ShowNotificationMessage("Exported to: " + exportFilePath, 3000);
        }

        public bool HasUnsavedChanges()
        {
            var environments = new List<RestEnvironment>() {GlobalEnvironment};
            environments = environments.Concat(Environments).ToList();
            return _environmentService.HasChanged(environments);
        }

        public async Task ImportAsync(bool usePostmanFormat = false)
        {
            IsSettingsFlyoutOpen = false;

            //get the file to import
            //var importFilePath = @"c:\users\jharmon\documents\Backup.postman_dump"; //temp
            //var importFilePath = @"c:\users\jharmon\documents\Credit%20Manager%20Service.postman_collection.json"; //temp
            var openDialogTitle = usePostmanFormat ? "Import Postman File" : "Import Rest Runner File";
            var importFilePath = ShowOpenFileDialog(openDialogTitle);
            if (importFilePath == null)
                return;

            //read and parse the import file
            var importer = new ImportExportService();
            ImportedData importedData;
            if (usePostmanFormat)
                importedData = await importer.ImportPostmanFile(importFilePath);
            else
                importedData = await importer.ImportFile(importFilePath);

            if (importedData.Errors.Count > 0)
            {
                await ShowMessageAsync("Import Error", importedData.Errors[0]);
                return;
            }

            //setup the dialog vm
            var vm = SetupImportExportViewModel(true, importedData.Commands, importedData.Chains, importedData.Environments, importedData.Globals, importedData.Description);
            
            //display the dialog
            var dlgResult = await ViewModelLocator.Main.ShowCustomDialog<ImportExportDialog>(vm);
            if (dlgResult != DialogResult.OK)
                return;

            //load commands
            if (importedData.Commands != null)
            {
                foreach (var command in importedData.Commands)
                    ViewModelLocator.Command.Commands.Add(command);
                ViewModelLocator.Command.SelectedCommand = ViewModelLocator.Command.Commands.FirstOrDefault();
            }

            //load environments with actual variables.  if there is already an environment with the same name, just add ot it
            if (importedData.Environments != null)
            {
                foreach (var environment in importedData.Environments.Where(e => e.Variables.Count > 0))
                {
                    var existingEnv = Environments.FirstOrDefault(e => e.Name.Equals(environment.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (existingEnv != null)
                        existingEnv.Update(environment);
                    else
                        Environments.Add(environment);
                }
            }

            //load globals
            if (importedData.Globals != null)
                GlobalEnvironment.Update(importedData.Globals);

            await ShowNotificationMessage($"Import Complete: {importedData.Commands?.Count() ?? 0} Commands, {importedData.Environments?.Count() ?? 0} Environments", 3000);
        }

        public void RemoveEnvironment(RestEnvironment environment)
        {
            Environments.Remove(environment);
        }

        public async Task SaveAsync(int displayNotificationDelay = 1500)
        {
            await ViewModelLocator.Command.SaveCommands();
            await ViewModelLocator.CommandChain.SaveChains();
            var environments = new List<RestEnvironment>() { GlobalEnvironment };
            environments = environments.Concat(Environments).ToList();
            await _environmentService.SaveEnvironmentsAsync(environments);

            await ShowNotificationMessage("Saved", displayNotificationDelay);
        }

        /// <summary>
        /// Called during the creation of the main window, giving view models access to methods that need a MetroWindow reference
        /// (such as showing dialogs)
        /// </summary>
        /// <param name="window"></param>
        public void SetWindow(MetroWindow window)
        {
            _metroWindow = window;
        }

        /// <summary>
        /// Returns true if the user has been in the current environment, without executing anything, for less
        /// than what is specified in SelectedEnvironment.ExecutionWarningDelayMinutes, or if the user manually
        /// said that they wanted to execute in the current environment.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ShouldExecute(string executionType, string commandName)
        {
            if (SelectedEnvironment.ExecutionWarningDelayMinutes < 0)
                return true;

            var latestTime = LastEnvironmentChange > RestCommand.LastExecution ? LastEnvironmentChange : RestCommand.LastExecution;
            var shouldWarn = (DateTime.Now - latestTime).TotalMinutes > SelectedEnvironment.ExecutionWarningDelayMinutes;

            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Run Command",
                NegativeButtonText = "Cancel",
                AnimateShow = true,
                AnimateHide = false
            };

            return !shouldWarn ||
                await ShowMessageAsync("Execute in This Environment?", $"Do you want to run the {executionType} '{commandName}' in: {SelectedEnvironment.Name}",
                MessageDialogStyle.AffirmativeAndNegative, mySettings) == MessageDialogResult.Affirmative;
        }

        public async Task<DialogResult> ShowCustomDialog<TUserControl>(DialogViewModel vm, MetroDialogSettings settings = null) where TUserControl : UserControl
        {
            if (settings == null)
            {
                settings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = false
                };
            }

            CustomDialog customDialog = new CustomDialog();

            //let the view model tell us when it is done
            var tcs = new TaskCompletionSource<string>();
            vm.CloseAction = () =>
            {
                _metroWindow.HideMetroDialogAsync(customDialog, settings);
                tcs.SetResult("Done");
            };

            //attach the vm to the control, and attach that to the dialog
            TUserControl customControl = (TUserControl)Activator.CreateInstance(typeof(TUserControl));
            customControl.DataContext = vm;
            customDialog.Content = customControl;
            
            //display the dialog, and wait for it to finish
            await _metroWindow.ShowMetroDialogAsync(customDialog, settings);
            await tcs.Task;
            
            return vm.DialogResult;
        }

        public async Task<string> ShowInputAsync(string title, string message, MetroDialogSettings settings = null)
        {
            if (settings == null)
            {
                settings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = false
                };
            }

            return await _metroWindow.ShowInputAsync(title, message, settings);
        }

        public async Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null)
        {
            if (settings == null)
            {
                settings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = false
                };
            }

            return await _metroWindow.ShowMessageAsync(title, message, style, settings);
        }

        public async Task ShowNotificationMessage(string message, int displayNotificationDelay = 1500)
        {
            NotificationMessage = message;

            IsSavedNotificationOpen = true;
            await Task.Delay(displayNotificationDelay);
            IsSavedNotificationOpen = false;
        }

        public string ShowOpenFileDialog(string title)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "JSON Files (*.json)|*.json",
                Title = title
            };

            var fileWasPicked = dlg.ShowDialog() == DialogResult.OK;
            return fileWasPicked ? dlg.FileName : null;
        }

        public string ShowSaveFileDialog(string title, string defaultFileName)
        {
            var dlg = new SaveFileDialog
            {
                DefaultExt = ".json",
                FileName = defaultFileName, //no extension
                Filter = "JSON Files (*.json)|*.json",
                Title = title
            };

            var fileWasPicked = dlg.ShowDialog() == DialogResult.OK;
            return fileWasPicked ? dlg.FileName : null;
        }

        #endregion Public Methods

        #region Commands

        public RelayCommand<RestEnvironment> AddEnvironmentCommand => new RelayCommand<RestEnvironment>(AddEnvironment);

        public RelayCommand<RestEnvironment> CloneEnvironmentCommand => new RelayCommand<RestEnvironment>(CloneEnvironment);

        public RelayCommand EnvironmentDisplayToggleCommand => new RelayCommand(() => IsEnvironmentFlyoutOpen = !IsEnvironmentFlyoutOpen);

        public RelayCommand ExportCommand => new RelayCommand(async () => await ExportAsync());

        public RelayCommand GlobalsDisplayToggleCommand => new RelayCommand(() =>
        {
            IsGlobalsFlyoutOpen = !IsGlobalsFlyoutOpen;
            IsEnvironmentFlyoutOpen = false;
        });

        public RelayCommand ImportCommand => new RelayCommand(async () => await ImportAsync());

        public RelayCommand ImportPostmanCommand => new RelayCommand(async () => await ImportAsync(true));

        public RelayCommand<RestEnvironment> RemoveEnvironmentCommand => new RelayCommand<RestEnvironment>(RemoveEnvironment, c => Environments.Count > 1);

        public RelayCommand SaveCommand => new RelayCommand(async () => await SaveAsync());

        public RelayCommand SettingsDisplayToggleCommand => new RelayCommand(() => IsSettingsFlyoutOpen = !IsSettingsFlyoutOpen);

        #endregion Commands
    }
}