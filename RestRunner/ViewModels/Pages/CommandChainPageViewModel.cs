using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight.CommandWpf;
using RestRunner.Design;
using RestRunner.Models;
using RestRunner.Services;
using RestSharp;

namespace RestRunner.ViewModels.Pages
{
    public class CommandChainPageViewModel : PageViewModel
    {
        private readonly ICommandChainService _chainService;
        private readonly IUserStateService _userStateService;
        private CancellationTokenSource _cancellationTokenOwner;

        public CommandChainPageViewModel(ICommandChainService chainService, IUserStateService userStateService)
        {
            _chainService = chainService;
            _userStateService = userStateService;
            Initialize();
        }

        #region Properties

        private ObservableCollection<RestCommandChain> _chains;
        public ObservableCollection<RestCommandChain> Chains
        {
            get { return _chains ?? (_chains = new ObservableCollection<RestCommandChain>()); }
            set { Set(ref _chains, value); }
        }

        /// <summary>
        /// List of sub-commands to show for the "Submit Multiple" button
        /// </summary>
        private List<CaptionedCommand> _executeMultipleCommands;
        public List<CaptionedCommand> ExecuteMultipleCommands
        {
            get
            {
                if (_executeMultipleCommands == null)
                {
                    _executeMultipleCommands = new List<CaptionedCommand>()
                    {
                        new CaptionedCommand("Single-Threaded", ExecuteSelectedChainMultipleCommand),
                        new CaptionedCommand("Multi-Threaded", ExecuteSelectedChainMultipleParallelCommand)
                    };
                }

                return _executeMultipleCommands;
            }
        }

        private int _multipleExecutionProgress = -1;
        public int MultipleExecutionProgress
        {
            get { return _multipleExecutionProgress; }
            set { Set(ref _multipleExecutionProgress, value); }
        }

        private ObservableCollection<RestResult> _results;
        public ObservableCollection<RestResult> Results
        {
            get { return _results ?? (_results = new ObservableCollection<RestResult>()); }
            set { Set(ref _results, value); }
        }

        private RestCommandChain _selectedChain;
        public RestCommandChain SelectedChain
        {
            get { return _selectedChain; }
            set
            {
                Set(ref _selectedChain, value);
                if ((SelectedChain?.Commands?.Count ?? 0) > 0)
                    SelectedCommand = SelectedChain.Commands[0];
            }
        }

        private RestCommand _selectedCommand;
        public RestCommand SelectedCommand
        {
            get { return _selectedCommand; }
            set { Set(ref _selectedCommand, value); }
        }

        public override string Subtitle => "";

        public override string Title => "Chains";

        #endregion Properties

        #region Private Methods

        private async Task Initialize()
        {
            var tempChains = await _chainService.GetCommandChainsAsync();

            //if there are no saved commands, then load the default ones to give the user something to start with
            if (tempChains.Count == 0)
            {
                tempChains = (await new DesignCommandChainService().GetCommandChainsAsync()).OrderBy(c => c.Label).ToList();
                tempChains.First().Category.IsExpanded = true;
            }

            foreach (var chain in tempChains)
                Chains.Add(chain);

            var curChainId = _userStateService.UserState.SelectedCommandId;
            SelectedChain = Chains.FirstOrDefault(c => c.Id == curChainId) ?? Chains[0];
            SelectedChain.Category.IsExpanded = true; //make sure the chain is always visible in the list
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

            var newChain = new RestCommandChain("New Chain")
            {
                Category = new RestCommandChainCategory(categoryLabel)
            };
            Chains.Add(newChain);
            SelectedChain = newChain;
        }

        public async Task AddChain(RestCommandChainCategory category)
        {
            var label = await ViewModelLocator.Main.ShowInputAsync("Chain Name", "What is the name of the chain?");
            if (label == null)
                return;
            if (string.IsNullOrWhiteSpace(label))
            {
                await ViewModelLocator.Main.ShowMessageAsync("Invalid Name", "The chain name cannot be blank");
                return;
            }

            var newChain = new RestCommandChain(label)
            {
                Category = category
            };
            Chains.Add(newChain);
            SelectedChain = newChain;
            AddCommands();
        }

        public void AddCommands()
        {
            ViewModelLocator.Main.IsChainAddCommandsFlyoutOpen = true;
        }

        public void AddCommandToSelectedChain(RestCommand command)
        {
            SelectedChain.AddCommand(command);
        }

        public void CancelChain()
        {
            _cancellationTokenOwner?.Cancel();
        }

        public void CloneChain(RestCommandChain origChain)
        {
            var newChain = origChain.DeepCopy();
            newChain.Label += " - Copy";
            Chains.Add(newChain);
            SelectedChain = newChain;
        }

        public void CloneCommand(RestCommand origCommand)
        {
            SelectedCommand = SelectedChain.CloneCommand(origCommand);
        }

        public void CopyResultsToClipboard()
        {
            var httpCommunications = Results.Select(r => $"/****************************************{Environment.NewLine} * {r.Command.Label}{Environment.NewLine} ****************************************/{Environment.NewLine}{r.HttpCommunicationDisplay}");
            var communicationSeparator = Environment.NewLine + Environment.NewLine;
            var finalText = string.Join(communicationSeparator, httpCommunications);

            Clipboard.SetText(finalText);
            ViewModelLocator.Main.ShowNotificationMessage("Result HTTP Logs were written to the clipboard");
        }

        public void EditCategory(RestCommandChainCategory category)
        {
            ViewModelLocator.Main.EditChainCategory(category);
        }

        public async Task ExecuteSelectedChain()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Chain", SelectedChain.Label);
            if (!shouldExecute)
                return;

            IsBusy = true;
            MultipleExecutionProgress = -1;
            using (_cancellationTokenOwner = new CancellationTokenSource())
                await SelectedChain.Execute(Results, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment);
            IsBusy = false;
        }

        public async Task ExecuteSelectedChainMultiple()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Chain", SelectedChain.Label);
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
                Results.Clear();
                using (_cancellationTokenOwner = new CancellationTokenSource())
                    await SelectedChain.Execute(Results, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment);

                //make sure nothing failed
                if (Results.Any(r => !r.Succeeded))
                    break;

                MultipleExecutionProgress = i + 1;
            }

            IsBusy = false;
        }

        public async Task ExecuteSelectedChainMultipleParallel()
        {
            var shouldExecute = await ViewModelLocator.Main.ShouldExecute("Chain", SelectedChain.Label);
            if (!shouldExecute)
                return;

            var timesToExecuteStr = await ViewModelLocator.Main.ShowInputAsync("Multiple Execution", "How many times to execute?");
            int timesToExecute;
            if ((!int.TryParse(timesToExecuteStr, out timesToExecute)) || (timesToExecute <= 0))
                return;

            IsBusy = true;
            MultipleExecutionProgress = 0;
            var startTime = DateTime.Now;
            int tasksStarted = 0;
            List<Task> tasks = new List<Task>();
            Results.Clear();

            var addExecution = new Action(() =>
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    ObservableCollection<RestResult> outputResults = new ObservableCollection<RestResult>();
                    await SelectedChain.Execute(outputResults, _cancellationTokenOwner.Token, ViewModelLocator.Main.SelectedEnvironment, ViewModelLocator.Main.GlobalEnvironment).ConfigureAwait(false);

                    if (outputResults.Any(r => !r.Succeeded))
                    {
                        Results = outputResults;
                        throw new Exception("Failed command");
                    }

                    Interlocked.Add(ref _multipleExecutionProgress, 1);
                    RaisePropertyChanged(nameof(MultipleExecutionProgress));
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

                    if (finishedTask.Status != TaskStatus.RanToCompletion)
                        break;

                    tasks.Remove(finishedTask);
                    if (tasksStarted < timesToExecute)
                        addExecution();
                }
            }

            IsBusy = false;
        }

        public override bool HasUnsavedChanges()
        {
            return _chainService.HasChanged(Chains);
        }

        public void RemoveChain(RestCommandChain chain)
        {
            Chains.Remove(chain);
            if (Chains.Count > 0)
                SelectedChain = Chains[0];
        }

        public void RemoveCommand(RestCommand command)
        {
            //in case you are deleting the selected command, store its index, so you can select an adjacent one
            var curSelectedCommandIndex = SelectedChain.Commands.IndexOf(SelectedCommand);

            SelectedChain.Commands.Remove(command);

            if (SelectedCommand == null)
                SelectedCommand = SelectedChain.Commands[Math.Min(curSelectedCommandIndex, SelectedChain.Commands.Count - 1)];
        }

        public void RemoveResult(RestResult result)
        {
            Results.Remove(result);
        }

        public async Task SaveChains()
        {
            await _chainService.SaveCommandChainsAsync(Chains);
        }

        #endregion Public Methods

        #region Commands

        public RelayCommand AddCategoryCommand => new RelayCommand(async () => await AddCategory());

        public RelayCommand<RestCommandChainCategory> AddChainToCategoryCommand => new RelayCommand<RestCommandChainCategory>(async cat => await AddChain(cat));

        public RelayCommand<RestCommand> AddCommandToSelectedChainCommand => new RelayCommand<RestCommand>(AddCommandToSelectedChain);

        public RelayCommand AddCommandsCommand => new RelayCommand(AddCommands);

        public RelayCommand CancelChainCommand => new RelayCommand(CancelChain, () => !(_cancellationTokenOwner?.IsCancellationRequested ?? true) && IsBusy);

        public RelayCommand<RestCommandChain> CloneChainCommand => new RelayCommand<RestCommandChain>(CloneChain);

        public RelayCommand<RestCommand> CloneCommandCommand => new RelayCommand<RestCommand>(CloneCommand);

        public RelayCommand CopyResultsToClipboardCommand => new RelayCommand(CopyResultsToClipboard);

        public RelayCommand<RestCommandChainCategory> EditCategoryCommand => new RelayCommand<RestCommandChainCategory>(EditCategory);

        public RelayCommand ExecuteSelectedChainCommand => new RelayCommand(async () => await ExecuteSelectedChain(), () => !IsBusy && ((SelectedChain?.Commands?.Count ?? 0) > 0));

        public RelayCommand ExecuteSelectedChainMultipleCommand => new RelayCommand(async () => await ExecuteSelectedChainMultiple(), () => !IsBusy && ((SelectedChain?.Commands?.Count ?? 0) > 0));

        public RelayCommand ExecuteSelectedChainMultipleParallelCommand => new RelayCommand(async () => await ExecuteSelectedChainMultipleParallel(), () => !IsBusy && ((SelectedChain?.Commands?.Count ?? 0) > 0));

        public RelayCommand<RestCommandChain> RemoveChainCommand => new RelayCommand<RestCommandChain>(RemoveChain, c => Chains.Count > 1);

        public RelayCommand<RestCommand> RemoveCommandCommand => new RelayCommand<RestCommand>(RemoveCommand, c => (SelectedChain?.Commands?.Count ?? 0) > 1);

        public RelayCommand<RestResult> RemoveResultCommand => new RelayCommand<RestResult>(RemoveResult, r => Results.Count > 0);

        #endregion Commands
    }
}
