using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestRunner.Models;
using RestRunner.ViewModels.Dialogs;

namespace RestRunner.Design
{
    public class DesignImportExportViewModel : ImportExportViewModel
    {
        public DesignImportExportViewModel()
        {
            var allCommands = (new DesignCommandService()).GetCommandsAsync().Result;
            var allCommandCategories = allCommands.Select(c => c.Category).Distinct();
            var allChains = (new DesignCommandChainService()).GetCommandChainsAsync().Result;
            var allChainCategories = allChains.Select(c => c.Category).Distinct();
            var allEnvironments = (new DesignEnvironmentService()).GetEnvironmentsAsync().Result;

            var commandCategoryItems = allCommandCategories.Select(category => new ImportExportItem<RestCommandCategory>(category, allCommands.Count(c => c.Category.Equals(category)), false, false)).ToList();
            CommandCategories = new ObservableCollection<ImportExportItem<RestCommandCategory>>(commandCategoryItems);

            var chainCategoryItems = allChainCategories.Select(category => new ImportExportItem<RestCommandChainCategory>(category, allChains.Count(c => c.Category.Equals(category)), false, false)).ToList();
            ChainCategories = new ObservableCollection<ImportExportItem<RestCommandChainCategory>>(chainCategoryItems);

            var environmentItems = allEnvironments.Select(env => new ImportExportItem<RestEnvironment>(env, 0, false, false));
            Environments = new ObservableCollection<ImportExportItem<RestEnvironment>>(environmentItems);

            Title = "Import Commands";
        }
    }
}
