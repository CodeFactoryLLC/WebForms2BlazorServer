using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.Logging;
using CodeFactory.VisualStudio;
using CodeFactory.VisualStudio.SolutionExplorer;
using CodeFactory.Formatting.CSharp;
using WebFormsToBlazorServerCommands.Dialogs;
using System.IO;

namespace WebFormsToBlazorServerCommands.Commands.Document
{
    /// <summary>
    /// Code factory command for automation of a document when selected from a project in solution explorer.
    /// </summary>
    public class MigrateWebForm : ProjectDocumentCommandBase
    {
        private static readonly string commandTitle = "Migrate to Blazor";
        private static readonly string commandDescription = "Migrates a single *.aspx page to a Blazor componenet.";

#pragma warning disable CS1998

        /// <inheritdoc />
        public MigrateWebForm(ILogger logger, IVsActions vsActions) : base(logger, vsActions, commandTitle, commandDescription)
        {
            //Intentionally blank
        }

        #region Overrides of VsCommandBase<VsProjectDocument>

        /// <summary>
        /// Validation logic that will determine if this command should be enabled for execution.
        /// </summary>
        /// <param name="result">The target model data that will be used to determine if this command should be enabled.</param>
        /// <returns>Boolean flag that will tell code factory to enable this command or disable it.</returns>
        public override async Task<bool> EnableCommandAsync(VsDocument result)
        {
            //Result that determines if the the command is enabled and visible in the context menu for execution.
            bool isEnabled = false;

            try
            {
                isEnabled = result.Name.Contains("aspx");
            }
            catch (Exception unhandledError)
            {
                _logger.Error($"The following unhandled error occured while checking if the solution explorer project document command {commandTitle} is enabled. ",
                    unhandledError);
                isEnabled = false;
            }

            return isEnabled;
        }

        /// <summary>
        /// Code factory framework calls this method when the command has been executed. 
        /// </summary>
        /// <param name="result">The code factory model that has generated and provided to the command to process.</param>
        public override async Task ExecuteCommandAsync(VsDocument result)
        {
            try
            {
                //User Control
                var migrateDialog = await VisualStudioActions.UserInterfaceActions.CreateVsUserControlAsync<MigrateWebFormDialog>();

                //Get Project List
                var solution = await VisualStudioActions.SolutionActions.GetSolutionAsync();

                //Set properties on the dialog
                var projects = await solution.GetProjectsAsync(false);
                migrateDialog.SolutionProjects = projects;
                migrateDialog.FormToMigrate = result;

                //Show the dialog
                await VisualStudioActions.UserInterfaceActions.ShowDialogWindowAsync(migrateDialog);

            }
            catch (Exception unhandledError)
            {
                _logger.Error($"The following unhandled error occured while executing the solution explorer project document command {commandTitle}. ",
                    unhandledError);

            }

        }


    }

    #endregion
}

