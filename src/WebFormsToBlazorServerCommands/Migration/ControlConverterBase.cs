using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Baseclass for all more specific converters to inherit from.
    /// </summary>
    public class ControlConverterBase : IControlConverter
    {
        internal ITagControlConverter _adapterHost = null;
        internal List<string> _TagsICanConvert = null;

        public ControlConverterBase(ITagControlConverter adapterHost)
        {
            _adapterHost = adapterHost;
        }

        /// <summary>
        /// List of controls/tags that the implementing Converter class knows how to handle
        /// </summary>
        public ReadOnlyCollection<string> AvailableConversionTags
        {
            get
            {
                return _TagsICanConvert?.AsReadOnly();
            }
        }

        /// <summary>
        /// Method to implement which will be called by the ConverterAdapter class
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns></returns>
        public virtual async Task<string> ConvertControlTag(string tagName, string tagNodeContent)
        {
            StringBuilder _result = new StringBuilder();

            try
            {
                _result.Append(tagNodeContent);

                return _result.ToString();
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
