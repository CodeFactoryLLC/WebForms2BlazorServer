using System.Linq;

namespace WebFormsToBlazorServerCommands.Migration
{
    public class ConverterAdapter : ITagControlConverter
    {
        private IControlConverter _tagConverter = null;
        private string _returnResult = string.Empty;

        public ConverterAdapter(IControlConverter controlTagConverter)
        {
            _tagConverter = controlTagConverter;
        }

        public string MigrateTagControl(string tagControlName, string tagNodeContent)
        {
            //check if control is available to be converted from IControlConverter
            if (!_tagConverter.AvailableConversionTags.Contains(tagControlName))
            {
                return tagNodeContent;
            }

            //Call migration logic from specific class
            _returnResult = _tagConverter.ConvertControlTag(tagControlName, tagNodeContent);

            return _returnResult;
        }
    }
}
