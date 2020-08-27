using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.DotNet.CSharp;
using CodeFactory.VisualStudio;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// This step is used to locate any code artifacts within the Web Forms application that inherit from HttpApplication in order to alter
        /// any of the default behavior of IIS.  Any code found will be copied and moved into the end of the blazor target projects Startup.cs file definition.
        /// </summary>
        /// <param name="webFormProjectData">Pre cached project data about the web forms project.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        private async Task MigrateStartupAsync(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Informing the dialog the migration step has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Startup, MigrationStatusEnum.Running);

                //Find class(es) that inherit HttpApplication
                var startupClasses = webFormProjectData.GetClassesThatInheritBase("HttpApplication");

                if (!startupClasses.Any())
                {
                    //No startup classes were found updating the hosting dialog to inform the user there was nothing to convert in the startup process.
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Startup, MessageTypeEnum.Information, $"No classes were found in {webFormProject.Name} that inherit from 'HttpApplication'");
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Startup, MigrationStatusEnum.Passed);
                    return;
                }

                var blazorStartupClass = await blazorServerProject.FindClassAsync("Startup", false);

                if (blazorStartupClass == null)
                {
                    //No startup class was found in the blazor server project. Cannot update the startup class. Informing the user and failing the step
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Startup, MessageTypeEnum.Warning, $"The target project {blazorServerProject.Name} does not have definition for a Startup class ('public class Startup').");
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Startup, MigrationStatusEnum.Failed);
                    return;
                }

                //Get the methods
                //Copy them into the blazorStartupClass commented out.
                foreach (var source in startupClasses)
                {

                    //loading the class data
                    var sourceClass = source.Classes.FirstOrDefault();

                    //If no class was found continue the process.
                    if (sourceClass == null) continue;

                    //double loop - I do not like this and it needs to be refactored.
                    foreach (var method in sourceClass.Methods)
                    {
                        await blazorStartupClass.AddToEndAsync(blazorStartupClass.SourceFiles.First(), $"\r\n/*{method.FormatCSharpDeclarationSyntax()}\r\n{{ { await method.GetBodySyntaxAsync()} \r\n}}*/");
                        await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Startup, MessageTypeEnum.Information, $"Class: {sourceClass.Name} Method: {method.Name} has been copied into the Startup.cs class commented out.  Please refactor manually.");
                    }
                }

                //All items process updating the status of the step has passed.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Startup, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Startup, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.Startup, MigrationStatusEnum.Failed);
            }

        }
    }
}
