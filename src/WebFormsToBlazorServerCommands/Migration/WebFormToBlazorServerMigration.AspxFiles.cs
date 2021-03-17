using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.VisualStudio;
using CodeFactory.Formatting;
using CodeFactory.DotNet.CSharp;
using CodeFactory.Formatting.CSharp;
using CodeFactory.SourceCode;

using System.ComponentModel.Design;
using HtmlAgilityPack;
using CodeFactory.Markup.Adapter;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {


        /// <summary>
        /// Migrates the existing aspx page files to a standard blazor page format.
        /// </summary>
        /// <param name="webFormProjectData">Pre cached project data about the web forms project.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public async Task MigrateAspxFiles(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Informing the dialog the migration step has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AspxPages, MigrationStatusEnum.Running);

                //Getting all the aspx & ascx files in the project.
                var aspxFiles = webFormProjectData.Where(p => p.ModelType == VisualStudioModelType.Document && ( p.Name.EndsWith(".aspx") || p.Name.EndsWith(".ascx"))).Cast<VsDocument>();

                if (!aspxFiles.Any())
                {
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Warning,
                        "No Aspx files were found in the web forms project. This step is finished.");
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AspxPages,
                        MigrationStatusEnum.Passed);
                    return;
                }

                //Migrate over the site.master layout pages.
                var layoutFiles = webFormProjectData.Where(p => p.Name.ToLower().Contains(".master"));
                var success = await ConvertLayoutFiles(layoutFiles, blazorServerProject);

                //Calling into the CodeFactory project system and getting all the direct children of the project.
                var blazorRootModels = await blazorServerProject.GetChildrenAsync(false);

                //Getting the pages folder from the blazor project.
                var blazorPagesFolder = blazorRootModels.FirstOrDefault(m =>
                    m.ModelType == VisualStudioModelType.ProjectFolder & m.Name.ToLower().Equals("pages")) as VsProjectFolder;

                //If the pages folder was not found fail this step and return.
                if (blazorPagesFolder == null)
                {
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error,
                        "No pages folder was found in the blazor project, cannot continue the aspx file conversion.");
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AspxPages,
                        MigrationStatusEnum.Failed);
                    return;
                }

                //Call the CodeFactory project system to get all the current children of the pages project folder.
                var pagesFolderModels = await blazorPagesFolder.GetChildrenAsync(true);

                //Filtering out everything but documents.
                var pages = pagesFolderModels.Where(m => m.ModelType == VisualStudioModelType.Document)
                    .Cast<VsDocument>();

                int collect = 0;

                //Processing each aspx file.
                foreach (VsDocument aspxFile in aspxFiles)
                {
                    collect++;

                    //Getting the formatted names that will be used in migrating the ASPX file and its code behind to the blazor project.
                    string targetFileNameNoExtension = Path.GetFileNameWithoutExtension(aspxFile.Path);
                    string aspxCodeBehindFileName = $"{targetFileNameNoExtension}.aspx.cs";
                    string ascxCodeBehindFileName = $"{targetFileNameNoExtension}.axcs.cs";
                    string razorPageFileName = $"{targetFileNameNoExtension}.razor";
                    string razorPageCodeBehindFileName = $"{targetFileNameNoExtension}.razor.cs";

                    //Searching for an existing razor page. We will delete razor pages and recreate them.
                    var currentRazorPage = pages.FirstOrDefault(p => p.Path.ToLower().EndsWith(razorPageFileName.ToLower()));

                    if (currentRazorPage != null)
                    {
                        //Razor page was found removing the razor page.
                        bool removedPage = await currentRazorPage.DeleteAsync();

                        if (removedPage)
                        {
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                MessageTypeEnum.Information, $"Removed the razor page {razorPageFileName}");

                            var currentRazorPageCodeBehind = pages.FirstOrDefault(p => p.Path.ToLower().EndsWith(razorPageCodeBehindFileName.ToLower()));

                            if (currentRazorPageCodeBehind != null)
                            {
                                if (File.Exists(currentRazorPageCodeBehind.Path))
                                {
                                    bool removedCodeBehind = await currentRazorPageCodeBehind.DeleteAsync();

                                    if (removedCodeBehind)
                                    {
                                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                            MessageTypeEnum.Information,
                                            $"Removed the razor page code behind file {razorPageCodeBehindFileName}");

                                    }
                                    else
                                    {
                                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                            MessageTypeEnum.Error,
                                            $"Could not remove the razor page code behind file {razorPageCodeBehindFileName}.The target ASPX file will not be migrated.");
                                        continue;
                                    }
                                }
                            }
                        }
                        else
                        {
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                MessageTypeEnum.Error,
                                $"Could not remove the razor page {razorPageFileName}.The target ASPX file will not be migrated.");
                            continue;
                        }
                    }

                    VsCSharpSource CodeBehindSource = null;
                    if (aspxFile.Path.Contains("ascx"))
                    {
                        //Getting the code behind file that supports the current aspx page.
                        CodeBehindSource = webFormProjectData
                            .Where(m => m.ModelType == VisualStudioModelType.CSharpSource).Cast<VsCSharpSource>()
                            .FirstOrDefault(s => s.SourceCode.SourceDocument.ToLower().EndsWith(ascxCodeBehindFileName.ToLower())) as VsCSharpSource;
                    }

                    //Getting the code behind file that supports the current aspx page.
                    CodeBehindSource = webFormProjectData
                        .Where(m => m.ModelType == VisualStudioModelType.CSharpSource).Cast<VsCSharpSource>()
                        .FirstOrDefault(s => s.SourceCode.SourceDocument.ToLower().EndsWith(aspxCodeBehindFileName.ToLower())) as VsCSharpSource;

                    //Converting the aspx page and the code behind file if it was found.
                    await ConvertAspxPage(aspxFile, blazorServerProject, blazorPagesFolder, CodeBehindSource);

                    if (collect == 4)
                    {
                        GC.Collect();
                        collect = 0;
                    }
                }

                //Completed the migration step informing the dialog.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AspxPages, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AspxPages, MigrationStatusEnum.Failed);
            }
        }

        public async Task MigrateSingleASPXFile(VsDocument aspxSourcefile, VsProject blazorServerProject)
        {
            try
            {
                

                
                //VsCSharpSource aspxCodeBehindFile
                //Getting the formatted names that will be used in migrating the ASPX file and its code behind to the blazor project.
                string targetFileNameNoExtension = Path.GetFileNameWithoutExtension(aspxSourcefile.Path);
                string aspxCodeBehindFileName = $"{targetFileNameNoExtension}.aspx.cs";
                string razorPageFileName = $"{targetFileNameNoExtension}.razor";
                string razorPageCodeBehindFileName = $"{targetFileNameNoExtension}.razor.cs";

                //Calling into the CodeFactory project system and getting all the direct children of the project.
                var blazorRootModels = await blazorServerProject.GetChildrenAsync(false);

                //Getting the pages folder from the blazor project.
                var blazorPagesFolder = blazorRootModels.FirstOrDefault(m =>
                    m.ModelType == VisualStudioModelType.ProjectFolder & m.Name.ToLower().Equals("pages")) as VsProjectFolder;

                //If the pages folder was not found fail this step and return.
                if (blazorPagesFolder == null)
                {
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error,
                        "No pages folder was found in the blazor project, cannot continue the aspx file conversion.");
                    return;
                }

                //Call the CodeFactory project system to get all the current children of the pages project folder.
                var pagesFolderModels = await blazorPagesFolder.GetChildrenAsync(true);

                //Filtering out everything but documents.
                var pages = pagesFolderModels.Where(m => m.ModelType == VisualStudioModelType.Document)
                    .Cast<VsDocument>();

                //Searching for an existing razor page. We will delete razor pages and recreate them.
                var currentRazorPage = pages.FirstOrDefault(p => p.Path.ToLower().EndsWith(razorPageFileName.ToLower()));

                if (currentRazorPage != null)
                {
                    //Razor page was found removing the razor page.
                    bool removedPage = await currentRazorPage.DeleteAsync();

                    if (removedPage)
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                            MessageTypeEnum.Information, $"Removed the razor page {razorPageFileName}");

                        var currentRazorPageCodeBehind = pages.FirstOrDefault(p => p.Path.ToLower().EndsWith(razorPageCodeBehindFileName.ToLower()));

                        if (currentRazorPageCodeBehind != null)
                        {
                            if (File.Exists(currentRazorPageCodeBehind.Path))
                            {
                                bool removedCodeBehind = await currentRazorPageCodeBehind.DeleteAsync();

                                if (removedCodeBehind)
                                {
                                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                        MessageTypeEnum.Information,
                                        $"Removed the razor page code behind file {razorPageCodeBehindFileName}");

                                }
                                else
                                {
                                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                                        MessageTypeEnum.Error,
                                        $"Could not remove the razor page code behind file {razorPageCodeBehindFileName}.The target ASPX file will not be migrated.");
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages,
                            MessageTypeEnum.Error,
                            $"Could not remove the razor page {razorPageFileName}.The target ASPX file will not be migrated.");
                        return;
                    }
                }

                var aspxChildren = await aspxSourcefile.GetChildrenAsync(true);
                var codeBehindFile = aspxChildren
                    .Where(c => (c.IsSourceCode == true) && (c.SourceType == SourceCodeType.CSharp)).FirstOrDefault();

                //Converting the aspx page and the code behind file if it was found.
                await ConvertAspxPage(aspxSourcefile, blazorServerProject, blazorPagesFolder, null, codeBehindFile);

                await _statusTracking.UpdateMigrationFinishedAsync();
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AspxPages, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");
            }
        }


        /// <summary>
        /// This method is used to send an Element through any registered ControlConverter adapters and get back 
        /// the migrated text from the AdapterHost.
        /// </summary>
        /// <param name="elementToProcess"></param>
        /// <returns>String</returns>
        private async Task<string> ProcessSourceElement(string elementToProcess, AdapterHost host)
        {
            HtmlNode processedElement = null;
            StringBuilder processedHTML = new StringBuilder();
            var htmlParser = new HtmlDocument();
            htmlParser.LoadHtml(elementToProcess);
            var contentControlObj = htmlParser.DocumentNode.FirstChild;
            
            try
            {
                //If this is an ASP:* control then call the migration code, append the *entire* migrated node, and return to the calling method.
                if (contentControlObj.Name.ToLower().Contains("asp:"))
                {
                    var newNodeText = await host.ConvertTag(contentControlObj.Name, contentControlObj.OuterHtml);

                    //We do *not* deal with any children of this element, as that is the responsibility of the MigratTagControl to deal with any children of the ASP control
                    return newNodeText;
                }
                else
                {
                    //if the current element has children,
                    // - add it to the targetDocumentFragment without the children attached
                    // - recursively call this method to deal with the children, passing in the new appended as the parent element to append children too
                    if (contentControlObj.ChildNodes.Count > 0)
                    {
                        //shallow clone
                        processedElement = contentControlObj.CloneNode(false);
                        
                        foreach (var item in contentControlObj.ChildNodes)
                        {
                            if (item.NodeType == HtmlNodeType.Element)
                            {
                                var migratedValue = await ProcessSourceElement(item.OuterHtml, host);
                                var migratedChild = HtmlNode.CreateNode(migratedValue.Length > 0 ? migratedValue : " ");
                                processedElement.AppendChild(migratedChild);
                            }
                        }
                        processedHTML.Append(processedElement.OuterHtml);
                    } else
                    {
                        processedHTML.Append((contentControlObj.Clone()).OuterHtml);
                    }
                    
                    return processedHTML.ToString();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            } 
            finally
            {
                htmlParser = null;
            }
        }

    }
}
