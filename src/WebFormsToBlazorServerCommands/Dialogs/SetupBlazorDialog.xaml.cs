using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CodeFactory.Logging;
using CodeFactory.VisualStudio;
using CodeFactory.VisualStudio.UI;
using WebFormsToBlazorServerCommands.Migration;

namespace WebFormsToBlazorServerCommands.Dialogs
{
    /// <summary>
    /// Interaction logic for SetupBlazorDialog.xaml
    /// </summary>
    public partial class SetupBlazorDialog : VsUserControl,IMigrationStatusUpdate
    {
        /// <summary>
        /// Creates an instance of the user control.
        /// </summary>
        /// <param name="vsActions">The visual studio actions that are accessible by this user control.</param>
        /// <param name="logger">The logger used by this user control.</param>
        public SetupBlazorDialog(IVsActions vsActions, ILogger logger) : base(vsActions, logger)
        {
            //Initializes the controls on the screen and subscribes to all control events (Required for the screen to run properly)
            InitializeComponent();

            //Creating an empty observable collection. This will be updated during the execution of the migration process.
            StepStatus = new ObservableCollection<MigrationStepStatus>();
        }

        #region Dependency Properties

        /// <summary>
        /// The solution projects that will be used by the dialog to select the source and destination projects.
        /// </summary>
        public IEnumerable<VsProject> SolutionProjects
        {
            get { return (IEnumerable<VsProject>)GetValue(SolutionProjectsProperty); }
            set { SetValue(SolutionProjectsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SolutionProjects.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SolutionProjectsProperty =
            DependencyProperty.Register("SolutionProjects", typeof(IEnumerable<VsProject>), typeof(SetupBlazorDialog), null);


        public static readonly DependencyProperty StepStatusProperty = DependencyProperty.Register(
            "StepStatus", typeof(ObservableCollection<MigrationStepStatus>), typeof(SetupBlazorDialog), new PropertyMetadata(default(ObservableCollection<MigrationStepStatus>)));

        public ObservableCollection<MigrationStepStatus> StepStatus
        {
            get { return (ObservableCollection<MigrationStepStatus>)GetValue(StepStatusProperty); }
            set { SetValue(StepStatusProperty, value); }
        }

        #endregion

        #region Button Event Management

        /// <summary>
        /// Processes the cancel button click event.
        /// </summary>
        /// <param name="sender">Hosting user control.</param>
        /// <param name="e">Ignored when used in this context.</param>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            //Closing the dialog and returning control to visual studio.
            this.Close();
        }

        /// <summary>
        /// Process the ok button click event.
        /// </summary>
        /// <param name="sender">Hosting user control.</param>
        /// <param name="e">We dont use the routing args with this implementation.</param>
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            //Loading the selected projects from the dialog
            VsProject webformProject = ComboboxWebFormsProject.SelectedItem as VsProject;
            VsProject blazorProject = ComboboxBlazorProject.SelectedItem as VsProject;

            //Checking that a source webforms project has been selected.
            if (webformProject == null)
            {
                MessageBox.Show("You must select a  Webforms Project before continuing.");
                return;
            }

            //Checking to make sure a target blazor project has been selected.
            if (blazorProject == null)
            {
                MessageBox.Show("You must have a Blazor Project selected before continuing.");
                return;
            }

            //Checking to make sure the same project was not selected.
            if (webformProject.Name == blazorProject.Name)
            {
                MessageBox.Show("The web forms project and the blazor project cannot be the same.");
                return;
            }

            bool migrationStepsSelected = false;

            migrationStepsSelected = CheckBoxMigrateAspxPages.IsChecked.GetResult() | CheckBoxMigrateBundling.IsChecked.GetResult() |
                                     CheckBoxMigrateConfiguration.IsChecked.GetResult() | CheckBoxMigrateHttpModules.IsChecked.GetResult() |
                                     CheckBoxMigrateLogic.IsChecked.GetResult() | CheckBoxMigrateStaticFiles.IsChecked.GetResult() |
                                     CheckBoxStartupProcess.IsChecked.GetResult();

            if (!migrationStepsSelected)
            {
                MessageBox.Show("You have to select a migration step in order to continue.");
                return;
            }

            try
            {
                var migrationSteps = new MigrationSteps(CheckBoxStartupProcess.IsChecked.GetResult(),
                    CheckBoxMigrateHttpModules.IsChecked.GetResult(), CheckBoxMigrateStaticFiles.IsChecked.GetResult(),
                    CheckBoxMigrateBundling.IsChecked.GetResult(), CheckBoxMigrateAspxPages.IsChecked.GetResult(),
                    CheckBoxMigrateConfiguration.IsChecked.GetResult(), CheckBoxMigrateLogic.IsChecked.GetResult());



                //Creating an empty observable collection. This will be updated during the execution of the migration process.
                StepStatus = new ObservableCollection<MigrationStepStatus>();

                //Updating the dialog to not accept input while the migration is processing
                ButtonOk.Content = "Processing";
                ButtonOk.IsEnabled = false;
                ButtonCancel.IsEnabled = false;

                //Creating the migration process logic. Notice that we pass in a copy of the visual studio actions that code factory uses for visual studio automation.
                //In addition we pass a reference to the dialog itself.
                //We have implemented an interface on the dialog that allows the background thread to call into this dialog and update the migration status. 
                var migrationProcess = new WebFormToBlazorServerMigration(_visualStudioActions, this);

                //Updating the UI to begin the migration process.
                SetupStatusForMigration(migrationSteps);

                //Starting the migration process on a background thread and letting the dialog keep processing UI updates.
                Task.Run(() => migrationProcess.StartMigration(webformProject, blazorProject, migrationSteps));

            }
            catch (Exception unhandledError)
            {
                //Displaying the error that was not managed during the migration process. 
                MessageBox.Show($"The following unhandled error occured while performing the setup operations. '{unhandledError.Message}'",
                    "Unhandled Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Helper that configures the screen once the migration process has begun.
        /// </summary>
        /// <param name="steps">The migration steps to be performed.</param>
        private void SetupStatusForMigration(MigrationSteps steps)
        {
            //Updating logic step
            CheckBoxMigrateLogic.Visibility = Visibility.Collapsed;
            TextBlockMigrateLogicStatus.Visibility = Visibility.Visible;
            if (!steps.AppLogic) TextBlockMigrateLogicStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating the Http Modules step
            CheckBoxMigrateHttpModules.Visibility = Visibility.Collapsed;
            TextBlockMigrateHttpModulesStatus.Visibility = Visibility.Visible;
            if (!steps.HttpModules) TextBlockMigrateHttpModulesStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating the static files step.
            CheckBoxMigrateStaticFiles.Visibility = Visibility.Collapsed;
            TextBlockMigrateStaticFilesStatus.Visibility = Visibility.Visible;
            if (!steps.StaticFiles) TextBlockMigrateStaticFilesStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating Aspx Page s step
            CheckBoxMigrateAspxPages.Visibility = Visibility.Collapsed;
            TextBlockMigrateAspxPagesStatus.Visibility = Visibility.Visible;
            if (!steps.AspxPages) TextBlockMigrateAspxPagesStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating the Bundling step
            CheckBoxMigrateBundling.Visibility = Visibility.Collapsed;
            TextBlockMigrateBundlingStatus.Visibility = Visibility.Visible;
            if (!steps.Bundling) TextBlockMigrateBundlingStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating the configuration step
            CheckBoxMigrateConfiguration.Visibility = Visibility.Collapsed;
            TextBlockMigrateConfigurationStatus.Visibility = Visibility.Visible;
            if (!steps.Configuration) TextBlockMigrateConfigurationStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating the app logic step
            CheckBoxMigrateLogic.Visibility = Visibility.Collapsed;
            TextBlockMigrateLogicStatus.Visibility = Visibility.Visible;
            if (!steps.AppLogic) TextBlockMigrateLogicStatus.TextDecorations = TextDecorations.Strikethrough;

            //Updating the startup logic step
            CheckBoxStartupProcess.Visibility = Visibility.Collapsed;
            TextBlockStartupProcessStatus.Visibility = Visibility.Visible;
            if (!steps.Startup) TextBlockStartupProcessStatus.TextDecorations = TextDecorations.Strikethrough;

            //Displaying the migration process status TextBlock.
            TextBlockMigrationProcessStatus.Visibility = Visibility.Visible;

        }

        #endregion

        #region Implementation of IMigrationStatusUpdate


        /// <summary>
        /// Informs the user of the current status of the migration process.
        /// </summary>
        /// <param name="migrationStep">The migration step the status applies to.</param>
        /// <param name="messageType">The type of messaging being communicated.</param>
        /// <param name="statusMessage">Status message to be sent to the user.</param>
        public async Task UpdateCurrentStatusAsync(MigrationStepEnum migrationStep, MessageTypeEnum messageType, string statusMessage)
        {
            //confirming there is a message to update.
            if (string.IsNullOrEmpty(statusMessage)) return;

            //Scheduling the update of the observable collection that bound to the data grid on the dialog.
            await Dispatcher.InvokeAsync(() =>
            {
                var status = new MigrationStepStatus { MessageType = messageType.GetName(), MigrationStep = migrationStep.GetName(), Status = statusMessage };
                StepStatus.Add(status);
            }
            , DispatcherPriority.Normal);
        }

        /// <summary>
        /// Informs the user of the status of a target step of the migration process.
        /// </summary>
        /// <param name="step">Step that is getting updated.</param>
        /// <param name="status">The status the step is being changed to.</param>
        public async Task UpdateStepStatusAsync(MigrationStepEnum step, MigrationStatusEnum status)
        {
            //Scheduling the update of the target step in the migration process process.
            //Extension method is used on the TextBlock to update the status of the migration step.
            await Dispatcher.InvokeAsync(() =>
            {
                switch (step)
                {
                    case MigrationStepEnum.Startup:
                        BorderStartupProcess.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;
                        TextBlockStartupProcessStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.HttpModules:
                        BorderMigrateHttpModules.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;
                        TextBlockMigrateHttpModulesStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.StaticFiles:
                        BorderMigrateStaticFiles.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;
                        TextBlockMigrateStaticFilesStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.Bundling:
                        BorderMigrateBundling.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;
                        TextBlockMigrateBundlingStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.AspxPages:
                        BorderMigrateAspxPages.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;

                        TextBlockMigrateAspxPagesStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.Config:
                        BorderMigrateConfiguration.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;
                        TextBlockMigrateConfigurationStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.AppLogic:
                        BorderMigrateLogic.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;

                        TextBlockMigrateLogicStatus.UpdateMigrationStatus(status);
                        break;

                    case MigrationStepEnum.MigrationProcess:
                        BorderMigrateProcess.Visibility = status == MigrationStatusEnum.Running
                            ? Visibility.Visible
                            : Visibility.Hidden;

                        TextBlockMigrationProcessStatus.UpdateMigrationStatus(status);
                        break;
                }
            }
                , DispatcherPriority.Normal);
        }

        /// <summary>
        /// Informs the hosting process the migration has been finished and other operations can continue.
        /// </summary>
        public async Task UpdateMigrationFinishedAsync()
        {
            //Schedules execution on the UI thread to update the OK button to disappear. 
            //The cancel button will be changed to finished. This will trigger the close of the UI.
            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockMigrationProcessStatus.Text = "Migration Process Complete";
                ButtonOk.Visibility = System.Windows.Visibility.Collapsed;
                ButtonCancel.Content = "Finished";
                ButtonCancel.IsEnabled = true;
            }
                , DispatcherPriority.Normal);
        }

        /// <summary>
        /// Informs the user of the migration process with a target message.
        /// </summary>
        /// <param name="title">The title of the message.</param>
        /// <param name="message">The message to be displayed to the user.</param>
        /// <param name="messageType">The type of message to display to the user.</param>
        public async Task MessageToUserAsync(string title, string message, MessageTypeEnum messageType)
        {
            //Schedules execution on the UI thread to show a message box
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBoxImage messageBoxImage = MessageBoxImage.Information;

                switch (messageType)
                {
                    case MessageTypeEnum.Warning:
                        messageBoxImage = MessageBoxImage.Warning;
                        break;
                    case MessageTypeEnum.Error:
                        messageBoxImage = MessageBoxImage.Error;
                        break;
                }

                MessageBox.Show(message, title, MessageBoxButton.OK, messageBoxImage);
            }
                , DispatcherPriority.Normal);
        }

        #endregion


    }
}
