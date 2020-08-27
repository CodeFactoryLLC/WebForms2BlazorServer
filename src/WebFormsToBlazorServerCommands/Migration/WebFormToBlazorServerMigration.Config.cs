using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using CodeFactory.VisualStudio;
using Newtonsoft.Json;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// Migrates the web.config to a settings file in the blazor server project.
        /// </summary>
        /// <param name="webFormProjectData">Data from the web forms project already loaded and provided in a list.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        public async Task MigrateConfig(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Informing the dialog the migration step has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Config, MigrationStatusEnum.Running);

                //Get the web.config file from the source project;
                if (!(webFormProjectData.FirstOrDefault(c =>
                    c.ModelType == VisualStudioModelType.Document &
                    c.Name.ToLower().Equals("web.config")) is VsDocument configDocument))
                {
                    //No web.config was found.
                    //Sending a status update to the dialog
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Config, MessageTypeEnum.Error,
                        $"No web.config file was found in the web forms project {webFormProject.Name}. Cannot migrate the configuration.");

                    //Informing the dialog the migration step has failed.
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Config, MigrationStatusEnum.Running);

                    return;
                }
                else
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(await configDocument.GetDocumentContentAsStringAsync());

                    var jsonConverted = JsonConvert.SerializeXmlNode(xmlDoc);

                    //Add converted web.config to a file in the target solution.  this will be named webconfig.json
                    var thing = await blazorServerProject.AddDocumentAsync("webconfig.json", jsonConverted);

                    //Sending a status update to the dialog
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Config, MessageTypeEnum.Information,
                        $"The web.config file has been moved to the root directory of {blazorServerProject.Name} and converted to 'webconfig.json'.");

                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Config, MessageTypeEnum.Information,
                        $"** Please review the webconfig.json file and make sure that it meets the needs of the converted Blazor app.");
                }

                //Completed the migration step informing the dialog.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Config, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Config, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Config, MigrationStatusEnum.Failed);
            }
        }
    }
}
