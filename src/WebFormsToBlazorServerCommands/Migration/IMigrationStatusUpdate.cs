using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Contract that defines how information is to be communicated from the migration automation to the user.
    /// </summary>
    public interface IMigrationStatusUpdate
    {
        /// <summary>
        /// Informs the user of the current status of the migration process.
        /// </summary>
        /// <param name="migrationStep">The migration step the status applies to.</param>
        /// <param name="messageType">The type of messaging being communicated.</param>
        /// <param name="statusMessage">Status message to be sent to the user.</param>
        Task UpdateCurrentStatusAsync(MigrationStepEnum migrationStep, MessageTypeEnum messageType, string statusMessage);

        /// <summary>
        /// Informs the user of the status of a target step of the migration process.
        /// </summary>
        /// <param name="step">Step that is getting updated.</param>
        /// <param name="status">The status the step is being changed to.</param>
        Task UpdateStepStatusAsync(MigrationStepEnum step, MigrationStatusEnum status);

        /// <summary>
        /// Informs the hosting process the migration has been finished and other operations can continue.
        /// </summary>
        Task UpdateMigrationFinishedAsync();

        /// <summary>
        /// Informs the user of the migration process with a target message.
        /// </summary>
        /// <param name="title">The title of the message.</param>
        /// <param name="message">The message to be displayed to the user.</param>
        /// <param name="messageType">The type of message to display to the user.</param>
        Task MessageToUserAsync(string title, string message, MessageTypeEnum messageType);
    }
}
