using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CodeFactory.VisualStudio;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// Migrates logic class files over to the blazor server project.
        /// </summary>
        /// <param name="webFormProjectData">List of pre cached models for from the web form project.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        public async Task MigrateLogic(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Informing the dialog the migration step has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AppLogic, MigrationStatusEnum.Running);

                var childFiles = webFormProjectData.GetSourceCodeDocumentsAsync(true);

                //we don't want any known aspx/ascx files hitching a ride.  just plain vanilla *.cs files should qualify.
                var logicFiles = childFiles.Where(p => (!p.Name.ToLower().Contains("aspx.") && !p.Name.ToLower().Contains("ascx.") && !p.Name.ToLower().Contains("asax."))).ToList();

                //put logic files (using the file system into the target under the project root)
                foreach (VsCSharpSource sourceDocument in logicFiles)
                {
                    //look for specific files that are native to a WebForm app and skip them. ** TODO: move this to a config setting maybe?
                    if (sourceDocument.Name.ToLower().Contains("bundleconfig")) continue;
                    if (sourceDocument.Name.ToLower().Contains("assemblyinfo")) continue;
                    if (sourceDocument.Name.ToLower().Contains("startup")) continue;
                    if (sourceDocument.Name.ToLower().Contains(".master")) continue;

                    var logicDocument = await sourceDocument.LoadDocumentModelAsync();

                    var parentFolders = await logicDocument.GetParentFolders();

                    var source = sourceDocument.SourceCode;
                    var docText = await logicDocument.GetDocumentContentAsStringAsync();

                    if (parentFolders.Count >= 1)
                    {
                        parentFolders.Reverse(); //The folders are returned in leaf-to-trunk so need to reverse the order for the next step.
                        VsProjectFolder createdFolder = null;

                        //deal with source folder hierarchy
                        for (int i = 0; i < parentFolders.Count; i++)
                        {
                            if (i > 0)
                            {
                                createdFolder = await createdFolder.CheckAddFolder(parentFolders[i].Name);
                            }
                            else
                                createdFolder = await blazorServerProject.CheckAddFolder(parentFolders[i].Name);
                        }

                        //copy the file.  We only really care about the most leaf/edge subfolder so its safe to use the creatdFolder variable here.
                        docText = docText.Replace(source.Classes.First().Namespace, $"{blazorServerProject.Name}.{createdFolder.Name}");

                        var targetFolderFiles = await createdFolder.GetChildrenAsync(false);

                        if (!targetFolderFiles.Any(c => c.Name.ToLower().Equals(logicDocument.Name.ToLower()) ) )
                        {
                            await createdFolder.AddDocumentAsync(logicDocument.Name, docText);
                            //Updating the dialog with a status
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AppLogic, MessageTypeEnum.Information,
                                $"Copied logic file: {logicDocument.Name} to project {blazorServerProject.Name} location: {Path.Combine(createdFolder.Path, logicDocument.Name)}");
                        } else
                        {
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AppLogic, MessageTypeEnum.Warning,
                                    $"Logic file: {logicDocument.Name} already exists in target folder location: {Path.Combine(createdFolder.Path, logicDocument.Name)} and was skipped.");
                        }

                    }
                    else
                    {
                        var projFiles = await blazorServerProject.GetChildrenAsync(false);

                        if (!projFiles.Any(c => c.Name.ToLower().Equals(logicDocument.Name.ToLower()) ) ) {

                            docText = docText.Replace(source.Classes.First().Namespace, $"{blazorServerProject.Name}");
                            
                            var thing = await blazorServerProject.AddDocumentAsync(logicDocument.Name, docText);
                            
                            //Updating the dialog with a status
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AppLogic, MessageTypeEnum.Information,
                                $"Copied static file: {logicDocument.Name} to project {blazorServerProject.Name} location: {Path.Combine(blazorServerProject.Path, logicDocument.Name)}");
                        }
                        else
                        {
                            //Updating the dialog with a status
                            await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AppLogic, MessageTypeEnum.Warning,
                                $"Static file: {logicDocument.Name} already exists in project {blazorServerProject.Name} location: {Path.Combine(blazorServerProject.Path, logicDocument.Name)} and was skipped.");
                        }
                    }
                }

                //Completed the migration step informing the dialog.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AppLogic, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.AppLogic, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.AppLogic, MigrationStatusEnum.Failed);
            }
        }
    }
}
