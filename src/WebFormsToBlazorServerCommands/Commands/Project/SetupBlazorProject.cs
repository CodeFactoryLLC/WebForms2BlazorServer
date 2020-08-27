using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.Logging;
using CodeFactory.VisualStudio;
using CodeFactory.VisualStudio.SolutionExplorer;
using WebFormsToBlazorServerCommands.Dialogs;

namespace WebFormsToBlazorServerCommands.Commands.Project
{
    /// <summary>
    /// Code factory command for automation of a project when selected from solution explorer.
    /// </summary>
    public class SetupBlazorProject : ProjectCommandBase
    {
        private static readonly string commandTitle = "Setup Blazor Project";
        private static readonly string commandDescription = "Will setup a blazor project with win form project data.";

#pragma warning disable CS1998

        /// <inheritdoc />
        public SetupBlazorProject(ILogger logger, IVsActions vsActions) : base(logger, vsActions, commandTitle, commandDescription)
        {
            //Intentionally blank
        }
#pragma warning disable CS1998
        #region Overrides of VsCommandBase<VsProject>

        /// <summary>
        /// Validation logic that will determine if this command should be enabled for execution.
        /// </summary>
        /// <param name="result">The target model data that will be used to determine if this command should be enabled.</param>
        /// <returns>Boolean flag that will tell code factory to enable this command or disable it.</returns>
        public override async Task<bool> EnableCommandAsync(VsProject result)
        {
            //Result that determines if the the command is enabled and visible in the context menu for execution.
            bool isEnabled = false;

            try
            {
                var children = await result.GetChildrenAsync(true);

                if (children.Any(p => p.ModelType.Equals(VisualStudioModelType.ProjectFolder) && p.Name.Equals("Pages")))
                {
                    isEnabled = true;
                }

            }
            catch (Exception unhandledError)
            {
                _logger.Error($"The following unhandled error occured while checking if the solution explorer project command {commandTitle} is enabled. ",
                    unhandledError);
                isEnabled = false;
            }

            return isEnabled;
        }

        /// <summary>
        /// Code factory framework calls this method when the command has been executed. 
        /// </summary>
        /// <param name="result">The code factory model that has generated and provided to the command to process.</param>
        public override async Task ExecuteCommandAsync(VsProject result)
        {
            try
            {
                //Creating an instance of the setup blazor dialog class. This internally hooks it into the visual studio UI system.
                var migrateDialog = await VisualStudioActions.UserInterfaceActions.CreateVsUserControlAsync<SetupBlazorDialog>();

                //Get Project List
                var solution = await VisualStudioActions.SolutionActions.GetSolutionAsync();

                //Setting the list of solution projects into the dialog.
                migrateDialog.SolutionProjects = await solution.GetProjectsAsync(true);

                //Showing the dialog
                await VisualStudioActions.UserInterfaceActions.ShowDialogWindowAsync(migrateDialog);
            }
            catch (Exception unhandledError)
            {
                _logger.Error($"The following unhandled error occured while executing the solution explorer project command {commandTitle}. ",
                    unhandledError);

            }
        }

        #endregion
    }
}
