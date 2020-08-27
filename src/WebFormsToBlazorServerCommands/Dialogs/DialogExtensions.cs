using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WebFormsToBlazorServerCommands.Migration;

namespace WebFormsToBlazorServerCommands.Dialogs
{
    /// <summary>
    /// Extensions class that manages data to be used in the dialog.
    /// </summary>
    public static class DialogExtensions
    {
        /// <summary>
        /// Check a nullable bool for a value, will return false if the bool is null.
        /// </summary>
        /// <param name="source">Nullable bool to check</param>
        /// <returns>standard boolean result.</returns>
        public static bool GetResult(this bool? source)
        {
            return source.HasValue ? source.Value : false;
        }

        /// <summary>
        /// Extension method used to help format labels that track migration status. Must be executed on the UI Thread.
        /// </summary>
        /// <param name="source">Label to be updated</param>
        /// <param name="status">Status type used for formatting.</param>
        public static void UpdateMigrationStatus(this TextBlock source, MigrationStatusEnum status)
        {
            switch (status)
            {
                case MigrationStatusEnum.Running:
                    source.FontWeight = FontWeights.Bold;
                    break;

                case MigrationStatusEnum.Passed:
                    source.FontWeight = FontWeights.ExtraBold;
                    source.Foreground = Brushes.Green;
                    break;

                case MigrationStatusEnum.Failed:
                    source.FontWeight = FontWeights.ExtraBold;
                    source.Foreground = Brushes.Red;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Gets a title assigned to each migration step.
        /// </summary>
        /// <param name="source">The migration step to be loaded.</param>
        /// <returns>The title.</returns>
        public static string GetName(this MigrationStepEnum source)
        {
            string name = null;

            switch (source)
            {
                case MigrationStepEnum.Startup:
                    name = "Startup";
                    break;

                case MigrationStepEnum.HttpModules:
                    name = "Http Modules";
                    break;

                case MigrationStepEnum.StaticFiles:
                    name = "Static Files";
                    break;

                case MigrationStepEnum.Bundling:
                    name = "Bundling";
                    break;

                case MigrationStepEnum.AspxPages:
                    name = "Aspx Pages";
                    break;

                case MigrationStepEnum.Config:
                    name = "Configuration";
                    break;

                case MigrationStepEnum.AppLogic:
                    name = "App Logic";
                    break;

                case MigrationStepEnum.MigrationProcess:
                    name = "Migration Process";
                    break;

            }

            return name;
        }

        /// <summary>
        /// Gets a friendly string name for each message type.
        /// </summary>
        /// <param name="source">Message type to process.</param>
        /// <returns>The friendly name.</returns>
        public static string GetName(this MessageTypeEnum source)
        {
            string name = null;

            switch (source)
            {
                case MessageTypeEnum.Information:
                    name = "Information";
                    break;

                case MessageTypeEnum.Warning:
                    name = "Warning";
                    break;

                case MessageTypeEnum.Error:
                    name = "Error";
                    break;
            }

            return name;
        }
    }
}
