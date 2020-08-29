using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Class that is used by the main template migration code to call a set of arbitrary conversions on different kinds of HTML/aspx tags
    /// </summary>
    public class ConverterAdapter : ITagControlConverter
    {
        private string _returnResult = string.Empty;
        private List<IControlConverter> _controlConverters = null;

        public ConverterAdapter(List<IControlConverter> controlTagConverters)
        {
            _controlConverters = controlTagConverters;
        }

        /// <summary>
        /// This method takes an incoming tag/control and its inclusive text represention and returns the converted text.
        /// </summary>
        /// <param name="tagControlName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns>The converted text - otherwise a String.Empty if there is no converter registered for the passed in tagControlName</returns>
        public string MigrateTagControl(string tagControlName, string tagNodeContent)
        {
            string _returnResult = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(tagControlName) )
                {
                    return 
                }

                //iterate over all of the IControlConverters that have been registerd and find the 
                //*first* one that knows how to convert the passed in tag

                //check if control is available to be converted from IControlConverter
                if (!_tagConverter.AvailableConversionTags.Contains(tagControlName))
                {
                    return tagNodeContent;
                }

                //Call migration logic from specific class
                _returnResult = _tagConverter.ConvertControlTag(tagControlName, tagNodeContent);

                return _returnResult;
            }
            catch (System.Exception)
            {

                throw;
            }

        }
    }
}
