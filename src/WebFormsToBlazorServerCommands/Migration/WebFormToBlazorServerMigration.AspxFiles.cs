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
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp;

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

                //Getting all the aspx files in the project.
                var aspxFiles = webFormProjectData.Where(p => p.ModelType == VisualStudioModelType.Document && p.Name.EndsWith(".aspx")).Cast<VsDocument>();

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

                //Processing each aspx file.
                foreach (VsDocument aspxFile in aspxFiles)
                {
                    //Getting the formatted names that will be used in migrating the ASPX file and its code behind to the blazor project.
                    string targetFileNameNoExtension = Path.GetFileNameWithoutExtension(aspxFile.Path);
                    string aspxCodeBehindFileName = $"{targetFileNameNoExtension}.aspx.cs";
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

                    //Getting the code behind file that supports the current aspx page.
                    var aspxCodeBehindSource = webFormProjectData
                        .Where(m => m.ModelType == VisualStudioModelType.CSharpSource).Cast<VsCSharpSource>()
                        .FirstOrDefault(s => s.SourceCode.SourceDocument.ToLower().EndsWith(aspxCodeBehindFileName.ToLower())) as VsCSharpSource;

                    //Converting the aspx page and the code behind file if it was found.
                    await ConvertAspxPage(aspxFile, blazorServerProject, blazorPagesFolder, aspxCodeBehindSource);
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



        private static void ProcessSourceElement(Element elementToProcess, ref Element targetParentElement, ref HtmlParser htmlParser)
        {
            Element processedElement = null;
            var converterAdapter = new ConverterAdapter();
            converterAdapter.RegisterControlConverter(new AspxToBlazorControlConverter(converterAdapter));

            try
            {
                //If this is an ASP:* control then call the migration code, append the *entire* migrated node, and return to the calling method.
                if (elementToProcess.LocalName.ToLower().Contains("asp:"))
                {
                    var newNodeText = Task.Run(() => converterAdapter.MigrateTagControl(elementToProcess.LocalName, elementToProcess.OuterHtml)).Result;
                    var convertedNodeObj = htmlParser.ParseFragment(newNodeText, null);

                    //Need to remove the HTML/HEAD/BODY tags that get added by the parser
                    var NodeToAppend = convertedNodeObj.GetElementsByTagName("BODY").First().FirstElementChild;

                    //Append the element to the targetDocumentFragment, or the lastAppendedElement depending
                    processedElement = targetParentElement.AppendElement(NodeToAppend) as Element;

                    //We do *not* deal with any children of this element, as that is the responsibility of the MigratTagControl to deal with any children of the ASP control
                    return;

                }
                else
                {
                    //if the current element has children,
                    // - add it to the targetDocumentFragment without the children attached
                    // - recursively call this method to deal with the children, passing in the new appended as the parent element to append children too
                    if (elementToProcess.ChildElementCount > 0)
                    {
                        //Make sure that we are not doubling up the HTML and/or the BODY element - these get added by default from AngleSharp even to fragments.
                        if (!elementToProcess.NodeName.ToLower().Equals(targetParentElement.NodeName.ToLower()))
                        {
                            processedElement = targetParentElement.AppendElement(elementToProcess.Clone(false) as Element);
                        }
                        foreach (Element item in elementToProcess.Children)
                        {
                            ProcessSourceElement(item, ref processedElement, ref htmlParser);
                        }

                    } else
                    {
                        targetParentElement.Append(elementToProcess.Clone(false));
                    }

                    return;

                }
            }
            catch (Exception)
            {

                throw;
            }

        }

    }
}
