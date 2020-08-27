using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Enumeration of the types of messages that can communicated to a user of the migration process.
    /// </summary>
    public enum MessageTypeEnum
    {
        /// <summary>
        /// You are providing the user with a standard information message.
        /// </summary>
        Information = 0,

        /// <summary>
        /// You are providing the user with a warning message.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// You are providing the user with an error message.
        /// </summary>
        Error =2,
    }
}
