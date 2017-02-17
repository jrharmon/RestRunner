using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RestRunner.ViewModels;

namespace RestRunner.Models
{
    /// <summary>
    /// A list of RestCommands that can be run in order, passing state between each command,
    /// using their parameters and capture values.
    /// 
    /// When adding new commands, use the Add or Insert methods, to ensure that copies will
    /// be made, instead of the original version.
    /// 
    /// When creating the chain, a copy of each command is made, and the originals are then
    /// ignored.  This means that if you change the originals, the version inside the chain
    /// won't be updated, and vice versa.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("ID: {Id}, Label: {Label}")]
    public class RestCommandChain : ObservableBase, IEquatable<RestCommandChain>
    {
        #region Properties

        public Guid Id { get; set; }

        private RestCommandChainCategory _category;
        public RestCommandChainCategory Category
        {
            get { return _category; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Category));

                Set(ref _category, value);
            }
        }

        //never additems directly to here.  use the Add or Insert methods instead
        private ObservableCollection<RestCommand> _commands;
        public ObservableCollection<RestCommand> Commands
        {
            get { return _commands; }
            set { Set(ref _commands, value); }
        }

        /// <summary>
        /// Default values for all commands in the chain.  This is copied from the fist command added, and
        /// all chains are set to this as their category.
        /// </summary>
        private RestCommandCategory _defaultCommandCategory;
        public RestCommandCategory DefaultCommandCategory
        {
            get { return _defaultCommandCategory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(DefaultCommandCategory));

                Set(ref _defaultCommandCategory, value);
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { Set(ref _description, value); }
        }

        private string _label;
        public string Label
        {
            get { return _label; }
            set { Set(ref _label, value); }
        }

        #endregion Properties

        //for serialization use only, as it requires a parameterless constructor
        private RestCommandChain()
        {
            
        }

        public RestCommandChain(List<RestCommand> commands, string label)
        {
            Id = Guid.NewGuid();
            Commands = new ObservableCollection<RestCommand>(commands.Select(c => c.DeepCopy()).ToList());
            Label = label;
        }

        public RestCommandChain(string label)
        {
            Id = Guid.NewGuid();
            Commands = new ObservableCollection<RestCommand>();
            Label = label;
        }

        #region Public Methods

        /// <summary>
        /// Add a copy of a command at the end of the command list.
        /// </summary>
        /// <param name="command">This command will be copied, and that copy will be added to the command list</param>
        public void AddCommand(RestCommand command)
        {
            //if this is the first command, then use its category as the default one for the chain
            if (Commands.Count == 0)
            {
                DefaultCommandCategory = command.Category.DeepCopy();
                DefaultCommandCategory.Name = "[ChainDefault]";
            }

            var commandToAdd = command.DeepCopy();
            commandToAdd.Category = DefaultCommandCategory;

            var origCategory = command.Category;
            if (origCategory.CredentialName != DefaultCommandCategory.CredentialName)
                commandToAdd.CredentialName = origCategory.CredentialName;
            if ((origCategory.Username != DefaultCommandCategory.Username) ||
                (origCategory.Password != DefaultCommandCategory.Password))
            {
                commandToAdd.Username = origCategory.Username;
                commandToAdd.Password = origCategory.Password;
            }
            //there is no way to know for sure, without knowing the values of all variables, if the resource depends on the base url, or if it is absolute, so just
            //make a best guess.  if wrong, the user can always fix it after
            if (origCategory.BaseUrl != DefaultCommandCategory.BaseUrl)
            {
                if ((!commandToAdd.ResourceUrl.StartsWith("%")) && //if it starts with a variable, it is most likely an environmental variable for the base
                    (!commandToAdd.ResourceUrl.StartsWith("http://")) &&
                    (!commandToAdd.ResourceUrl.StartsWith("https://")))
                {
                    commandToAdd.ResourceUrl = origCategory.BaseUrl + commandToAdd.ResourceUrl;
                }
            }

            Commands.Add(commandToAdd);
        }

        public RestCommand CloneCommand(RestCommand command)
        {
            var newCommand = command.DeepCopy();
            newCommand.Label += " - Copy";
            Commands.Add(newCommand);
            return newCommand;
        }

        /// <summary>
        /// Create a completely new object that is a copy of this one, and each individual
        /// command is a deep copy of the original.
        /// </summary>
        /// <returns></returns>
        public RestCommandChain DeepCopy()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream s = new MemoryStream())
            {
                formatter.Serialize(s, this);
                s.Position = 0;
                RestCommandChain result = (RestCommandChain)formatter.Deserialize(s);
                result.Id = Guid.NewGuid();
                result.Category = Category;
                result.Commands = new ObservableCollection<RestCommand>(Commands.Select(c => c.DeepCopy()));
                return result;
            }
        }

        /// <summary>
        /// Execute each command, one after the other, passing any captured values from commands to
        /// the parameters of any commands executed after it.
        /// </summary>
        /// <param name="outputResults">A list of the results from all RestCommands, cleared out at
        /// the start of processing, and added to as each result comes in.</param>
        /// <param name="cancelToken"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public async Task Execute(ObservableCollection<RestResult> outputResults, CancellationToken cancelToken, RestEnvironment environment = null, RestEnvironment globalEnvironment = null)
        {
            outputResults.Clear();
            var variables = new Dictionary<string, string>();

            foreach (var command in Commands)
            {
                var result = await command.Execute(variables, cancelToken, environment, globalEnvironment);
                outputResults.Add(result);

                if (!result.Succeeded)
                    return;

                foreach (var value in result.CapturedValues)
                    variables[value.Key] = value.Value;
            }
        }

        /// <summary>
        /// Execute each command, one after the other, passing any captured values from commands to
        /// the parameters of any commands executed after it.
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <param name="environment"></param>
        /// <returns>A list of the results from all RestCommands</returns>
        public async Task<List<RestResult>> Execute(CancellationToken cancelToken, RestEnvironment environment = null, RestEnvironment globalEnvironment = null)
        {
            ObservableCollection<RestResult> results = new ObservableCollection<RestResult>();
            await Execute(results, cancelToken, environment, globalEnvironment);

            return results.ToList();
        }

        public bool Equals(RestCommandChain other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                Equals(_category, other._category) &&
                _commands.SequenceEqual(other._commands) &&
                string.Equals(_description, other._description) &&
                string.Equals(_label, other._label);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as RestEnvironment);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Insert a copy of a command in the proper location.
        /// </summary>
        /// <param name="index">The 0-based index at which to insert the command</param>
        /// <param name="command">This command will be copied, and that copy will be inserted into the command list</param>
        public void InsertCommand(int index, RestCommand command)
        {
            Commands.Insert(index, command.DeepCopy());
        }

        #endregion Public Methods
    }
}
