using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Enumeration used to determine the status of a migration step.
    /// </summary>
    public enum MigrationStatusEnum
    {
        /// <summary>
        /// The current migration step is running.
        /// </summary>
        Running = 0,

        /// <summary>
        /// The current migration step has passed.
        /// </summary>
        Passed = 1,

        /// <summary>
        /// The current migration step has failed. 
        /// </summary>
        Failed = 2
    }
}
