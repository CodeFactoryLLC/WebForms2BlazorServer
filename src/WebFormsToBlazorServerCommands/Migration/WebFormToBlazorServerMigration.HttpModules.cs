using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.DotNet.CSharp;
using CodeFactory.VisualStudio;
using WebFormsToBlazorServerCommands.Templates;

namespace WebFormsToBlazorServerCommands.Migration
{
    public partial class WebFormToBlazorServerMigration
    {
        /// <summary>
        /// Migrates the definition of existing Http Modules that were used in the web forms project into the blazor project.
        /// </summary>
        /// <param name="webFormProjectData">Pre cached project data about the web forms project.</param>
        /// <param name="webFormProject">The web forms project that we are migrating data from.</param>
        /// <param name="blazorServerProject">The blazor server project this is being migrated to.</param>
        public async Task MigrateHttpModulesAsync(IReadOnlyList<VsModel> webFormProjectData, VsProject webFormProject, VsProject blazorServerProject)
        {
            try
            {
                //Letting the dialog know the http modules migration has started.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.HttpModules, MigrationStatusEnum.Running);

                //find class(es) that inherit HttpApplication
                var handlerClasses = webFormProjectData.GetClassesThatImplementInterface("IHttpHandler");
                var moduleClasses = webFormProjectData.GetClassesThatImplementInterface("IHttpModule");

                if (!handlerClasses.Any() && !moduleClasses.Any())
                {
                    //No handler classes or modules were found updating the status and exiting this step.
                    await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.Startup, MessageTypeEnum.Error,
                        $"No classes were found in {webFormProject.Name} that inherit from either 'IHttpHandler' or 'IHttpModule'");

                    //Setting the status to passed wasn't a failure and the solution will not require these to be added.
                    await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.HttpModules, MigrationStatusEnum.Passed);
                    return;
                }

                //Making sure a modules folder exists, if it does not create it in the blazor server project.
                var modulesFolder = await blazorServerProject.CheckAddFolder("Modules");

                //Creating a dictionary that holds the target namespace for modules.
                var conversionData = new Dictionary<string, string>
                {
                    {"Namespace", $"{blazorServerProject.DefaultNamespace}.Modules"}
                };

                CsModelStore store = null;

                //Create a class file in the Modules folder from a T4 Template for each handler class
                foreach (CsSource source in handlerClasses)
                {
                    //Setting up the model data to pass off to a T4 Factory
                    store = new CsModelStore();
                    store.SetModel(source);

                    //Calling the T4 factory and getting back the formatted file content.
                    var fileContent = ModuleFactory.GenerateSource(store, conversionData);

                    //Calling CodeFactory project system API to add a new document to the project folder, also injecting the new file content into the file.
                    await modulesFolder.AddDocumentAsync($"{Path.GetFileNameWithoutExtension(source.SourceDocument)}Handler.cs", fileContent);

                    //Clearing model store for the next call
                    store = null;
                }

                //Create a class file in the Modules folder from a T4 Template for each model class
                foreach (CsSource source in moduleClasses)
                {
                    store = new CsModelStore();
                    store.SetModel(source);
                    await _visualStudioActions.ProjectFolderActions.AddDocumentAsync(modulesFolder, $"{Path.GetFileNameWithoutExtension(source.SourceDocument)}Handler.cs", ModuleFactory.GenerateSource(store, conversionData));
                    store = null;
                }

                //Completed process the modules informing the dialog it has passed.
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.HttpModules, MigrationStatusEnum.Passed);
            }
            catch (Exception unhandledError)
            {
                //Dumping the exception that occured directly into the status so the user can see what happened.
                await _statusTracking.UpdateCurrentStatusAsync(MigrationStepEnum.HttpModules, MessageTypeEnum.Error, $"The following unhandled error occured. '{unhandledError.Message}'");

                //Updating the status that the step failed
                await _statusTracking.UpdateStepStatusAsync(MigrationStepEnum.HttpModules, MigrationStatusEnum.Failed);
            }
        }
    }
}
