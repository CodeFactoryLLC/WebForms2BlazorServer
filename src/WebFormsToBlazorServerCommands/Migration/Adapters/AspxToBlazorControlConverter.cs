using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeFactory.Formatting.CSharp;
using HtmlAgilityPack;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Converter class that knows how to translate a specifi asp: tag into a Blazor tag/component
    /// </summary>
    public class AspxToBlazorControlConverter : ControlConverterBase
    {

        public AspxToBlazorControlConverter(ITagControlConverter adapterHost) : base(adapterHost)
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

        /// <summary>
        /// Class specific override of the baseclass ControlConverterBase
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns></returns>
        public override async Task<string> ConvertControlTag(string tagName, string tagNodeContent)
        {
            string _result = string.Empty;

            switch (tagName.ToLowerInvariant())
            {
                case "asp:content":
                    _result = await ConvertContentControl(tagNodeContent);
                    break;

                case "asp:formview":
                    _result = await ConvertFormViewControl(tagNodeContent);
                    break;

                case "asp:editform":
                    _result = await ConvertEditFormControl(tagNodeContent);
                    break;

                case "asp:listview":
                    _result = await ConvertListViewControl(tagNodeContent);
                    break;

                default:
                    _result = await ConvertGenericControl(tagNodeContent);
                    break;
            }

            return _result;

        }

        #region Private conversion methods

        /// <summary>
        /// This method is used to generically convert an asp control by removing the 'asp:' 
        /// prefix from the control name and then returning the resultant content back to the caller.
        /// </summary>
        /// <param name="nodeContent"></param>
        /// <returns>converted control content</returns>
        private async Task<string> ConvertGenericControl(string nodeContent)
        {
            var model = new HtmlDocument();
            model.LoadHtml(nodeContent);

            try
            {
                int pos = nodeContent.IndexOf("asp:");
                if (pos < 0)
                {
                    return nodeContent;
                }

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var aspFormTags = model.DocumentNode.Descendants().Where(p => p.Name.ToLower().Contains("asp:")).ToList();
                foreach (var formObj in aspFormTags)
                {
                    var migratedControlText = await _adapterHost.MigrateTagControl(formObj.Name, formObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);

                    formObj.ParentNode.ReplaceChild(tempNode, formObj);
                }

                return model.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// This method understands now to convert an asp:FormView control into a blazor equivalent.
        /// </summary>
        /// <param name="nodeContent"></param>
        /// <returns></returns>
        private async Task<string> ConvertFormViewControl(string nodeContent)
        {
            var currentModel = new HtmlDocument();
            var newModel = new HtmlDocument();
            currentModel.LoadHtml(nodeContent);

            try
            {
                var editFormNode = currentModel.DocumentNode.FirstChild;// ("//asp:listview");// model.All.Where(p => p.LocalName.ToLower().Equals("asp:formview")).FirstOrDefault();

                newModel.LoadHtml($"<EditForm Model={editFormNode.GetAttributeValue("ItemType", "")} OnValidSubmit={editFormNode.GetAttributeValue("SelectMethod","")}></EditForm>");
                var newNode = newModel.DocumentNode.SelectSingleNode("//editform");
                //this is now a live list having substituted the EditForm control for the old FormView one
                newNode.AppendChildren(editFormNode.ChildNodes);

                //deal with itemtemplates... ??

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var aspFormTags = newModel.DocumentNode.FirstChild.Descendants().Where(p => p.Name.ToLower().Contains("asp:")).ToList();
                foreach (var formObj in aspFormTags)
                {
                    var migratedControlText = await _adapterHost.MigrateTagControl(formObj.Name, formObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);

                    formObj.ParentNode.ReplaceChild(tempNode, formObj);
                }

                return newModel.DocumentNode.OuterHtml;

                //var editFormNode = model.All.Where(p => p.LocalName.ToLower().Equals("asp:formview")).FirstOrDefault();
                //var newNode = parser.ParseFragment($"<EditForm Model={editFormNode.GetAttribute("ItemType")} OnValidSubmit={editFormNode.GetAttribute("SelectMethod")}></EditForm>", editFormNode);

                ////this is now a live list having substituted the EditForm control for the old FormView one
                //newNode.First().AppendNodes(editFormNode.ChildNodes.ToArray());

                //if (model.All.Any(p => p.TagName.ToLower().Equals("itemtemplate")))
                //{
                //    //deal with itemtemplates... ??
                //}

                ////send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                //var aspFormTags = newNode.First().Descendents<IElement>().Where(p => p.NodeName.ToLower().Contains("asp:")).ToList();
                //foreach (var formObj in aspFormTags)
                //{
                //    var migratedControlText = await _adapterHost.MigrateTagControl(formObj.NodeName, formObj.OuterHtml);
                //    var tempNode = parser.ParseFragment(migratedControlText, null);

                //    //ParseFragment always adds on a HTML & BODY tags, at least with this call setup.  We need to pull out *just* the element that we have migrated.
                //    var appendElement = tempNode.GetElementsByTagName("BODY").First().ChildNodes;

                //    formObj.Replace(appendElement.ToArray());
                //}

                //return newNode.First().ToHtml();//.OuterHTML; // .ToString();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// This method understands now to convert an asp:FormView control into a blazor equivalent.
        /// </summary>
        /// <param name="nodeContent"></param>
        /// <returns></returns>
        private async Task<string> ConvertListViewControl(string nodeContent)
        {
            //var model = await _angleSharpContext.OpenAsync(req => req.Content(nodeContent));
            var currentModel = new HtmlDocument();
            var newModel = new HtmlDocument();
            currentModel.LoadHtml(nodeContent);
            

            try
            {
                var listViewNode = currentModel.DocumentNode.FirstChild;// ("//asp:listview");// model.All.Where(p => p.LocalName.ToLower().Equals("asp:formview")).FirstOrDefault();
                
                newModel.LoadHtml($"<ListView Model={listViewNode.GetAttributeValue("ItemType","")} OnValidSubmit={listViewNode.GetAttributeValue("SelectMethod","")}></ListView>");
                var newNode = newModel.DocumentNode.SelectSingleNode("//listview");
                //this is now a live list having substituted the EditForm control for the old FormView one
                newNode.AppendChildren(listViewNode.ChildNodes);

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var aspFormTags =  newModel.DocumentNode.FirstChild.Descendants().Where(p => p.Name.ToLower().Contains("asp:")).ToList();
                foreach (var formObj in aspFormTags)
                {
                    var migratedControlText = await _adapterHost.MigrateTagControl(formObj.Name, formObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);

                    formObj.ParentNode.ReplaceChild(tempNode, formObj);
                }

                return newModel.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// This method understands how to convert an asp:EditForm control into a blazor equivalent.
        /// </summary>
        /// <param name="nodeContent"></param>
        /// <returns></returns>
        private async Task<string> ConvertEditFormControl(string nodeContent)
        {
            string _result = string.Empty;

            try
            {
                //return content as-is for now.
                return nodeContent;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// This method is used to convert the asp:Content control to a Blazor equivalent.  In this particular 
        /// case - there is no equivalent.  This control is the top-level asp:* container for a page and really just holds all other
        /// controls that are found in an *.aspx page definition.
        /// </summary>
        /// <param name="nodeContent"></param>
        /// <returns></returns>
        private async Task<string> ConvertContentControl(string nodeContent)
        {
            string _result = string.Empty;
            var model = new HtmlDocument();

            try
            {
                model.LoadHtml(nodeContent);
                var contentControlObj = model.DocumentNode.FirstChild;

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var aspFormTags = contentControlObj.Descendants().Where(p => p.Name.ToLower().Contains("asp:")).ToList();
                foreach (var formObj in aspFormTags)
                {
                    var migratedControlText = await _adapterHost.MigrateTagControl(formObj.Name, formObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);

                    formObj.ParentNode.ReplaceChild(tempNode, formObj);
                }

                //There isn't any asp:content control equivalent, so we'll just return the innerHTML
                return contentControlObj.InnerHtml;
 
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion
    }
}
