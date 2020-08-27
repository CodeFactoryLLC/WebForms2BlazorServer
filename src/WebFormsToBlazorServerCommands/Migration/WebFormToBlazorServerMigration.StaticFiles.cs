using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeFactory.VisualStudio;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// Copies the static files that were used in the web forms project to the blazor server project.
        /// </summary>
        /// <param name="webFormProjectData">Pre cached project data about the web forms project.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        public async Task MigrateStaticFiles(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Letting the dialog know the migration step has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.StaticFiles, MigrationStatusEnum.Running);

                //Fields that will hold the reference to different visual studio files that will be copied to the blazor server project.
                List<VsDocument> imageFiles = new List<VsDocument>();
                List<VsDocument> cssFiles = new List<VsDocument>();
                List<VsDocument> jsFiles = new List<VsDocument>();

                //Loading up the project directory from the web forms project definition.
                string webFormsProjectDirectory = Path.GetDirectoryName(webFormProject.Path);

                if (string.IsNullOrEmpty(webFormsProjectDirectory))
                {
                    //Could not find the web forms project directory, fail this migration step and continue.
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Error, $"Could not locate the web forms project directory, step cannot be completed.");
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.StaticFiles, MigrationStatusEnum.Failed);
                    return;
                }

                //Loading up the project directory for the blazor project.
                string blazorProjectDirectory = Path.GetDirectoryName(blazorServerProject.Path);

                if (string.IsNullOrEmpty(blazorProjectDirectory))
                {
                    //Could not find the blazor project directory, fail this migration step and continue.
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Error, $"Could not locate the blazor project directory, step cannot be completed.");
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.StaticFiles, MigrationStatusEnum.Failed);
                    return;
                }

                //Locating the wwwroot folder in the blazor project
                var blazorProjectDirectoryInfo = new DirectoryInfo(blazorProjectDirectory);
                var blazorWebRootFolder = blazorProjectDirectoryInfo.GetDirectories("wwwroot").FirstOrDefault()?.FullName;
                if (string.IsNullOrEmpty(blazorWebRootFolder))
                {
                    blazorWebRootFolder = $"{blazorProjectDirectory}\\wwwroot";
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Warning, $"Could not locate the wwwroot folder in the blazor project will be added at the root of the {blazorServerProject.Name} project.");
                }

                //Setting the root file paths where static content files will be copied to.
                var imagesFolder = $"{blazorWebRootFolder}\\images";
                var cssFolder = $"{blazorWebRootFolder}\\css";
                var jsFolder = $"{blazorWebRootFolder}\\script";

                //Get Image files (.gif, .jpeg, .png, .bitmap)
                imageFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".gif", true));
                imageFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".jpeg", true));
                imageFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".png", true));
                imageFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".bitmap", true));
                imageFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".ico", true));

                //Copying Image files (using the file system into the target under the wwwroot/Images/{folder}/{imageFile}
                foreach (var document in imageFiles)
                {
                    var target = document.CopyProjectFile(webFormsProjectDirectory, imagesFolder);
                    if (target == null)
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Warning, $"Could not copy the file '{document.Name}' to the blazor project '{blazorServerProject.Name}', you will need to move this file.");
                    }
                    else
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles,
                            MessageTypeEnum.Information,
                            $"Copied the static file '{document.Name}' to the blazor project '{blazorServerProject.Name}'");
                    }
                }

                //Get CSS files (.css)
                cssFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".css", true));

                //Copying css files (using the file system into the target under the wwwroot/css/{folder}/{cssFile}
                foreach (var document in cssFiles)
                {
                    var target = document.CopyProjectFile(webFormsProjectDirectory, cssFolder);
                    if (target == null)
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Warning, $"Could not copy the file '{document.Name}' to the blazor project '{blazorServerProject.Name}', you will need to move this file.");
                    }
                    else
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles,
                            MessageTypeEnum.Information,
                            $"Copied the static file '{document.Name}' to the blazor project '{blazorServerProject.Name}'");
                    }
                }

                //Get CSS files (.css)
                jsFiles.AddRange(webFormProjectData.GetDocumentsWithExtension(webFormsProjectDirectory, ".js", true));

                //Copying css files (using the file system into the target under the wwwroot/css/{folder}/{cssFile}
                foreach (var document in jsFiles)
                {
                    var target = document.CopyProjectFile(webFormsProjectDirectory, jsFolder);
                    if (target == null)
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Warning, $"Could not copy the file '{document.Name}' to the blazor project '{blazorServerProject.Name}', you will need to move this file.");
                    }
                    else
                    {
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles,
                            MessageTypeEnum.Information,
                            $"Copied the static file '{document.Name}' to the blazor project '{blazorServerProject.Name}'");
                    }
                }

                //Completed the migration step informing the dialog.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.StaticFiles, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.StaticFiles, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.StaticFiles, MigrationStatusEnum.Failed);
            }

        }
    }
}
