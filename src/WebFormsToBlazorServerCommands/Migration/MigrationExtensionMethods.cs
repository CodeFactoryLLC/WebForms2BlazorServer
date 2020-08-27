using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CodeFactory.DotNet.CSharp;
using CodeFactory.VisualStudio;

namespace WebFormsToBlazorServerCommands.Migration
{
    /// <summary>
    /// Class that holds extension methods that are used in migration process.
    /// </summary>
    public static class MigrationExtensionMethods
    {
        /// <summary>
        /// Extension method that loads all <see cref="VsProjectFolder"/> , <see cref="VsDocument"/>, and <see cref="VsCSharpSource"/>
        /// </summary>
        public static async Task<IReadOnlyList<VsModel>> LoadAllProjectData(this VsProject source,bool loadSourceCode = true)
        {
            return source != null ? await source.GetChildrenAsync(true, loadSourceCode) : ImmutableList<VsModel>.Empty;
        }

        /// <summary>
        /// Extension method that searches C# source code files for a base class inheritance.
        /// </summary>
        /// <param name="source">The source visual studio project to search.</param>
        /// <param name="baseClassName">The name of the base class to search for.</param>
        /// <param name="searchChildren">Flag that determines if you search all child project folders under the project.</param>
        /// <returns>The target source code that meets the criteria or an empty list. </returns>
        public static async Task<IReadOnlyList<CsSource>> GetClassesThatInheritBaseAsync(this VsProject source, string baseClassName, bool searchChildren)
        {
            //If the project is not created return an empty list.
            if (source == null) return ImmutableList<CsSource>.Empty;

            //Calling into the CodeFactory project system api to load all project items, will pre load the source code models.
            var children = await source.GetChildrenAsync(searchChildren,true);

            //Pulling out the list of all code files.
            var sourceCodeFiles = children.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource)).Cast<VsCSharpSource>();

            //Returning the code files that implement the target base class.
            return sourceCodeFiles.Select(codeFile => codeFile.SourceCode)
                .Where(sourceCode => sourceCode.Classes.Any(c => c.BaseClass.Name.Equals(baseClassName)))
                .ToImmutableList();
        }

        /// <summary>
        /// Extension method that searches C# source code files for a base class inheritance.
        /// </summary>
        /// <param name="source">The source visual studio project to search.</param>
        /// <param name="baseClassName">The name of the base class to search for.</param>
        /// <returns>The target source code that meets the criteria or an empty list. </returns>
        public static IReadOnlyList<CsSource> GetClassesThatInheritBase(this IReadOnlyList<VsModel> source, string baseClassName)
        {
            //No source model was provided will return an empty list.
            if (source == null) return ImmutableList<CsSource>.Empty;

            //Pulling out the list of all code files.
            var sourceCodeFiles = source.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource)).Cast<VsCSharpSource>();

            //Returning the code files that meet the criteria.
            return sourceCodeFiles.Select(codeFile => codeFile.SourceCode)
                .Where(sourceCode => sourceCode.Classes.Any(c => c.BaseClass.Name.Equals(baseClassName)))
                .ToImmutableList();
        }

        /// <summary>
        /// Extension method that searches a project for a C# class that exists in one of the projects documents.
        /// </summary>
        /// <param name="source">Source Project to search through</param>
        /// <param name="className">The name of the class to search for.</param>
        /// <param name="searchChildren">Flag that determines if the entire project should be searched or just the root of the project.</param>
        /// <returns>The first instance of the class or null.</returns>
        public static async Task<CsClass> FindClassAsync(this VsProject source, string className, bool searchChildren)
        {
                //Loading the visual studio models from the project and pre creating the source code files.
                var children = await source.GetChildrenAsync(searchChildren,true);

                //Extracting all the c# source code files from the returned models.
                var sourceCodeFiles = children.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource)).Cast<VsCSharpSource>();

                //Getting the first code file that contains the class. Returning either null or the found class.
                return sourceCodeFiles.FirstOrDefault(s => s.SourceCode.Classes.Any(c => c.Name.Equals(className)))
                    ?.SourceCode.Classes.FirstOrDefault(c => c.Name.Equals(className));
        }

        /// <summary>
        /// Extension method that searches a list of project models for a C# class that exists in one of the projects documents.
        /// </summary>
        /// <param name="source">List of visual studio models to search</param>
        /// <param name="className">The name of the class to search for.</param>
        /// <returns>The first instance of the class or null.</returns>
        public static CsClass FindClass(this IReadOnlyList<VsModel> source, string className)
        {
            //Extracting all the c# source code files from the returned models.
            var sourceCodeFiles = source.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource)).Cast<VsCSharpSource>();

            //Getting the first code file that contains the class. Returning either null or the found class.
            return sourceCodeFiles.FirstOrDefault(s => s.SourceCode.Classes.Any(c => c.Name.Equals(className)))
                ?.SourceCode.Classes.FirstOrDefault(c => c.Name.Equals(className));
        }

        /// <summary>
        /// Gets target classes that implement a target interface. It will skip classes that implement Page or HttpApplication.
        /// </summary>
        /// <param name="source">The project to search for the classes in.</param>
        /// <param name="interfaceName">The name of the interface to search for.</param>
        /// <param name="searchChildren">Flag to determine if sub folder should be searched or just the root project folder.</param>
        /// <returns>Readonly list of the found source code files with the target classes in them. or an empty list.</returns>
        public static async Task<IReadOnlyList<CsSource>> GetClassesThatImplementInterfaceAsync(this VsProject source, string interfaceName, bool searchChildren)
        {
            //Bounds check will return an empty list if no project was provided.
            if (source == null) return ImmutableList<CsSource>.Empty;

            //Calls into the CodeFactory project system and gets the children of the supplied project. Will load all code files that support C# as CSharpSource files.
            var children = await source.GetChildrenAsync(searchChildren, true);

            //Extracting all the C# code files from the returned project data.
            var codeFiles = children.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource))
                .Cast<VsCSharpSource>();

            //Collection all the code files that meet the criteria and returning the source code models for each.
            return codeFiles.Where(s => s.SourceCode.Classes.Any(c =>
                            (!c.BaseClass.Name.Equals("Page") && !c.BaseClass.Name.Equals("HttpApplication"))
                            &&
                            c.InheritedInterfaces.Any(x => x.Name.Equals(interfaceName))))
                    .Select(s => s.SourceCode)
                    .ToImmutableList();
        }

        /// <summary>
        /// Gets target classes that implement a target interface. It will skip classes that implement Page or HttpApplication.
        /// </summary>
        /// <param name="source">The list of visual studio models to search for the classes in.</param>
        /// <param name="interfaceName">The name of the interface to search for.</param>
        /// <returns>Readonly list of the found source code files with the target classes in them. or an empty list.</returns>
        public static IReadOnlyList<CsSource> GetClassesThatImplementInterface(this IReadOnlyList<VsModel> source, string interfaceName)
        {
            //Bounds check will return an empty list if no project was provided.
            if (source == null) return ImmutableList<CsSource>.Empty;

            //Extracting all the C# code files from the returned project data.
            var codeFiles = source.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource))
                .Cast<VsCSharpSource>();

            //Collection all the code files that meet the criteria and returning the source code models for each.
            return codeFiles.Where(s => s.SourceCode.Classes.Any(c =>
                    (!c.BaseClass.Name.Equals("Page") && !c.BaseClass.Name.Equals("HttpApplication"))
                    &&
                    c.InheritedInterfaces.Any(x => x.Name.Equals(interfaceName))))
                .Select(s => s.SourceCode)
                .ToImmutableList();
        }

        /// <summary>
        /// Used to check a project model for the existence of a folder at the root level of a given name.  If the folder is 
        /// missing - create it.
        /// </summary>
        /// <param name="source">The visual studio project that we are checking exists or creating.</param>
        /// <param name="folderName">The name of the folder to return.</param>
        /// <returns>The existing or created project folder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either provided parameter is not provided.</exception>
        public static async Task<VsProjectFolder> CheckAddFolder(this VsProject source, string folderName)
        {
            //Bounds checking to make sure all the data needed to get the folder returned is provided.
            if(source == null) throw new ArgumentNullException(nameof(source));
            if(string.IsNullOrEmpty(folderName)) throw new ArgumentNullException(nameof(folderName));

            //Calling the project system in CodeFactory and getting all the children in the root of the project.
            var projectFolders = await source.GetChildrenAsync(false);

            //Searching for the project folder, if it is not found will add the project folder to the root of the project.
            return projectFolders.Where(m => m.ModelType == VisualStudioModelType.ProjectFolder)
                       .Where(m => m.Name.Equals(folderName))
                       .Cast<VsProjectFolder>()
                       .FirstOrDefault() 
                   ?? await source.AddProjectFolderAsync(folderName);

        }

        /// <summary>
        /// Returns a list of non-source code documents from VsProject that have a matching extension.
        /// </summary>
        /// <param name="source">The source visual studio project to search.</param>
        /// <param name="extension">The file extension to search for</param>
        /// <param name="searchChildren">Flag that determines if nested project folders should also be searched for files.</param>
        /// <param name="excludeKnownExternalFolders">Flag that determines if a content filter should be applied.</param>
        /// <returns>List of documents that meet the criteria.</returns>
        public static async Task<IReadOnlyList<VsDocument>> GetDocumentsWithExtensionAsync(this VsProject source, string extension, bool searchChildren, bool excludeKnownExternalFolders)
        {
            //If no source is found return an empty list.
            if (source == null) return ImmutableList<VsDocument>.Empty;

            //If no file extension is provided return an empty list.
            if (string.IsNullOrEmpty(extension)) return ImmutableList<VsDocument>.Empty;

            List<VsDocument> result = new List<VsDocument>();

            //Making sure we start with a period for the extension for searching purposes.
            if (!extension.StartsWith(".")) extension = $".{extension}";
            
                //Calling the CodeFactory project system api to get the children of the project.
                var children = await source.GetChildrenAsync(searchChildren);

                //Filtering out to just 
                var sourceFiles = children.Where(p => p.ModelType.Equals(VisualStudioModelType.Document))
                    .Cast<VsDocument>().Where(d => !d.IsSourceCode);

                return sourceFiles.Where(s =>
                {
                    //If we are excluding external folders just check for the extension.
                    if (!excludeKnownExternalFolders) return s.Name.EndsWith(extension);

                    //Checking to make sure the file is not in the excluded list.
                    var documentPath = s.Path;
                    if (string.IsNullOrEmpty(documentPath)) return false;
                    return !documentPath.ToLower().Contains("\\content\\") && s.Name.EndsWith(extension);
                }).ToImmutableList();
        }

        /// <summary>
        /// Returns a list of non-source code documents from VsProject that have a matching extension.
        /// </summary>
        /// <param name="source">The list of visual studio models to search for documents in.</param>
        /// <param name="projectDirectory">The fully qualified path to the project directory.</param>
        /// <param name="extension">The file extension to search for</param>
        /// <param name="excludeKnownExternalFolders">Flag that determines if a content filter should be applied.</param>
        /// <returns>List of documents that meet the criteria.</returns>
        public static IReadOnlyList<VsDocument> GetDocumentsWithExtension(this IReadOnlyList<VsModel> source, string projectDirectory, string extension, bool excludeKnownExternalFolders)
        {
            //If no source is found return an empty list.
            if (source == null) return ImmutableList<VsDocument>.Empty;

            //If no file extension is provided return an empty list.
            if (string.IsNullOrEmpty(extension)) return ImmutableList<VsDocument>.Empty;

            List<VsDocument> result = new List<VsDocument>();

            //Making sure we start with a period for the extension for searching purposes.
            if (!extension.StartsWith(".")) extension = $".{extension}";

            //Filtering out to just 
            var sourceFiles = source.Where(p => p.ModelType.Equals(VisualStudioModelType.Document))
                .Cast<VsDocument>().Where(d => !d.IsSourceCode);

            return sourceFiles.Where(s =>
            {
                //If we are excluding external folders just check for the extension.
                if (!excludeKnownExternalFolders) return s.Name.EndsWith(extension);

                //Checking to make sure the file is not in the excluded list.
                var documentPath = s.Path;
                if (string.IsNullOrEmpty(documentPath)) return false;
                return !documentPath.ToLower().Contains("\\content\\") && s.Name.EndsWith(extension);
            }).ToImmutableList();
        }

        /// <summary>
        /// Extension method that copies a <see cref="VsDocument"/> from a source project to a target location in a supplied destination directory.
        /// Will replace the source project directory path with a new root destination path.
        /// This will overwrite the existing file.
        /// </summary>
        /// <param name="source">The document to be copied</param>
        /// <param name="sourceProjectDirectory">The source project directory to be replaced.</param>
        /// <param name="rootDestinationDirectory">The new target destination path for the file.</param>
        /// <returns>Null if the file was not copied, or the fully qualified path where the file was copied to.</returns>
        public static string CopyProjectFile(this VsDocument source, string sourceProjectDirectory,
            string rootDestinationDirectory)
        {
            //Bounds checking to make sure all data has been passed in correctly. If not return null.
            if (source == null) return null;
            if (string.IsNullOrEmpty(sourceProjectDirectory)) return null;
            if (string.IsNullOrEmpty(rootDestinationDirectory)) return null;

            //Setting the result variable.
            string result = null;

            try
            {
                //Loading the source file path from the visual studio document.
                var sourceFile = source.Path;

                //Replacing the source path with the target destination directory. 
                var destinationFile = sourceFile.Replace(sourceProjectDirectory, rootDestinationDirectory);

                //Making sure the directory already exists in the target project, if it does not go ahead and add it to the project.
                var destinationDirectory = Path.GetDirectoryName(destinationFile);

                if (string.IsNullOrEmpty(destinationDirectory)) return null;

                if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

                //Copying the project file to the new project.
                File.Copy(sourceFile,destinationFile,true);

                //Returning the new file location of the project file in the new project.
                result = destinationFile;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                //An error occurred we are going to swallow the exception and return a null return type.
                return null;
            }

            return result;
        }


        /// <summary>
        /// Gets the c# source code files from a target provided lists.
        /// </summary>
        /// <param name="source">The source list of files.</param>
        /// <param name="excludeKnownExternalFolders">Flag that determines if target files by directory location are excluded.</param>
        /// <returns>List of the found files.</returns>
        public static IReadOnlyList<VsCSharpSource> GetSourceCodeDocumentsAsync(this IReadOnlyList<VsModel> source, bool excludeKnownExternalFolders)
        {

            string extension = ".cs";
            var sourceFiles = source.Where(p => p.ModelType.Equals(VisualStudioModelType.CSharpSource)).Cast<VsCSharpSource>();

            var result = sourceFiles.Where(s =>
            {
                if (excludeKnownExternalFolders)
                {
                    var folderChain = s.SourceCode.SourceDocument;

                    //repeat this section for anything else that might qualify.  This is an attempt to give the caller an option
                    //to *not* bring over bootstrap artifacts or the like.
                    if (folderChain.ToLower().Contains("\\app_data\\")) return false;
                    if (folderChain.ToLower().Contains("\\app_start\\")) return false;
                    if (folderChain.ToLower().Contains("\\app_readme\\")) return false;
                    if (folderChain.ToLower().Contains("\\content\\")) return false;
                }

                if (s.Name.EndsWith(extension)) return true;
                return false;
            }).ToImmutableList();
            
            return result;
        }

        /// <summary>
        /// Gets the immediate VsProject Folder objects that this Document object lives in.  
        /// This list is in reverse order from leaf-to-trunk. An empty list is returned if the document lives in the root of the project.
        /// </summary>
        /// <param name="sourceDocument">The visual studio document to search for.</param>
        /// <returns>List of the the parent <see cref="VsProjectFolder"/> found.</returns>
        public static async Task<List<VsProjectFolder>> GetParentFolders(this VsDocument sourceDocument)
        {
            //Stores the list of parents being returned.
            List<VsProjectFolder> folderHierarchy = new List<VsProjectFolder>();

            //Getting the parent of the source document.
            var parentModel = await sourceDocument.GetParentAsync();

            //If no parent was found return the empty list.
            if (parentModel == null) return folderHierarchy;

            //Climb back up the file and folders until you get to the hosting project.
            while (!parentModel.ModelType.Equals(VisualStudioModelType.Project))
            {
                if (parentModel.ModelType.Equals(VisualStudioModelType.ProjectFolder))
                {
                    //Casting the model to the project folder.
                    var parentFolder = parentModel as VsProjectFolder;

                    //checking to make sure the cast ran clean.
                    if (parentFolder == null) return folderHierarchy;

                    //Adding the parent folder to the list to be returned
                    folderHierarchy.Add(parentFolder);

                    //Getting the next parent and confirming it was found.
                    parentModel = await (parentFolder).GetParentAsync();
                    if (parentModel == null) return folderHierarchy;
                }
                else
                {
                    //Casting to the parent document model.
                    var parentDocument = parentModel as VsDocument;

                    //If the cast failed return what was found.
                    if (parentDocument == null) return folderHierarchy;

                    //Getting the next parent and confirming it was found.
                    parentModel = await (parentDocument).GetParentAsync();
                    if (parentModel == null) return folderHierarchy;
                }

            }

            //Returning the found parent models.
            return folderHierarchy;

        }

        /// <summary>
        /// Confirms the target project folder exists in the project, if not will create it.
        /// </summary>
        /// <param name="projectFolder">The source project folder to check.</param>
        /// <param name="folderName">The name of the folder to create or return if it exists.</param>
        /// <returns>The target project folder.</returns>
        public static async Task<VsProjectFolder> CheckAddFolder(this VsProjectFolder projectFolder, string folderName)
        {

            //Call CodeFactory API and get the children of the project folder.
            var projectFolders = await projectFolder.GetChildrenAsync(false);

            //Search for the project folder to confirm it exists, if not create it and return the created folder.
            return projectFolders.Where(m => m.Name.Equals(folderName)).Cast<VsProjectFolder>().FirstOrDefault() ??
                               await projectFolder.AddProjectFolderAsync(folderName);

        }

        /// <summary>
        /// Extension method that runs an async call from a sync thread.
        /// </summary>
        /// <param name="source">Target C# method to evaluate.</param>
        /// <returns>The content of the the method body.</returns>
        public static String MethodContent(this CsMethod source)
        {
            var taskObj = Task.Run(async () => await source.GetBodySyntaxAsync());
            return taskObj.Result;
        }
    }
}
