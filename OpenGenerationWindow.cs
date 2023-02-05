using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Task = System.Threading.Tasks.Task;

namespace RepoServiceGenerator
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenGenerationWindow
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("71fa3c73-fbc6-40df-bef0-f6660d170b7d");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE2 _dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenGenerationWindow"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private OpenGenerationWindow(AsyncPackage package, OleMenuCommandService commandService)
        {
            this._dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenGenerationWindow Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in OpenGenerationWindow's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenGenerationWindow(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectItems mainItems = _dte.Solution.Projects.Item(1).ProjectItems;

            var dbContextClass = FindDbContex(mainItems, "User");
            var modelsFolder = FindModelsFolder(mainItems, "Models");
            var models = GetModelsNamesFromFolder(modelsFolder);

            System.Windows.Window window = CreateGeneratorWindow();

            var wpfWindowControl = new GeneratorWindow(window);
            wpfWindowControl.InitializeDbContextComboBoxWithItems(dbContextClass?.FullName);
            wpfWindowControl.InitializeModelsComboBoxWithItems(models);

            window.Content = wpfWindowControl;
            var result = window.ShowDialog();
            //if (result == true)
            //{
            //CreateFolder("Services");
            //CreateFolder("Repositories");
            //CreateInterface("Service", "Services", "User");
            //CreateInterface("Repository", "Repositories", "User");


            //var text = wpfWindowControl.ModelClass;
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    text,
            //    "Lol",
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            // }
            //string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            //string title = "OpenGenerationWindow";


            //string fullPath = System.IO.Path.Combine(dTE2.Solution.Projects.Item(1).ProjectItems.Item(1).Properties.Item("FullPath").Value.ToString(), "User.cs");
            //var folder = dTE2.Solution.Projects.Item(1).ProjectItems.AddFolder("Services");
            //folder.ProjectItems.AddFromTemplate(System.IO.Path.Combine(folder.Properties.Item("FullPath").Value.ToString(), "User.cs"), "User.cs").FileCodeModel.AddClass("User", -1, null, null, vsCMAccess.vsCMAccessPublic); 
            //ProjectItem projectItem = _dte.Solution.FindProjectItem("User.cs");

            //var codeClass = projectItem.FileCodeModel.AddInterface("User", vsCMAccess.vsCMAccessPublic);

            //codeClass.AddFunction("ToString", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefString, -1, vsCMAccess.vsCMAccessPublic, null);
            //codeClass.AddImport();
        }


        private System.Windows.Window CreateGeneratorWindow()
        {
            System.Windows.Window window = new System.Windows.Window
            {
                Width = 400,
                Height = 500,
                Title = "Repository and Service Generator",
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
            };
            return window;
        }

        private void CreateFolder(string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_dte.Solution.Projects.Item(1).ProjectItems.Item(folderName) == null)
            {
                _dte.Solution.Projects.Item(1).ProjectItems.AddFolder(folderName).ProjectItems.AddFolder("Interfaces");
            }
        }

        private void CreateInterface(string fileName, string folderName, string modelName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string modelParametrName = char.ToLower(modelName[0]) + modelName.Substring(1);
            string idParameterName = $"{modelParametrName}Id";

            ProjectItem folder = _dte.Solution.Projects.Item(1).ProjectItems.Item(folderName).ProjectItems.Item("Interfaces");
            ProjectItem newInterfaceFile = folder.ProjectItems.AddFromTemplate(System.IO.Path.Combine(folder.Properties.Item("FullPath").Value.ToString(), $"I{modelName}{fileName}.cs"),
                $"I{modelName}{fileName}.cs");

            
            CodeNamespace actualNamespace = newInterfaceFile.FileCodeModel.AddNamespace($"{GetSolutionName()}.{folderName}", -1);

            AddUsings(newInterfaceFile.FileCodeModel.CodeElements.Item(1).StartPoint, new string[] { "System", $"{GetSolutionName()}.Models", "System.Collections.Generic" });

            CodeInterface newInterface = actualNamespace.AddInterface($"I{modelName}{fileName}", -1, null, vsCMAccess.vsCMAccessPublic);
            
            //Create
            CodeFunction createFunction = newInterface.AddFunction($"Create{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            createFunction.AddParameter(modelParametrName, modelName, -1);

            //Get by Id
            CodeFunction getByIdFunction = newInterface.AddFunction($"Get{modelName}ById", vsCMFunction.vsCMFunctionFunction, modelName, -1, vsCMAccess.vsCMAccessPublic);
            getByIdFunction.AddParameter(idParameterName, "Guid", -1);

            //Get all
            CodeFunction getAllFunction = newInterface.AddFunction($"GetAll", vsCMFunction.vsCMFunctionFunction, $"IEnumerable<{modelName}>", -1, vsCMAccess.vsCMAccessPublic);

            //Update
            CodeFunction updateFunction = newInterface.AddFunction($"Update{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            updateFunction.AddParameter(modelParametrName, modelName, -1);

            //Delete
            CodeFunction deleteFunction = newInterface.AddFunction($"Delete{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            deleteFunction.AddParameter(idParameterName, "Guid", -1);
        }

        private string GetSolutionName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return System.IO.Path.GetFileNameWithoutExtension(_dte.Solution.FullName);
        }

        private void AddUsings(TextPoint textPoint, string[] usings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EditPoint editPoint = textPoint.CreateEditPoint();
            foreach(string us in usings)
            {
                editPoint.Insert($"using {us};\n");
            }
            editPoint.Insert("\n");
        }

        private CodeClass FindDbContex(ProjectItems items, string dbContextBaseClassName = "DbContext")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)
            {
                if(item.FileCodeModel != null){
                    foreach(CodeElement codeElement in item.FileCodeModel.CodeElements)
                    {
                        if(codeElement.Kind == vsCMElement.vsCMElementNamespace)
                        {
                            foreach(CodeElement classElement in codeElement.Children)
                            {
                                if(classElement.Kind == vsCMElement.vsCMElementClass && ((CodeClass)classElement).Bases.Count > 0)
                                {
                                    if(((CodeClass)classElement).Bases.Item(1).Name.ToLower().Equals(dbContextBaseClassName.ToLower()))
                                    {
                                        return (CodeClass)classElement;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    CodeClass codeClass = FindDbContex(item.ProjectItems, dbContextBaseClassName);
                    if(codeClass != null)
                    {
                        return codeClass;
                    }
                }
            }
            return null;
        }


        private ProjectItem FindModelsFolder(ProjectItems items, string folderName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)
            {
                if(item.Name.ToLower().Equals(folderName.ToLower()))
                {
                    return item;
                }
                else
                {
                    ProjectItem projectItem = FindModelsFolder(item.ProjectItems, folderName);
                    if(projectItem != null)
                    {
                        return projectItem;
                    }
                }
            }
            return null;
        }

        private List<string> GetModelsNamesFromFolder(ProjectItem modelsFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<string> models = new List<string>();

            foreach(ProjectItem item in modelsFolder.ProjectItems)
            {
                if(item.FileCodeModel != null)
                {
                    foreach (CodeElement codeElement in item.FileCodeModel.CodeElements)
                    {
                        if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                        {
                            foreach (CodeElement classElement in codeElement.Children)
                            {
                                if (classElement.Kind == vsCMElement.vsCMElementClass)
                                {
                                    models.Add(((CodeClass)classElement).FullName);
                                }
                            }
                        }
                    }
                }
            }

            return models;
        }
    }
}
