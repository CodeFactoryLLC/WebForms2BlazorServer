using CodeFactory.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CodeFactory.DotNet.CSharp;
using Newtonsoft.Json;
using WebFormsToBlazorServerCommands.Templates;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Code Automation process that migrates code, files, and configuration data to a server side blazor project. 
    /// </summary>
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// Field that holds the CodeFactory visual studio actions that can be used for automation.
        /// </summary>
        private readonly IVsActions _visualStudioActions;

        /// <summary>
        /// Holds the dialog that is to be updated, using the exposed interface to update the dialog.
        /// </summary>
        private readonly IMigrationStatusUpdate _statusTracking;

        /// <summary>
        /// Initializes the migration class and creates a new instance.
        /// </summary>
        /// <param name="vsActions">The CodeFactory automation actions that will be used in the migration process.</param>
        /// <param name="statusUpdate">The dialog that will be updated during the migration process.</param>
        public WebFormToBlazorServerMigration(IVsActions vsActions, IMigrationStatusUpdate statusUpdate)
        {
            _visualStudioActions = vsActions;
            _statusTracking = statusUpdate;
        }

        /// <summary>
        /// Starts the migration process from webforms project to a blazor server project.
        /// </summary>
        /// <param name="webFormsProject">Source project to read data from.</param>
        /// <param name="blazorServerProject">Target server blazor project to inject into.</param>
        /// <param name="steps">The migration steps that are to be run.</param>
        public async Task StartMigration(VsProject webFormsProject, VsProject blazorServerProject, MigrationSteps steps)
        {
            try
            {
                //bounds checking that migration steps were provided.
                if (steps == null)
                {
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.MigrationProcess,
                        MigrationStatusEnum.Failed);
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess,
                        MessageTypeEnum.Error,
                        "Could not determine which migration steps are to be perform migration aborted.");
                    await _statusTracking.UpdateMigrationFinishedAsync();
                    return;
                }

                //Bounds checking that the web forms project was provided.
                if (webFormsProject == null)
                {
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.MigrationProcess,
                        MigrationStatusEnum.Failed);
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess,
                        MessageTypeEnum.Error,
                        "Internal error occured, no web forms project was provided the migration was aborted.");
                    await _statusTracking.UpdateMigrationFinishedAsync();
                    return;
                }

                //Bounds checking if the blazor server project was provided.
                if (blazorServerProject == null)
                {
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.MigrationProcess,
                        MigrationStatusEnum.Failed);
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess,
                        MessageTypeEnum.Error,
                        "Internal error occured, no blazor server project was provided the migration was aborted.");
                    await _statusTracking.UpdateMigrationFinishedAsync();
                    return;
                }

                //Starting the migration process by caching the web forms project data
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.MigrationProcess,
                    MigrationStatusEnum.Running);
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess,
                    MessageTypeEnum.Information, "Please wait loading the data from the web forms project...");

                //Loading the visual studio models from the webFormsProject.
                //This is a resource intensive task we only need to do this once since we are never updating the web forms project.
                //We will cache this data and pass it on to all parts of the migration process
                var webFormProjectData = await webFormsProject.LoadAllProjectData();

                //Confirming the web forms data has been cached
                if (webFormProjectData.Any())
                {
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.MigrationProcess,
                        MigrationStatusEnum.Passed);
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess,
                        MessageTypeEnum.Information, "Web forms data has been cached, migration beginning.");
                }
                else
                {
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.MigrationProcess,
                        MigrationStatusEnum.Failed);
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess,
                        MessageTypeEnum.Error,
                        "Failed to load the web forms project data, cannot continue the migration process.");
                    await _statusTracking.UpdateMigrationFinishedAsync();
                    return;
                }

                //Running the migration steps in sequential order
                if (steps.Startup) await MigrateStartupAsync(webFormProjectData, webFormsProject, blazorServerProject);
                if (steps.HttpModules) await MigrateHttpModulesAsync(webFormProjectData, webFormsProject, blazorServerProject);
                if (steps.StaticFiles) await MigrateStaticFiles(webFormProjectData, webFormsProject, blazorServerProject);
                if (steps.Bundling) await MigrateBundling(webFormProjectData, webFormsProject, blazorServerProject);
                if (steps.AspxPages) await MigrateAspxFiles(webFormProjectData, webFormsProject, blazorServerProject);
                if (steps.Configuration) await MigrateConfig(webFormProjectData, webFormsProject, blazorServerProject);
                if (steps.AppLogic) await MigrateLogic(webFormProjectData, webFormsProject, blazorServerProject);

            }
            catch (Exception unhandledError)
            {
                //Updating the dialog with the unhandled error that occured.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.MigrationProcess, MessageTypeEnum.Error,
                    $"The migration process had an unhandled error, the migration process is aborting. The following error occured. '{unhandledError.Message}'");

            }
            finally
            {
                //Informing the hosting dialog the migration has finished.
                await _statusTracking.UpdateMigrationFinishedAsync();
            }
        }

        #region Migration Steps

        /// <summary>
        /// Helper method that converts Aspx Pages to Blazor pages.
        /// </summary>
        /// <param name="sourcePage">The source aspx page to be converted.</param>
        /// <param name="targetProject">The target blazor project to write to.</param>
        /// <param name="targetPagesFolder">The target visual studio project folder the converted aspx will be added to.</param>
        /// <param name="sourceCodeBehind">Optional parameter that provides the code behind file for the aspx page to be converted also.</param>
        /// <returns>Flag used to determine if the conversion was successful.</returns>
        private async Task ConvertAspxPage(VsDocument sourcePage, VsProject targetProject, VsProjectFolder targetPagesFolder, VsCSharpSource sourceCodeBehind = null, VsDocument sourceDocCodeBehind = null)
        {
            try
            {
                //Getting the content from the source document.
                var pageContent = await sourcePage.GetDocumentContentAsStringAsync(); //File.ReadAllText(result.Path);

                //grab the <%Page element from the source and pull it from the text.  Its meta data anyway and just screws up the conversion down the line.
                var pageHeaderData = System.Text.RegularExpressions.Regex.Match(pageContent, @"<%@\s*[^%>]*(.*?)\s*%>").Value;

                if (pageHeaderData.Length > 0)
                {
                    pageContent = Regex.Replace(pageContent, @"<%@\s*[^%>]*(.*?)\s*%>", string.Empty);
                }

                //Swap ASP.NET string tokens for Razor syntax  (<%, <%@, <%:, <%#:, etc
                var targetText = RemoveASPNETSyntax(pageContent);

                //Convert ASP.NET into Razor syntax.  **This actually presumes that any controls like <asp:ListView..
                //will have an equivalent Razor component in place called <ListView.. etc
                var conversionData = await ReplaceAspControls(targetText);

                //Drop the pageHeaderData into the Dictionary for later processing by any downstream T4 factories
                conversionData.Add("HeaderData", pageHeaderData);
                //just some status text for the dialog ( need to convert this to events )

                //Getting the source code from the code behind file provided.
                var codeSource = sourceCodeBehind?.SourceCode;
                if ((codeSource == null) && (sourceDocCodeBehind != null))
                {
                    codeSource = await sourceDocCodeBehind.GetCSharpSourceModelAsync();
                }

                //put the files in the target project
                String targetFileName = Path.GetFileNameWithoutExtension(sourcePage.Path);
                conversionData.Add("Namespace", $"{targetProject.DefaultNamespace}.Pages");

                //Setup Page directives, using statements etc
                var targetWithMeta = await SetRazorPageDirectives(targetFileName, conversionData);

                //Adding the converted content from the aspx page to the new razor page.
                VsDocument success = await targetPagesFolder.AddDocumentAsync($"{targetFileName}.razor", targetWithMeta);

                if (success != null)
                {
                    //Updating the dialog with the status the aspx page has been converted.
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                        MessageTypeEnum.Information,
                        $"Converted the aspx page to a razor page, added razor page {targetFileName}.razor");

                    //If we have the source code from the original aspx page add it to the razor pages folder.
                    if (codeSource != null)
                    {
                        //Creating a CodeFactory model store this will be used to pass data to a T4 factory.
                        CsModelStore modelStore = new CsModelStore();

                        //Adding the current class from the code behind into the model store for processing.
                        modelStore.SetModel(codeSource.Classes.FirstOrDefault());
                        
                        //Processing the T4 factory and loading the source code.
                        var codeBehindFormattedSourceCode =
                            Templates.PageCodeBehind.GenerateSource(modelStore, conversionData);

                        //Calling the CodeFactory project system and adding a new code behind file and injecting the formatted code into the file.
                        var codeBehind = await targetPagesFolder.AddDocumentAsync($"{targetFileName}.razor.cs", codeBehindFormattedSourceCode);

                        if (codeBehind != null)
                        {
                            //Updating the dialog with the status the aspx page code behind has been converted.
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                MessageTypeEnum.Information,
                                $"Converted the aspx page code behind file to a razor page code behind file, added code behind file {targetFileName}.razor.cs");
                        }
                        else
                        {
                            //Updating the dialog with the status the aspx page code behind failed
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                MessageTypeEnum.Error,
                                $"Failed the conversion of the aspx page code behind file to a razor page code behind file {targetFileName}.razor.cs");
                        }
                    }
                }
                else
                {
                    //Updating the dialog with the status the aspx page failed conversion.
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                        MessageTypeEnum.Error,
                        $"Failed the conversion of the aspx page {targetFileName}.razor. Will not convert the code behind file.");
                }
            }
            catch (Exception unhandledError)
            {
                 await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error,
                    $"The following unhandled error occured while trying to convert the aspx page. '{unhandledError.Message}'");
            }
        }

        /// <summary>
        /// Migrates the site.master layout files from the source WebForms project into the \Shared folder
        /// in the Blazor target application project.
        /// </summary>
        /// <param name="webFormSiteMasterFiles">The flattened list of WebForms project objects/files</param>
        /// <param name="blazorServerProject">The target Blazor project object</param>
        /// <returns></returns>
        ///         private async Task ConvertAspxPage(VsDocument sourcePage, VsProject targetProject, VsProjectFolder targetPagesFolder, VsCSharpSource sourceCodeBehind = null)
        private async Task<string> ConvertLayoutFiles(IEnumerable<VsModel> webFormSiteMasterFiles, VsProject blazorServerProject)
        {
            string headerData = null;
            //put the files in the target project
            var targetFolder = await blazorServerProject.CheckAddFolder("Shared");

            try
            {
                foreach (var layoutFile in webFormSiteMasterFiles)
                {
                    //We don't want to touch/migrate any of the *.designer files
                    if (layoutFile.Name.ToLower().Contains("designer")) continue;

                    String targetFileName = layoutFile.Name.Replace(".", "");
                    //Get any existing children in the targetFolder that match.
                    var existingBlazorMatches = await targetFolder.GetChildrenAsync(true, false);
                    var docMatches = existingBlazorMatches.Where(p => p.Name.ToLower().Contains(targetFileName.ToLower())).Cast<VsDocument>();

                    foreach (VsDocument matchFile in docMatches)
                    {
                        //delete each matched file in the target folder.
                        await matchFile.DeleteAsync();
                    }

                    //work on just the Site.Master file which is basically a specialized *.aspx file.
                    if (!layoutFile.Name.ToLower().Contains(".cs"))
                    {
                        var docObj = layoutFile as VsDocument;
                        var textFromResult = await docObj.GetDocumentContentAsStringAsync();

                        //grab the <%Page element from the source and pull it from the text.  Its meta data anyway and just screws up the conversion down the line.
                        var pageHeaderData = System.Text.RegularExpressions.Regex.Match(textFromResult, @"<%@\s*[^%>]*(.*?)\s*%>").Value;

                        if (pageHeaderData.Length > 0)
                        {
                            textFromResult = Regex.Replace(textFromResult, @"<%@\s*[^%>]*(.*?)\s*%>", string.Empty);
                        }

                        //Swap ASP.NET string tokens for Razor syntax  (<%, <%@, <%:, <%#:, etc
                        var targetText = RemoveASPNETSyntax(textFromResult);

                        //Convert Site.Master file into razor syntax, specifically the asp:controls that might be used there.
                        var conversionData = await ReplaceAspControls(targetText);
                        //Drop the pageHeaderData into the Dictionary for later processing by any downstream T4 factories
                        conversionData.Add("HeaderData", pageHeaderData);

                        //Setup Page directives, using statements etc
                        var targetWithMeta = await SetRazorPageDirectives(targetFileName, conversionData);


                        VsDocument success = await _visualStudioActions.ProjectFolderActions.AddDocumentAsync(targetFolder, $"{targetFileName}.razor", targetWithMeta);
                        //Updating the dialog
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Information,
                         $"The file {targetFileName}.razor was copied to {targetFolder.Name}.");

                    }

                    //Get sibling/named code-behind file
                    //1. Get the children of this *.aspx file
                    //2. Grab the doc that has the same name with a ".cs" extension on the end of it
                    if (layoutFile.Name.Contains(".cs"))
                    {
                        targetFileName = layoutFile.Name.Replace(".cs", "");
                        targetFileName = targetFileName.Replace(".", "");

                        var sourceObj = layoutFile as VsCSharpSource;
                        var codeSource = sourceObj.SourceCode;
                        var metaDataDictionary = new Dictionary<string, string>();
                        metaDataDictionary.Add("Namespace", $"{blazorServerProject.Name}.Pages");

                        //Setup Page directives, using statements etc
                        //var targetWithMeta = await SetRazorPageDirectives(targetFileName, conversionData);
                        CsModelStore modelStore = new CsModelStore();
                        modelStore.SetModel(codeSource.Classes.FirstOrDefault());
                        var codebehind = await _visualStudioActions.ProjectFolderActions.AddDocumentAsync(targetFolder,
                            $"{targetFileName}.razor.cs",
                            Templates.PageCodeBehind.GenerateSource(modelStore, metaDataDictionary));

                        // Updating the dialog
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Information,
                        $"The file {targetFileName}.razor.cs was copied to {targetFolder.Name}.");
                    }
                }

            }

            catch (Exception unhandledError)
            {
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error,
                    $"The following unhandled error occured while trying to migrate the layout files. '{unhandledError.Message}'");
            }
            return headerData;
        }



        /// <summary>
        /// Swap ASP.NET string tokens for Razor syntax  (<%, <%@, <%:, <%#:, etc
        /// </summary>
        /// <param name="sourceMarkup">The source markup to replace</param>
        /// <returns>The updated markup</returns>
        string RemoveASPNETSyntax(string sourceMarkup)
        {
            String result = sourceMarkup;
            var tokenMatches = Regex.Matches(sourceMarkup, @"<%\s*[^%>]*(.*?)\s*%>");

            try
            {
                String subMatch = string.Empty;
                foreach (Match matchObj in tokenMatches)
                {
                    subMatch = string.Empty;
                    switch (matchObj.Value.Substring(2, 2))
                    {
                        case ": ":
                            subMatch = matchObj.Value.Replace("<%:", "@(");
                            subMatch = subMatch.Replace("%>", ")");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        case "= ":
                            subMatch = matchObj.Value.Replace("<%=", "@(");
                            subMatch = subMatch.Replace("%>", ")");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        case "# ":
                            subMatch = matchObj.Value.Replace("<%#", "@(");
                            subMatch = subMatch.Replace("%>", ")");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        case "$ ":
                            subMatch = matchObj.Value.Replace("<%$", "@(");
                            subMatch = subMatch.Replace("%>", ")");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        case "#:":
                            subMatch = matchObj.Value.Replace("<%#:", "@(");
                            subMatch = subMatch.Replace("%>", ")");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        case "--":
                            subMatch = matchObj.Value.Replace("<%--", "@*");
                            subMatch = subMatch.Replace("--%>", "*@");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        case "\r\n":
                            subMatch = matchObj.Value.Replace("<%\r\n", "@{\r\n");
                            subMatch = subMatch.Replace("\r\n%>", "\r\n}");
                            result = result.Replace(matchObj.Value, subMatch);
                            break;

                        default:
                            if (matchObj.Value.Substring(2, 1).Contains("="))
                            {
                                subMatch = matchObj.Value.Replace("<%=", "@(");
                                subMatch = subMatch.Replace("%>", ")");
                                result = result.Replace(matchObj.Value, subMatch);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Used to cycle through a set of ASP source code and swap out any of the old-style
        /// asp:* style controls for either there newer razor equivalents, or simplem HTML 5 compliant 
        /// tags where there is no equivalent razor match.
        /// </summary>
        /// <param name="sourceAspCode"></param>
        /// <returns>string with replaced asp:* controls</returns>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private async Task<Dictionary<string, string>> ReplaceAspControls(string sourceAspCode)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //set anglesharp
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var scratchSource = sourceAspCode;
            var model = await context.OpenAsync(req => req.Content(sourceAspCode));
            var parser = context.GetService<HtmlParser>();

            var aspContentTags = model.All.Where(p => p.LocalName.ToLower().Equals("asp:content"));
            if (aspContentTags.Any())
            {
                result.Add("ContentPlaceHolderID", aspContentTags.First().Attributes.Select(p => p.Name.Equals("ContentPlaceHolderID")).First().ToString());
                //There should only be a single asp:Content tag per aspx page
            }

            //Deal with asp:FormView tag
            var aspFormTags = model.All.Where(p => p.LocalName.ToLower().Equals("asp:formview")).ToList();
            foreach (var formObj in aspFormTags)
            {
                var newNode = parser.ParseFragment($"<EditForm Model={formObj.GetAttribute("ItemType")} OnValidSubmit={formObj.GetAttribute("SelectMethod")}> </EditForm>", formObj);
                //check for ItemTemplate tag and remove it.  There isn't one in Blazor/Razor
                newNode.FirstOrDefault().AppendNodes(
                    formObj.Children.Any(p => p.TagName.ToLower().Equals("itemtemplate"))
                        ? formObj.Children.First(c => c.TagName.ToLower().Equals("itemtemplate")).Children.ToArray()
                        : formObj.ChildNodes.ToArray());
                formObj.Replace(newNode.ToArray());

                result.Add("ItemType", formObj.GetAttribute("ItemType"));
                result.Add("SelectMethod", formObj.GetAttribute("SelectMethod"));

                scratchSource = model.All.First(p => p.LocalName.ToLower().Equals("body")).InnerHtml;
            }

            //Look for any tags that have 'asp:' in them -- then drop the 'asp:' from it.
            var aspHelperTags = model.All.Where(p => p.LocalName.ToLower().Contains("asp:")).ToList();
            bool hasContentTag = false;
            foreach (var tagObj in aspHelperTags)
            {
                if (tagObj.LocalName.ToLower().Contains("asp:content "))
                {
                    hasContentTag = true;
                    continue;
                }

                //Removing the asp: tag from the html
                var cleanedHtml = Regex.Replace(tagObj.OuterHtml, "asp:", "", RegexOptions.IgnoreCase);

                //Having the cleanHtml reloaded into the parser.
                var replacementTagNode = parser.ParseFragment(cleanedHtml, tagObj);

                //Injecting the cleaned up parsed content back into the target tag in the dom. 
                tagObj.Replace(replacementTagNode.ToArray());

                //var replacementTagNode =
                //        parser.ParseFragment(tagObj.OuterHtml.Replace("asp:", ""), tagObj);
                //tagObj.Replace(replacementTagNode.ToArray());
            }

            scratchSource = hasContentTag
                    ? model.All.First(p => p.LocalName.ToLower().Equals("asp:content")).InnerHtml
                    : model.All.First(p => p.LocalName.ToLower().Equals("body")).InnerHtml;

            result.Add("source", sourceAspCode);
            result.Add("alteredSource", scratchSource);

            return result;
        }

        /// <summary>
        /// Updates the directives in the page file to use razor syntax.
        /// </summary>
        /// <param name="fileName">The file being processed.</param>
        /// <param name="sourceData">The source data to be updated.</param>
        /// <returns>The updated content.</returns>
        private async Task<String> SetRazorPageDirectives(string fileName, Dictionary<string, string> sourceData)
        {
            String result = String.Empty;

            try
            {
                var pageData = sourceData["HeaderData"];
                Regex regex = new Regex(@"(?<=\bMasterPageFile="")[^""]*");
                Match match = regex.Match(pageData);
                string layout = match.Value;
                layout = layout.Replace(".", ""); //remove the old-style Site.Master layout to read SiteMaster
                regex = new Regex(@"(?<=\bInherits="")[^""]*");
                match = regex.Match(pageData);
                string inherits =
                result = $"@page \"/{fileName}\"\r\n";
                if (layout.Length > 0)
                {
                    //Making sure the ~ gets removed from directives
                    result += $"@layout { layout.Replace("~/", "")}\r\n";
                }
                result += $"@inherits {fileName}Base\r\n\r\n {sourceData["alteredSource"]}";
            }
            catch (Exception unhandledError)
            {
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error,
                    $"The following unhandled error occured while setting the razor page directives in the file {fileName}. '{unhandledError.Message}'");
            }

            return result;
        }

        #endregion
    }
}
