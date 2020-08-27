using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.VisualStudio;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// Clones the bundleconfig.json into the blazer server project.
        /// </summary>
        /// <param name="webFormProjectData">Pre cached project data about the web forms project.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        public async Task MigrateBundling(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Letting the dialog know the migration step has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Bundling, MigrationStatusEnum.Running);

                //Finding the bundlconfig.json file.
                if (webFormProjectData.FirstOrDefault(p => p.Name.ToLower().Equals("bundleconfig.json")) is VsDocument bundleConfig)
                {
                    //Found the config file. loading its content.
                    var bundleText = await bundleConfig.GetDocumentContentAsStringAsync();

                    //Creating the bundleconfig.json in the blazor server project, and injecting the content.
                    var thing = await blazorServerProject.AddDocumentAsync("bundleconfig.json", bundleText);

                    //Sending a status to the dialog
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Bundling, MessageTypeEnum.Information,
                        $"The bundleconfig.json file has been copied to the root directory of {blazorServerProject.Name}.");
                }
                else
                {
                    //No bundle configuration was found. No additional actions need to be taken.
                    //Sending a status to the dialog
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Bundling, MessageTypeEnum.Information,
                        $"There was no 'bundleconfig.json' file found in the root of the source project {webFormProject.Name}.  No files were copied.");

                }

                //Completed the migration step informing the dialog.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Bundling, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Bundling, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Bundling, MigrationStatusEnum.Failed);
            }

        }
    }
}
