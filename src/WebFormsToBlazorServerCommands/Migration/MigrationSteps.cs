using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Data class that holds which migration steps will be executed. 
    /// </summary>
    public class MigrationSteps
    {
        #region Backing fields for properties
        private readonly bool _startup;
        private readonly bool _httpModules;
        private readonly bool _staticFiles;
        private readonly bool _bundling;
        private readonly bool _aspxPages;
        private readonly bool _config;
        private readonly bool _appLogic;
        #endregion

        /// <summary>
        /// Creates an instance of the <see cref="MigrationSteps"/> data class.
        /// </summary>
        /// <param name="startup">Flag to determine the migration of the startup data.</param>
        /// <param name="httpModules">Flag to determine the migration of the http modules.</param>
        /// <param name="staticFiles">Flag to determine the migration of static file content.</param>
        /// <param name="bundling">Flag to determine the migration of bundling data.</param>
        /// <param name="aspxPages">Flag to determine the migration of aspx pages.</param>
        /// <param name="config">Flag to determine the migration of the app configuration.</param>
        /// <param name="appLogic">Flag to determine the migration of existing application logic.</param>
        public MigrationSteps(bool startup, bool httpModules, bool staticFiles, bool bundling, bool aspxPages,
            bool config, bool appLogic)
        {
            _startup = startup;
            _httpModules = httpModules;
            _staticFiles = staticFiles;
            _bundling = bundling;
            _aspxPages = aspxPages;
            _config = config;
            _appLogic = appLogic;
        }

        /// <summary>
        /// Flag that determines if the startup data should be migrated.
        /// </summary>
        public bool Startup => _startup;

        /// <summary>
        /// Flag that determines if the Http modules should be migrated.
        /// </summary>
        public bool HttpModules => _httpModules;

        /// <summary>
        /// Flag that determines if static content should be migrated.
        /// </summary>
        public bool StaticFiles => _staticFiles;

        /// <summary>
        /// Flag that determines if the bundling should be migrated.
        /// </summary>
        public bool Bundling => _bundling;

        /// <summary>
        /// Flag that determines if the aspx pages should be migrated.
        /// </summary>
        public bool AspxPages => _aspxPages;

        /// <summary>
        /// Flag that determines if the configuration should be migrated.
        /// </summary>
        public bool Configuration => _config;

        /// <summary>
        /// Flag that determines if the application logic should be migrated.
        /// </summary>
        public bool AppLogic => _appLogic;
    }
}
