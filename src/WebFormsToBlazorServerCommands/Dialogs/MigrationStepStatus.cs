using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Dialogs
{
    /// <summary>
    /// Data class that holds current status information about a migration step.
    /// </summary>
    public class MigrationStepStatus
    {
        /// <summary>
        /// Type of status messaging being communicated.
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Which migration step does the message belong to.
        /// </summary>
        public string MigrationStep { get; set; }

        /// <summary>
        /// The status message to be displayed.
        /// </summary>
        public string Status { get; set; }
    }
}
