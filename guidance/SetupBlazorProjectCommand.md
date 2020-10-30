# SetupBlazorProject.cs Project Command file

## Overview
This file, logically, is a CodeFactory [Project Command](http://docs.codefactory.software/guidance/overview-commands-intro.html) and contains all of the logic neccesary for the CodeFactory runtime to call and execute from Visual Studio.  There is a single class defined called `public class MigrateWebForm : ProjectDocumentCommandBase`

## Fields
The following fields are defined within this class:

Declaration | Notes
----------- | -----------
`private static readonly string commandTitle` | Used to set the title that shows up in the context-menu for the Visual Studio Solution Explorer.
`private static readonly string commandDescription` | Sets a longer descriptive text for the command that shows up in several of the loaded-command windows and diagnostics screens.

## Constrcutor
There is a single default constructor that is defined.  This constructor should have no logic in it and is responsible for passing back its parameters to its baseclass.

`public SetupBlazorProject(ILogger logger, IVsActions vsActions) : base(logger, vsActions, commandTitle, commandDescription)`

## Methods
These are the following methods which are defined within this class file:

Declaration | Notes
--------- | --------
`public override async Task<bool> EnableCommandAsync(VsProject result)` | Validation logic that will determine if this command should be enabled for execution.
`public override async Task ExecuteCommandAsync(VsProject result)` | Code factory framework calls this method when the command has been executed.