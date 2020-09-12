using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Base Contract that all converter Types must implement in order to be called by the main ConverterAdapter Class
    /// </summary>
    public interface IControlConverter
    {
        /// <summary>
        /// List of controls/tags that the implementing Converter class knows how to handle
        /// </summary>
        ReadOnlyCollection<string> AvailableConversionTags { get; }

        /// <summary>
        /// Method to implement which will be called by the ConverterAdapter class
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagNodeContent"></param>
        Task<string> ConvertControlTag(string tagName, string tagNodeContent);
    }
}
