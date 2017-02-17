using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using RestRunner.Design;
using RestRunner.Helpers;
using RestRunner.Models;
using RestRunner.Services;
using RestRunner.ViewModels.Dialogs;
using RestRunner.Views.Dialogs;
using RestSharp;

namespace RestRunner.ViewModels.Pages
{
    public class CommandPageViewModel : PageViewModel
    {
        private const int MaxResults = 20;

        private readonly ICommandService _commandService;
        private readonly IUserStateService _userStateService;
        private readonly DispatcherTimer _executionTimer = new DispatcherTimer();
        private CancellationTokenSource _cancellationTokenOwner;

        public CommandPageViewModel(ICommandService commandService, IUserStateService userStateService)
        {
            _commandService = commandService;
            _userStateService = userStateService;
            Initialize();
        }

        #region Properties

        public IEnumerable<RestCommandCategory> Categories
        {
            get { return _commands.Select(c => c.Category).Distinct(); }
        }

        private ObservableCollection<RestCommand> _commands;
        public ObservableCollection<RestCommand> Commands
        {
            get { return _commands ?? (_commands = new ObservableCollection<RestCommand>()); }
            set { Set(ref _commands, value); }
        }

        /// <summary>
        /// A list of environment variables that persist between command calls.  New values
        /// are added automatically as they get captured from commands.
        /// </summary>
        private ObservableCollection<CaptionedKeyValuePair> _environmentVariables;
        public ObservableCollection<CaptionedKeyValuePair> EnvironmentVariables
        {
            get { return _environmentVariables ?? (_environmentVariables = new ObservableCollection<CaptionedKeyValuePair>()); }
            set { Set(ref _environmentVariables, value); }
        }

        /// <summary>
        /// List of sub-commands to show for the "Submit Multiple" button
        /// </summary>
        private List<CaptionedCommand> _executeMultipleCommands;
        public List<CaptionedCommand> ExecuteMultipleCommands
        {
            get
            {
                return _executeMultipleCommands ?? (_executeMultipleCommands = new List<CaptionedCommand>()
                {
                    new CaptionedCommand("Single-Threaded", ExecuteSelectedCommandMultipleCommand),
                    new CaptionedCommand("Multi-Threaded", ExecuteSelectedCommandMultipleParallelCommand)
                    //new CaptionedCommand("Parameter List", ExecuteSelectedCommandParameterListCommand)
                });
            }
        }

        private int _multipleExecutionProgress = -1;
        public int MultipleExecutionProgress
        {
            get { return _multipleExecutionProgress; }
            set { Set(ref _multipleExecutionProgress, value); }
        }

        /// <summary>
        /// The result of the most recent command executed.
        /// </summary>
        private RestResult _result;
        public RestResult Result
        {
            get { return _result; }
            set { Set(ref _result, value); }
        }

        /// <summary>
        /// A running list of results from all commands executed
        /// </summary>
        private ObservableCollection<RestResult> _results;
        public ObservableCollection<RestResult> Results
        {
            get { return _results ?? (_results = new ObservableCollection<RestResult>()); }
            set { Set(ref _results, value); }
        }

        private RestCommand _selectedCommand;
        public RestCommand SelectedCommand
        {
            get { return _selectedCommand; }
            set
            {
                Set(ref _selectedCommand, value);
                _userStateService.UserState.SelectedCommandId = value?.Id;
                _userStateService.SaveUserStateAsync();
            }
        }

        public RestEnvironment SelectedEnvironment => ViewModelLocator.Main.SelectedEnvironment;

        public override string Subtitle => "";

        public override string Title => "Commands";

        #endregion Properties

        #region Private Methods

        private void AddResult(RestResult result)
        {
            Results.Add(result);

            //keep the number of results low, since virtualization isn't enabled, and it will slow down performance when switching tabs (turning on virtualization will disable smooth scrolling of the results list, which is more important)
            while (Results.Count > MaxResults)
                Results.RemoveAt(0);

            //update the envrionment with any captured values
            foreach (var value in result.CapturedValues)
                EnvironmentVariables.Set(value.Key, value.Value);
        }

        private async Task Initialize()
        {
            var tempCommands = await _commandService.GetCommandsAsync();

            //if there are no saved commands, then load the default ones to give the user something to start with
            if (tempCommands.Count == 0)
            {
                tempCommands = (await new DesignCommandService().GetCommandsAsync()).OrderBy(c => c.Label).ToList();
                tempCommands.First().Category.IsExpanded = true;
            }

            foreach (var command in tempCommands)
                Commands.Add(command);

            var curCommandId = _userStateService.UserState.SelectedCommandId;
            SelectedCommand = Commands.FirstOrDefault(c => c.Id == curCommandId) ?? Commands[0];
            SelectedCommand.Category.IsExpanded = true; //make sure the command is always visible in the list
        }

        #endregion Private Methods

        #region Public Methods

        public async Task AddCategory()
        {
            var categoryLabel = await ViewModelLocator.Main.ShowInputAsync("Category Name", "What is the name of the category?");
            if (categoryLabel == null)
                return;
            if (string.IsNullOrWhiteSpace(categoryLabel))
            {
                await ViewModelLocator.Main.ShowMessageAsync("Invalid Name", "The category name cannot be blank");
                return;
            }

            var newCommand = new RestCommand("", "", Method.POST)
            {
                Category = new RestCommandCategory(categoryLabel, "", "", "") {IsExpanded = true},
                Label = "New Command"
            };
            Commands.Add(newCommand);
            SelectedCommand = newCommand;
            EditCategory(SelectedCommand.Category);
        }

        public async Task AddCommand(RestCommandCategory category = null)
        {
            var label = await ViewModelLocator.Main.ShowInputAsync("Command Name", "What is the name of the command?");
            if (label == null)
                return;
            if (string.IsNullOrWhiteSpace(label))
            {
                await ViewModelLocator.Main.ShowMessageAsync("Invalid Name", "The command name cannot be blank");
                return;
            }

            var newCommand = new RestCommand("", "", Method.POST)
            {
                Category = category ?? SelectedCommand.Category,
                Label = label
            };
            Commands.Add(newCommand);
            SelectedCommand = newCommand;
        }

        public void ClearEnvironmentVariables()
        {
            EnvironmentVariables.Clear();
        }

        public void ClearResults()
        {
            Results.Clear();
        }

        /// <summary>
        /// Clear out any environment variables, and past results
        /// </summary>
        public void ClearWorkspace()
        {
            ClearEnvironmentVariables();
            ClearResults();
        }

        public async Task CloneCommand(RestCommand origCommand)
        {
            CloneViewModel vm = new CloneViewModel {NameTitle = "Command Name", ClonedName = origCommand.Label + " - Copy", Title = "Copy Command", Category = origCommand.Category};
            var dlgResult = await ViewModelLocator.Main.ShowCustomDialog<CloneDialog>(vm);

            if (dlgResult != DialogResult.OK)
                return;

            var newCommand = origCommand.DeepCopy();
            newCommand.Label = vm.ClonedName;
            newCommand.Category = vm.Category;
            Commands.Add(newCommand);
            SelectedCommand = newCommand;
        }

        public void CopyResultsToClipboard()
        {
            var httpCommunications = Results.Select(r => $"/****************************************{Environment.NewLine} * {r.Command.Label}{Environment.NewLine} ****************************************/{Environment.NewLine}{r.HttpCommunicationDisplay}");
            var communicationSeparator = Environment.NewLine + Environment.NewLine;
            var finalText = string.Join(communicationSeparator, httpCommunications);

            Clipboard.SetText(finalText);
            ViewModelLocator.Main.ShowNotificationMessage("Result HTTP Logs were written to the clipboard");
        }
        
        public void EditCategory(RestCommandCategory category)
        {
            ViewModelLocator.Main.EditCommandCategory(category);
        }

        public async Task ExecuteSelectedCommand()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Command", SelectedCommand.Label);
            if (!shouldExecute)
                return;

            IsBusy = true;

            //run the command
            MultipleExecutionProgress = -1;
            using (_cancellationTokenOwner = new CancellationTokenSource())
                Result = await SelectedCommand.Execute(EnvironmentVariables, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment);
            AddResult(Result);

            IsBusy = false;
            BusyDuration = TimeSpan.Zero;
        }

        public async Task ExecuteSelectedCommandMultiple()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Command", SelectedCommand.Label);
            if (!shouldExecute)
                return;

            var timesToExecuteStr = await ViewModelLocator.Main.ShowInputAsync("Multiple Execution", "How many times to execute?");
            int timesToExecute;
            if ((!int.TryParse(timesToExecuteStr, out timesToExecute)) || (timesToExecute <= 0))
                return;

            IsBusy = true;

            var startTime = DateTime.Now;
            MultipleExecutionProgress = 0;
            for (int i = 0; i < timesToExecute; i++)
            {
                using (_cancellationTokenOwner = new CancellationTokenSource())
                    Result = await SelectedCommand.Execute(EnvironmentVariables, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment);
                AddResult(Result);

                //make sure nothing failed
                if (!Result.Succeeded)
                    break;

                MultipleExecutionProgress = i + 1;
            }

            IsBusy = false;
        }

        public async Task ExecuteSelectedCommandMultipleParallel()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Chain", SelectedCommand.Label);
            if (!shouldExecute)
                return;

            var timesToExecuteStr = await ViewModelLocator.Main.ShowInputAsync("Multiple Execution", "How many times to execute?");
            int timesToExecute;
            if ((!int.TryParse(timesToExecuteStr, out timesToExecute)) || (timesToExecute <= 0))
                return;

            IsBusy = true;

            var startTime = DateTime.Now;
            MultipleExecutionProgress = 0;
            int tasksStarted = 0;
            List<Task<RestResult>> tasks = new List<Task<RestResult>>();

            var addExecution = new Action(() =>
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    ObservableCollection<RestResult> outputResults = new ObservableCollection<RestResult>();
                    var result = await SelectedCommand.Execute(EnvironmentVariables, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment);
                    //AddResult(Result);

                    if (outputResults.Any(r => !r.Succeeded))
                    {
                        Results = outputResults;
                        throw new Exception("Failed command");
                    }

                    Interlocked.Add(ref _multipleExecutionProgress, 1);
                    RaisePropertyChanged(nameof(MultipleExecutionProgress));

                    return result;
                }, TaskCreationOptions.LongRunning).Unwrap();
                tasks.Add(task);
                tasksStarted++;
            });

            using (_cancellationTokenOwner = new CancellationTokenSource())
            {
                var threadCount = Math.Min(64, timesToExecute);
                //64 threads is roughly 20-25% faster than 32.  it doesn't get any faster after that though
                for (int i = 0; i < threadCount; i++)
                    addExecution();

                while (tasks.Count > 0)
                {
                    var finishedTask = await Task.WhenAny(tasks);
                    AddResult(finishedTask.Result);

                    if (finishedTask.Status != TaskStatus.RanToCompletion)
                        break;

                    tasks.Remove(finishedTask);
                    if (tasksStarted < timesToExecute)
                        addExecution();
                }
            }

            IsBusy = false;
        }

        /// <summary>
        /// Execute the command for every parameter in the parameter list
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteSelectedCommandParameterList()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Command", SelectedCommand.Label);
            if (!shouldExecute)
                return;

            IsBusy = true;

            //TEMP: Should be passed in
            var parameterList = new List<RestParameter>
            {
                new RestParameter("PaymentId", EnvironmentVariables.Get("PaymentId")),
                new RestParameter("Currency", "USD"),
                new RestParameter("Amount", new List<string> {"1", "2", "3"})
            };

            await ExecutionHelpers.ExecuteForParameterList(SelectedCommand.Parameters, parameterList,
                async () =>
                {
                    using (_cancellationTokenOwner = new CancellationTokenSource())
                        Result = await SelectedCommand.Execute(EnvironmentVariables, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment);
                    AddResult(Result);
                });

            IsBusy = false;
        }

        public override bool HasUnsavedChanges()
        {
            return _commandService.HasChanged(Commands);
        }

        public void RemoveCommand(RestCommand command)
        {
            Commands.Remove(command);

            if (Commands.Count > 0)
                SelectedCommand = Commands[0];
        }

        public void RemoveResult(RestResult result)
        {
            Results.Remove(result);
        }

        public async Task SaveCommands()
        {
            await _commandService.SaveCommandsAsync(Commands);
        }

        #endregion Public Methods

        #region Commands

        public RelayCommand AddCategoryCommand => new RelayCommand(async () => await AddCategory());

        public RelayCommand AddCommandCommand => new RelayCommand(async () => await AddCommand());

        public RelayCommand<RestCommandCategory> AddCommandToCategoryCommand => new RelayCommand<RestCommandCategory>(async cat => await AddCommand(cat));

        public RelayCommand CancelCommandCommand => new RelayCommand(() => _cancellationTokenOwner?.Cancel(), () => !(_cancellationTokenOwner?.IsCancellationRequested ?? true));

        public RelayCommand ClearEnvironmentVariablesCommand => new RelayCommand(ClearEnvironmentVariables);

        public RelayCommand ClearResultsCommand => new RelayCommand(ClearResults);

        public RelayCommand ClearWorkspaceCommand => new RelayCommand(ClearWorkspace);

        public RelayCommand<RestCommand> CloneCommandCommand => new RelayCommand<RestCommand>(async c => await CloneCommand(c));

        public RelayCommand CopyResultsToClipboardCommand => new RelayCommand(CopyResultsToClipboard);

        public RelayCommand<RestCommandCategory> EditCategoryCommand => new RelayCommand<RestCommandCategory>(EditCategory);
        
        public RelayCommand ExecuteSelectedCommandCommand => new RelayCommand(async () => await ExecuteSelectedCommand(), () => !IsBusy);

        public RelayCommand ExecuteSelectedCommandMultipleCommand => new RelayCommand(async () => await ExecuteSelectedCommandMultiple(), () => !IsBusy);

        public RelayCommand ExecuteSelectedCommandMultipleParallelCommand => new RelayCommand(async () => await ExecuteSelectedCommandMultipleParallel(), () => !IsBusy);

        public RelayCommand ExecuteSelectedCommandParameterListCommand => new RelayCommand(async () => await ExecuteSelectedCommandParameterList(), () => !IsBusy);

        public RelayCommand<RestCommand> RemoveCommandCommand => new RelayCommand<RestCommand>(RemoveCommand, c => Commands.Count > 1);

        public RelayCommand<RestResult> RemoveResultCommand => new RelayCommand<RestResult>(RemoveResult, r => Results.Count > 0);

        public RelayCommand SaveCommandsCommand => new RelayCommand(async () => await SaveCommands());

        #endregion Commands
    }
}
