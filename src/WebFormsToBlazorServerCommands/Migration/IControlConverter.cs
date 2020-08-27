using System.Collections.Generic;

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
        IReadOnlyList<string> AvailableConversionTags { get; }

        /// <summary>
        /// Method to implement which will be called by the ConverterAdapter class
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns></returns>
        string ConvertControlTag(string tagName, string tagNodeContent);
    }
}
