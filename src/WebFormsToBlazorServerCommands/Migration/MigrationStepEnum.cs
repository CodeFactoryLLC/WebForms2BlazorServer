using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormsToBlazorServerCommands.Migration
{
    public enum MigrationStepEnum
    {
        Startup = 0,
        HttpModules = 1,
        StaticFiles = 2,
        Bundling = 3,
        AspxPages = 4,
        Config = 5,
        AppLogic =6,
        MigrationProcess = 7
    }
}
