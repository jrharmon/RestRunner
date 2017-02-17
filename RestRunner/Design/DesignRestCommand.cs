using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestRunner.Models;
using RestSharp;

namespace RestRunner.Design
{
    [Serializable]
    public class DesignRestCommand : RestCommand
    {
        public DesignRestCommand()
            : base("resourceUrl", "Body", Method.POST)
        {
            Label = "Label";
        }
    }
}
