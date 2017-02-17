using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace RestRunner.Helpers
{
    public class EnumToItemsSource : MarkupExtension
    {
        private readonly Type _type;
        private readonly bool _sortValues;

        public EnumToItemsSource(Type type, bool sortValues = false)
        {
            _type = type;
            _sortValues = sortValues;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var values = Enum.GetValues(_type)
                .Cast<object>()
                .Select(e => new {Value = e, DisplayName = e.ToString()});

            if (_sortValues)
                values = values.OrderBy(e => e.DisplayName);

            return values;
        }
    }
}
