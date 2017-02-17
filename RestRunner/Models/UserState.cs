using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestRunner.Models
{
    [Serializable]
    public class UserState
    {
        public Guid? SelectedCommandId { get; set; }
        
        public Guid? SelectedEnvironmentId { get; set; }
    }
}
