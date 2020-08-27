using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Converter class that knows how to translate a specifi asp: tag into a Blazor tag/component
    /// </summary>
    public class AspxToBlazorControlConverter : IControlConverter
    {
        public AspxToBlazorControlConverter() {
        }

        public IReadOnlyList<string> AvailableConversionTags => throw new NotImplementedException();

        public string ConvertControlTag(string tagName, string tagNodeContent)
        {
            throw new NotImplementedException();
        }
    }
}
