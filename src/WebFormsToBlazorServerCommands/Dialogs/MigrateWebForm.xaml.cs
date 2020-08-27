using System;
using System.Collections.Generic;
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
using CodeFactory.Logging;
using CodeFactory.VisualStudio;
using CodeFactory.VisualStudio.UI;
using CodeFactory;
using System.Collections.ObjectModel;
using WebFormsToBlazorServerCommands.Migration;
using System.Windows.Threading;

namespace WebFormsToBlazorServerCommands.Dialogs
{
    /// <summary>
    /// Interaction logic for ShowFileDOM.xaml
    /// </summary>
    public partial class MigrateWebFormDialog : VsUserControl, IMigrationStatusUpdate
    {

        /// <summary>
        /// Creates an instance of the user control.
        /// </summary>
        /// <param name="vsActions">The visual studio actions that are accessible by this user control.</param>
        /// <param name="logger">The logger used by this user control.</param>
        public MigrateWebFormDialog(IVsActions vsActions, ILogger logger) : base(vsActions, logger)
        {
            //Initializes the controls on the screen and subscribes to all control events (Required for the screen to run properly)
            InitializeComponent();

            //Creating an empty observable collection. This will be updated during the execution of the migration process.
            StepStatus = new ObservableCollection<MigrationStepStatus>();
        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Btn_Ok(object sender, RoutedEventArgs e)
        {

            //Loading the selected project from the dialog
            VsProject blazorProject = ProjectsCombo.SelectedItem as VsProject;

            //Checking to make sure a target blazor project has been selected.
            if (blazorProject == null)
            {
                MessageBox.Show("You must have a Blazor Project selected before continuing.");
                return;
            }

            //Checking to make sure that we've got a source *.aspx file to convert
            if (FormToMigrate == null)
            {
                MessageBox.Show("There has been a problem - there is no source *.aspx file selected to migrate.");
                this.Close();
            }

            try
            {
                //Updating the dialog to not accept input while the migration is processing
                ButtonOk.Content = "Processing";
                ButtonOk.IsEnabled = false;
                ButtonCancel.IsEnabled = false;

                //Creating the migration process logic. Notice that we pass in a copy of the visual studio actions that code factory uses for visual studio automation.
                //In addition we pass a reference to the dialog itself.
                //We have implemented an interface on the dialog that allows the background thread to call into this dialog and update the migration status. 
                var migrationProcess = new WebFormToBlazorServerMigration(_visualStudioActions, this);

                //Starting the migration process on a background thread and letting the dialog keep processing UI updates.
                Task.Run(() => migrationProcess.MigrateSingleASPXFile(FormToMigrate,blazorProject));// .StartMigration(webformProject, blazorProject, migrationSteps))
            }
            catch (Exception unhandledError)
            {
                //Displaying the error that was not managed during the migration process. 
                MessageBox.Show($"The following unhandled error occured while performing the setup operations. '{unhandledError.Message}'",
                    "Unhandled Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public VsDocument FormToMigrate = null;
        public VsProject SourceProject = null;

        // Using a DependencyProperty as the backing store for SolutionProjects.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SolutionProjectsProperty =
            DependencyProperty.Register("SolutionProjects", typeof(IEnumerable<VsProject>), typeof(MigrateWebFormDialog), null);

        /// <summary>
        /// The solution projects that will be used by the dialog to select the source and destination projects.
        /// </summary>
        public IEnumerable<VsProject> SolutionProjects
        {
            get { return (IEnumerable<VsProject>)GetValue(SolutionProjectsProperty); }
            set { SetValue(SolutionProjectsProperty, value); }
        }

        public static readonly DependencyProperty StepStatusProperty = DependencyProperty.Register(
            "StepStatus", typeof(ObservableCollection<MigrationStepStatus>), typeof(MigrateWebFormDialog), new PropertyMetadata(default(ObservableCollection<MigrationStepStatus>)));

        public ObservableCollection<MigrationStepStatus> StepStatus
        {
            get { return (ObservableCollection<MigrationStepStatus>)GetValue(StepStatusProperty); }
            set { SetValue(StepStatusProperty, value); }
        }

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

        public async Task UpdateStepStatusAsync(MigrationStepEnum step, MigrationStatusEnum status)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateMigrationFinishedAsync()
        {
            //Schedules execution on the UI thread to update the OK button to disappear. 
            //The cancel button will be changed to finished. This will trigger the close of the UI.
            await Dispatcher.InvokeAsync(() =>
                {
                    ButtonOk.Visibility = System.Windows.Visibility.Collapsed;
                    ButtonCancel.Content = "Finished";
                    ButtonCancel.IsEnabled = true;
                }
                , DispatcherPriority.Normal);
            
        }

        public async Task MessageToUserAsync(string title, string message, MessageTypeEnum messageType)
        {
            throw new NotImplementedException();
        }
    }
}
