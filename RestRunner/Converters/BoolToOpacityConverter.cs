using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace RestRunner.Converters
{
    /// <summary>
    /// Returns full opacity when the bool is true, and semi-trasparent when false
    /// </summary>
    public sealed class BoolToOpacityConverter : BooleanConverter<Double>
    {
        public BoolToOpacityConverter() : base(1, 0.1)
        { }
    }
}
