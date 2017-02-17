using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestRunner.Models
{
    public class CaptionedCommand
    {
        public CaptionedCommand(string caption, ICommand command)
        {
            Caption = caption;
            Command = command;
        }

        public String Caption { get; }
        public ICommand Command { get; }

    }
}
