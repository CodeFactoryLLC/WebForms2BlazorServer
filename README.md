# WebForms2BlazorServer Automation Template

This project is an implementation of a CodeFactory automation template designed specifically to migrate an existing legacy .NET Webforms application to a Blazor Server application.  This template is offered as open-source and anyone can download and alter it to suit the particular needs of any WebForms migration efforts that they are faced with.

## New to CodeFactory?
In the simplest terms, CodeFactory is a real time software factory that is triggered from inside Visual Studio during the design and construction of software. CodeFactory allows for development staff to automate repetitive development tasks that take up developer’s time.

Please see the following link for further information and guidance about the [CodeFactory Runtime](https://github.com/CodeFactoryLLC/CodeFactory) or the [CodeFactory SDK](https://www.nuget.org/packages/CodeFactorySDK/).

## Core purpose of the template
This automation template was built using the [CodeFactory SDK](https://www.nuget.org/packages/CodeFactorySDK/) to make the task of migrating/converting a legacy .NET WebForms web application over to an updated Blazor Server-side application.  The template has the following commands and features avaible to anyone who has a valid copy of [CodeFactory Runtime](http://www.codefactory.software) installed as an extension inside of their local copy of Visual Studio.
- Migrate a single *.aspx page from a source WebForms project into a target Blazor project within the same solution.
  - This will convert any of the legacy markup from `<% %>` to its new Razor syntax `@()`.
  - locate any `asp:*` tags/controls within the source and convert those over to a WebForms Blazor component from this project [Fritz.BlazorWebFormsComponents](https://www.nuget.org/packages/Fritz.BlazorWebFormsComponents/)
  - Locate any code-behind files and migrate those over into a new code-behind file for the Blazor Component. (Some code is commented out as it does not apply to Blazor but is copied for the sake of historical reference)
- Migrate an entire WebForms project in bulk over to a target Blazor Server-side project.
  - Migrate Startup process artifacts
  - Migrate HTTP Modules
  - Migrate static assets (images/scripts)
  - Migrate any script bundling configurations
  - Migrate all *.aspx pages in the project including any code-behind files
  - Migrate over .config settings
  - Migrate over business/app logic (any *.cs files that are *not* an aspx/asax/ascx)

## Links to Guidance
For technical explanations of each file/class/command in this Automation Template please see the [guidance](guidance/guidance.md) page for further information.