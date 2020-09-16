using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public ConverterAdapter()
        {
        }

        public bool RegisterControlConverter(IControlConverter converterObj)
        {
            try
            {
                if (_controlConverters == null)
                {
                    _controlConverters = new List<IControlConverter>();
                }
                _controlConverters.Add(converterObj);

                return true;
            }
            catch (System.Exception)
            {

                return false;
            }

        }


        /// <summary>
        /// This method takes an incoming tag/control and its inclusive text represention and returns the converted text.
        /// </summary>
        /// <param name="tagControlName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns>The converted text - otherwise a String.Empty if there is no converter registered for the passed in tagControlName</returns>
        public async Task<string> MigrateTagControl(string tagControlName, string tagNodeContent)
        {
            string _returnResult = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(tagControlName) )
                {
                    return "nothing to convert.";
                }

                //iterate over all of the IControlConverters that have been registerd and find the 
                //*first* one that knows how to convert the passed in tag

                //check if control is available to be converted from IControlConverter
                foreach (var registerdConverter in _controlConverters)
                {
                    if (registerdConverter.AvailableConversionTags.Contains(tagControlName.ToLower()))
                    {
                        //Call migration logic from specific class
                        _returnResult = await registerdConverter.ConvertControlTag(tagControlName, tagNodeContent);
                    }
                }

                return _returnResult;
            }
            catch (System.Exception)
            {

                throw;
            }

        }
    }
}
