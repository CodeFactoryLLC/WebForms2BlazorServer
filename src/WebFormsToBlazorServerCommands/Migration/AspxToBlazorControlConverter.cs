using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Converter class that knows how to translate a specifi asp: tag into a Blazor tag/component
    /// </summary>
    public class AspxToBlazorControlConverter : ControlConverterBase
    {
        public AspxToBlazorControlConverter(ITagControlConverter adapterHost):base(adapterHost) 
        {
           _TagsICanConvert = new List<string>();

            _TagsICanConvert.Add("asp:listview");
            _TagsICanConvert.Add("asp:content");
            _TagsICanConvert.Add("asp:formview");
            _TagsICanConvert.Add("asp:editform");
            _TagsICanConvert.Add("asp:button");
            _TagsICanConvert.Add("asp:checkbox");
            _TagsICanConvert.Add("asp:hyperlink");
            _TagsICanConvert.Add("asp:image");
            _TagsICanConvert.Add("asp:imagebutton");
            _TagsICanConvert.Add("asp:label");
            _TagsICanConvert.Add("asp:linkbutton");
            _TagsICanConvert.Add("asp:panel");
            _TagsICanConvert.Add("asp:radiobutton");
            _TagsICanConvert.Add("asp:table");
            _TagsICanConvert.Add("asp:tablecell");
            _TagsICanConvert.Add("asp:tablerow");
            _TagsICanConvert.Add("asp:textbox");
            _TagsICanConvert.Add("asp:listbox");
            _TagsICanConvert.Add("asp:checkboxlist");
            _TagsICanConvert.Add("asp:radiobuttonlist");
            _TagsICanConvert.Add("asp:datalist");
            _TagsICanConvert.Add("asp:datagrid");
            _TagsICanConvert.Add("asp:dropdownlist");
                        
        }


    }
}
