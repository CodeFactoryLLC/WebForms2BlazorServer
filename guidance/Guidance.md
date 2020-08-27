# WebFormsToBlazorServer Guidance
This set of documentation goes into the details of the example CodeFactory Code Automation Template **WebFormsToBlazor**.  If you need explainations of what a Code Automation Template is or how CodeFactory works please use the following link to that documentation.  [CodeFactory Guidance](../../sdk/sdkdocumentation/guidance/toc.md)

## Template Overview
In order to make use of this template you will need to have a target WebForms solution, in our examples below we reference the [*WingTipToys*]() project.  This is an older reference project that is freely available to anyone who wished to download it and give this template a try.  Once you have your project open inside of Visual Studio - go ahead and add a new Project to the solution of type Blazor - Server.

![](../../sdk/sdkdocumentation/images/AddNewBlazorProject.png)

The compiled output of the WebFormsToBlazorServer project, a file called *WebFormsToBlazorServerCommands.cfx* file just needs to be dropped into the root solution folder of the WebForms project.

![](../../sdk/sdkdocumentation/images/WingTipToysRoot.png)  

The next time that you open the WingTipToys solution the CodeFactory runtime will [load](../../sdk/sdkdocumentation/guidance/overview/howdoesitwork.md) the package and make the command available for use.

## Commands
There are currently two(2) commands that have been built inside of the project; 

- SetupBlazorProject
- MigrateWebForm

The first command, *SetupBlazorProject*, is an implementation of a [Project Command](../../sdk/sdkdocumentation/guidance/overview/commands/project.md) type and can be found by right-clicking on the Blazor project in your solution.  The command is found at the bottom of the context menu.

![](../../sdk/sdkdocumentation/images/SetupBlazorProjectContextMenu.png)

The second command, *MigrateWebForm*, is an implementation of a [Project Document Command](../../sdk/sdkdocumentation/guidance/overview/commands/projectdocument.md) type and can be found by right-clicking on any *.aspx file that is found in the *WingTipToys* project.

![](../../sdk/sdkdocumentation/images/MigrateToBlazorContextMenu.png)

## Project Structure
The following folders are found in this project.

### Commands
This folder is further broken down into command type folders.  Please click on each item found below to get further details.
#### Document
Name | Description
-----|-------
MigrateWebForm.cs | This is a [Project Document]() command type that is built to migrate a single *.aspx file from the source WebForms project into an equivalent Blazor Page Component file in the target Blazor Server project.  Please click [here](MigrateWebFormCommand.md) for more details

#### Project
Name | Description
-----|-------
SetupBlazorProject.cs | This is a [Project]() command type that will allow a developer to bulk-migrate an **entire** WebForms appliction including all of its logic, configuration and static assets into a Blazor Server application.  Please click [here](SetupBlazorProjectCommand.md) for more details.
### Dialogs
Name | Description
-----|-------

### Migration
Name | Description
-----|-------

### Templates
Name | Description
-----|-------
