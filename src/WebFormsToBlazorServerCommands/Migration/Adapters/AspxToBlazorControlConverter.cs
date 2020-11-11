using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeFactory.Formatting.CSharp;
using CodeFactory.Markup.Adapter;
using HtmlAgilityPack;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Converter class that knows how to translate a specifi asp: tag into a Blazor tag/component
    /// </summary>
    public class AspxToBlazorControlConverter : BaseTagAdapter
    {

        public AspxToBlazorControlConverter(IAdapterHost adapterHost) : base(adapterHost)
        {
            this.RegisterSupportTag("asp:listview");
            this.RegisterSupportTag("asp:content");
            this.RegisterSupportTag("asp:formview");
            this.RegisterSupportTag("asp:editform");
            this.RegisterSupportTag("asp:button");
            this.RegisterSupportTag("asp:checkbox");
            this.RegisterSupportTag("asp:hyperlink");
            this.RegisterSupportTag("asp:image");
            this.RegisterSupportTag("asp:imagebutton");
            this.RegisterSupportTag("asp:label");
            this.RegisterSupportTag("asp:linkbutton");
            this.RegisterSupportTag("asp:panel");
            this.RegisterSupportTag("asp:radiobutton");
            this.RegisterSupportTag("asp:table");
            this.RegisterSupportTag("asp:tablecell");
            this.RegisterSupportTag("asp:tablerow");
            this.RegisterSupportTag("asp:textbox");
            this.RegisterSupportTag("asp:listbox");
            this.RegisterSupportTag("asp:checkboxlist");
            this.RegisterSupportTag("asp:radiobuttonlist");
            this.RegisterSupportTag("asp:datalist");
            this.RegisterSupportTag("asp:datagrid");
            this.RegisterSupportTag("asp:dropdownlist");
            this.RegisterSupportTag("asp:placeholder");
            this.RegisterSupportTag("asp:requiredfieldvalidator");
            this.RegisterSupportTag("asp:literal");

            this.RegisterSupportTag("asp:validationsummary");
            this.RegisterSupportTag("asp:modelerrormessage");
            this.RegisterSupportTag("asp:comparevalidator");
            this.RegisterSupportTag("asp:fileupload");
            this.RegisterSupportTag("asp:regularexpressionvalidator");
            this.RegisterSupportTag("asp:gridview");
            this.RegisterSupportTag("asp:boundfield");
            this.RegisterSupportTag("asp:detailsview");
            this.RegisterSupportTag("asp:templatefield");

        }

        /// <summary>
        /// Class specific override of the baseclass ControlConverterBase
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagNodeContent"></param>
        /// <returns></returns>
        public override async Task<string> ConvertTag(string tagName, string tagNodeContent)
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

        public override async Task<ConversionResult> ConvertTagWithResult(string tag, string content)
        {
            string _result = string.Empty;
            ConversionResult returnValue = null;

            try
            {
                switch (tag.ToLowerInvariant())
                {
                    case "asp:content":
                        returnValue = ConversionResult.Init(true, await ConvertContentControl(content));

                        break;

                    case "asp:formview":
                        returnValue = ConversionResult.Init(true, await ConvertContentControl(content));
                        break;

                    case "asp:editform":
                        returnValue = ConversionResult.Init(true, await ConvertContentControl(content));
                        break;

                    case "asp:listview":
                        returnValue = ConversionResult.Init(true, await ConvertContentControl(content));
                        break;

                    default:
                        returnValue = ConversionResult.Init(true, await ConvertGenericControl(content));
                        break;
                }

            }
            catch (Exception ex)
            {
                returnValue = ConversionResult.Init(false, ex.Message);
            }

            return returnValue;
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
            HtmlNode rootNode = null;

            try
            {
                rootNode = model.DocumentNode.FirstChild;

                if (rootNode.Name.Contains("asp:"))
                {
                    rootNode.Name = rootNode.Name.Replace("asp:", string.Empty);
                }

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var allchildren = rootNode.Descendants().Where(p => p.Name.ToLower().Contains("asp:"));
                List<HtmlNode> targetedChildren = new List<HtmlNode>();

                //filter out nested asp:* controls from the list (children are processed by the specific adapter converter method)
                //this way we don't end up double-calling a AdapterHost.ConvertTag on controls that have already been taken care of by their 
                //parent asp:* control converter
                foreach (var control in allchildren)
                {
                    var anc1 = control.Ancestors().Where(c => c.Name.Contains("asp:"));

                    if (!anc1.Any())
                    {
                        targetedChildren.Add(control);
                    }
                }
                foreach (var nodeObj in targetedChildren)
                {
                    var migratedControlText = await AdapterHost.ConvertTag(nodeObj.Name, nodeObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);
                    nodeObj.ParentNode.ReplaceChild(tempNode, nodeObj);
                }

                //var aspFormTags = rootNode.Descendants().Where(p => p.Name.ToLower().Contains("asp:"));//.ToList();
                //foreach (var control in aspFormTags)
                //{
                //    var migratedControlText = await AdapterHost.ConvertTag(control.Name, control.OuterHtml);

                //    var tempNode = HtmlNode.CreateNode(migratedControlText);

                //    control.ParentNode.ReplaceChild(tempNode, control);
                //}

                return rootNode.OuterHtml;

            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                model = null;
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

                newModel.LoadHtml($"<EditForm Model={editFormNode.GetAttributeValue("ItemType", "")} OnValidSubmit={editFormNode.GetAttributeValue("SelectMethod", "")}></EditForm>");
                var newNode = newModel.DocumentNode.SelectSingleNode("//editform");
                //this is now a live list having substituted the EditForm control for the old FormView one
                newNode.AppendChildren(editFormNode.ChildNodes);

                //deal with itemtemplates... ??

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var aspFormTags = newModel.DocumentNode.FirstChild.Descendants().Where(p => p.Name.ToLower().Contains("asp:")).ToList();
                foreach (var formObj in aspFormTags)
                {
                    var migratedControlText = await AdapterHost.ConvertTag(formObj.Name, formObj.OuterHtml);
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
            finally
            {
                currentModel = null;
                newModel = null;
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

                newModel.LoadHtml($"<ListView Model={listViewNode.GetAttributeValue("ItemType", "")} OnValidSubmit={listViewNode.GetAttributeValue("SelectMethod", "")}></ListView>");
                var newNode = newModel.DocumentNode.SelectSingleNode("//listview");
                //this is now a live list having substituted the EditForm control for the old FormView one
                newNode.AppendChildren(listViewNode.ChildNodes);

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var aspFormTags = newModel.DocumentNode.FirstChild.Descendants().Where(p => p.Name.ToLower().Contains("asp:")).ToList();
                foreach (var formObj in aspFormTags)
                {
                    var migratedControlText = await AdapterHost.ConvertTag(formObj.Name, formObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);

                    formObj.ParentNode.ReplaceChild(tempNode, formObj);
                }

                return newModel.DocumentNode.OuterHtml;
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                currentModel = null;
                newModel = null;
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
            var innerModel = new HtmlDocument();
            HtmlNode returnNode;

            try
            {
                model.LoadHtml(nodeContent);
                var contentControlObj = model.DocumentNode.FirstChild;
                returnNode = contentControlObj.CloneNode(false);
                innerModel.LoadHtml(contentControlObj.InnerHtml);  //<-- this is where we actually remove the asp:content control from the model

                //send any child asp:* controls to be converted by the a call back out to the adapterHosting class.
                var allchildren = innerModel.DocumentNode.Descendants().Where(p => p.Name.ToLower().Contains("asp:"));
                List<HtmlNode> targetedChildren = new List<HtmlNode>();

                //filter out nested asp:* controls from the list (children are processed by the specific adapter converter method)
                //this way we don't end up double-calling a AdapterHost.ConvertTag on controls that have already been taken care of by their 
                //parent asp:* control converter
                foreach (var control in allchildren)
                {
                    var anc1 = control.Ancestors().Where(c => c.Name.Contains("asp:"));

                    if (!anc1.Any())
                    {
                        targetedChildren.Add(control);
                    }
                }

                foreach (var nodeObj in targetedChildren)
                {
                    var migratedControlText = await AdapterHost.ConvertTag(nodeObj.Name, nodeObj.OuterHtml);
                    var tempNode = HtmlNode.CreateNode(migratedControlText);
                    nodeObj.ParentNode.ReplaceChild(tempNode, nodeObj);
                }

                return innerModel.DocumentNode.OuterHtml;

            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                model = null;
            }
        }
        #endregion
    }
}
