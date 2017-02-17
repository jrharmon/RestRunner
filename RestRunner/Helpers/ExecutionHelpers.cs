using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestRunner.Models;
using RestRunner.ViewModels;

namespace RestRunner.Helpers
{
    public static class ExecutionHelpers
    {
        public static async Task ExecuteForParameterList(IList<RestParameter> commandParameters, IList<RestParameter> parameterList, Func<Task> executeAction)
        {
            //make sure that the parameter list doesn't have any duplicate names
            if (parameterList.Count != parameterList.Select(p => p.Name).Distinct().Count())
                throw new ArgumentException("No two parameters can have the same name", nameof(parameterList));

            //store the original command parameter values that are being overridden by a parameter in parameterList, and remove them from the command's paramters
            var originalParameters = commandParameters.Where(p => parameterList.Any(pl => pl.Name == p.Name)).ToList();
            foreach (var parameter in originalParameters)
                commandParameters.Remove(parameter);

            //add a blank command parameter for each parameter in parameterList, which will have its value updated for each execution with the value for that run
            foreach (var parameter in parameterList)
                commandParameters.Add(new RestParameter(parameter.Name, ""));

            //run the action for each parameter set
            var executionCount = parameterList.Max(p => p.PresetValues.Count);
            for (int i = 0; i < executionCount; i++)
            {
                //update each parameter for this iteration
                foreach (var parameter in parameterList)
                {
                    //if there is a value use it.  if there are not enough preset values for the current iteration, then set the value to empty (which is what parameter.Value would be if that check was reached)
                    var curCommandParameter = commandParameters.Single(cp => cp.Name == parameter.Name);
                    if ((!string.IsNullOrEmpty(parameter.Value)) || (i >= parameter.PresetValues.Count))
                        curCommandParameter.Value = parameter.Value;
                    else
                        curCommandParameter.Value = parameter.PresetValues[i];
                }

                await executeAction();
            }

            //remove any parameters added from parameterList
            foreach (var parameter in parameterList)
                commandParameters.Remove(commandParameters.Single(cp => cp.Name == parameter.Name));

            //add the original parameters that were removed, back in
            foreach (var parameter in originalParameters)
                commandParameters.Add(parameter);
        }
    }
}
