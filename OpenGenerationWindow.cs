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

            var dbContextClases = FindDbContexNames(mainItems, new string[] { "DbContext", "IdentityDbContext" });
            var modelsFolder = FindModelsFolder(mainItems, "Models");
            var models = GetModelsNamesFromFolder(modelsFolder);

            System.Windows.Window window = CreateGeneratorWindow();

            var wpfWindowControl = new GeneratorWindow(window);
            wpfWindowControl.InitializeDbContextComboBoxWithItems(dbContextClases);
            wpfWindowControl.InitializeModelsComboBoxWithItems(models);

            window.Content = wpfWindowControl;
            var result = window.ShowDialog();
            if (result == true)
            {
                string modelFullName = wpfWindowControl.ModelsComboBox.Text;

                string dbContextFullPath = wpfWindowControl.DbContextComboBox.Text;
                string[] dbContextSplittedPath = dbContextFullPath.Split('.');
                var dbContextFile = FindDbContextFile(mainItems, dbContextSplittedPath[dbContextSplittedPath.Length - 2], dbContextSplittedPath[dbContextSplittedPath.Length - 1]);

                var dbSetName = GetDbsetName(dbContextFile, modelFullName);

                CreateFolder("Services");
                CreateFolder("Repositories");

                string modelName = modelFullName.Split('.')[modelFullName.Split('.').Length - 1];

                CreateInterface("Service", "Services", modelName);
                CreateInterface("Repository", "Repositories", modelName);

                CreateRepositoryClass("Repository", "Repositories", modelFullName, dbContextFullPath, dbSetName);
                CreateServiceClass("Service", "Services", modelFullName, dbContextFullPath, dbSetName);
            }


            //CreateClass("Service", "Services", "ConsoleApp3.Models.User", "ConsoleApp3.DAL.AppDbContext");
            //CreateRepositoryClass("Repository", "Repositories", "ConsoleApp3.Models.User", "ConsoleApp3.DAL.AppDbContext", "Users");
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

            if(folder.ProjectItems.Item($"I{modelName}{fileName}.cs") != null)
            {
                return;
            }

            ProjectItem newInterfaceFile = folder.ProjectItems.AddFromTemplate(System.IO.Path.Combine(folder.Properties.Item("FullPath").Value.ToString(), $"I{modelName}{fileName}.cs"),
                $"I{modelName}{fileName}.cs");

            
            CodeNamespace actualNamespace = newInterfaceFile.FileCodeModel.AddNamespace($"{GetSolutionName()}.{folderName}.Interfaces", -1);

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

        private void CreateRepositoryClass(string fileName, string folderName, string modelFullName, string dbContextFullName, string dbSetName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string modelName = modelFullName.Split('.')[modelFullName.Split('.').Length - 1];
            string modelFolderName = System.IO.Path.GetFileNameWithoutExtension(modelFullName);
            string modelParametrName = char.ToLower(modelName[0]) + modelName.Substring(1);
            string idParameterName = $"{modelParametrName}Id";


            string dbContextName = dbContextFullName.Split('.')[dbContextFullName.Split('.').Length - 1];
            string dbContextFolderName = System.IO.Path.GetFileNameWithoutExtension(dbContextFullName);

            ProjectItem folder = _dte.Solution.Projects.Item(1).ProjectItems.Item(folderName);
            if (folder.ProjectItems.Item($"{modelName}{fileName}.cs") != null)
            {
                return;
            }

            ProjectItem newClassFile = folder.ProjectItems.AddFromTemplate(System.IO.Path.Combine(folder.Properties.Item("FullPath").Value.ToString(), $"{modelName}{fileName}.cs"),
                    $"{modelName}{fileName}.cs");

            CodeNamespace actualNamespace = newClassFile.FileCodeModel.AddNamespace($"{GetSolutionName()}.{folderName}", -1);

            if (dbContextFolderName.Equals(modelFolderName))
            {
                AddUsings(newClassFile.FileCodeModel.CodeElements.Item(1).StartPoint, new string[] { "System", modelFolderName, $"{GetSolutionName()}.{folderName}.Interfaces",
                    "System.Collections.Generic", "System.Linq" });
            }
            else
            {
                AddUsings(newClassFile.FileCodeModel.CodeElements.Item(1).StartPoint, new string[] { "System", modelFolderName, dbContextFolderName, 
                    $"{GetSolutionName()}.{folderName}.Interfaces", "System.Collections.Generic", "System.Linq" });
            }

            CodeClass newClass = actualNamespace.AddClass($"{modelName}{fileName}", -1, null, $"I{modelName}{fileName}", vsCMAccess.vsCMAccessPublic);
            
            //Db Context property
            CodeVariable context = newClass.AddVariable("_context", dbContextName, -1, vsCMAccess.vsCMAccessPrivate, null);
            EditPoint ep = context.StartPoint.CreateEditPoint();
            ep.CharRight(7);
            ep.Insert(" readonly ");

            //Constructor
            CodeFunction constructor = newClass.AddFunction($"{modelName}{fileName}", vsCMFunction.vsCMFunctionConstructor, -1, vsCMAccess.vsCMAccessPublic);
            constructor.AddParameter("context", dbContextName, -1);
            AddCodeToFunction(constructor, "\t\t\t_context = context;\n");

            //Create
            CodeFunction createFunction = newClass.AddFunction($"Create{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            createFunction.AddParameter(modelParametrName, modelName, -1);
            string code = $"\t\t\t_context.{dbSetName}.Add({modelParametrName});\n\t\t\t_context.SaveChanges();";
            AddCodeToFunction(createFunction, code);

            //Get by id
            CodeFunction getByIdFunction = newClass.AddFunction($"Get{modelName}ById", vsCMFunction.vsCMFunctionFunction, modelName, -1, vsCMAccess.vsCMAccessPublic);
            getByIdFunction.AddParameter(idParameterName, "Guid", -1);
            code = $"\t\t\treturn _context.{dbSetName}.Where(x => x.Id == {idParameterName}).FirstOrDefault();";
            AddCodeToFunction(getByIdFunction, code);

            //Get all
            CodeFunction getAllFunction = newClass.AddFunction($"GetAll", vsCMFunction.vsCMFunctionFunction, $"IEnumerable<{modelName}>", -1, vsCMAccess.vsCMAccessPublic);
            code = $"\t\t\treturn _context.{dbSetName}.Where(_ => true);";
            AddCodeToFunction(getAllFunction, code);

            //Update
            CodeFunction updateFunction = newClass.AddFunction($"Update{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            updateFunction.AddParameter(modelParametrName, modelName, -1);
            code = $"\t\t\t_context.{dbSetName}.Update({modelParametrName});\n\t\t\t_context.SaveChanges();";
            AddCodeToFunction(updateFunction, code);

            //Delete
            CodeFunction deleteFunction = newClass.AddFunction($"Delete{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            deleteFunction.AddParameter(idParameterName, "Guid", -1);
            code = $"\t\t\tvar {modelParametrName} = Get{modelName}ById({idParameterName});\n\t\t\t_context.{dbSetName}.Remove({modelParametrName});\n\t\t\t_context.SaveChanges();";
            AddCodeToFunction(deleteFunction, code);
        }

        private void CreateServiceClass(string fileName, string folderName, string modelFullName, string dbContextFullName, string dbSetName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string modelName = modelFullName.Split('.')[modelFullName.Split('.').Length - 1];
            string modelFolderName = System.IO.Path.GetFileNameWithoutExtension(modelFullName);
            string modelParametrName = char.ToLower(modelName[0]) + modelName.Substring(1);
            string idParameterName = $"{modelParametrName}Id";


            string dbContextName = dbContextFullName.Split('.')[dbContextFullName.Split('.').Length - 1];
            string dbContextFolderName = System.IO.Path.GetFileNameWithoutExtension(dbContextFullName);

            ProjectItem folder = _dte.Solution.Projects.Item(1).ProjectItems.Item(folderName);
            if (folder.ProjectItems.Item($"{modelName}{fileName}.cs") != null)
            {
                return;
            }

            ProjectItem newClassFile = folder.ProjectItems.AddFromTemplate(System.IO.Path.Combine(folder.Properties.Item("FullPath").Value.ToString(), $"{modelName}{fileName}.cs"),
                $"{modelName}{fileName}.cs");

            CodeNamespace actualNamespace = newClassFile.FileCodeModel.AddNamespace($"{GetSolutionName()}.{folderName}", -1);


            if (dbContextFolderName.Equals(modelFolderName))
            {
                AddUsings(newClassFile.FileCodeModel.CodeElements.Item(1).StartPoint, new string[] { "System", modelFolderName, $"{GetSolutionName()}.{folderName}.Interfaces",
                    $"{GetSolutionName()}.Repositories.Interfaces", "System.Collections.Generic", "System.Linq" });
            }
            else
            {
                AddUsings(newClassFile.FileCodeModel.CodeElements.Item(1).StartPoint, new string[] { "System", modelFolderName, dbContextFolderName,
                    $"{GetSolutionName()}.{folderName}.Interfaces", $"{GetSolutionName()}.Repositories.Interfaces", "System.Collections.Generic", "System.Linq" });
            }


            CodeClass newClass = actualNamespace.AddClass($"{modelName}{fileName}", -1, null, $"I{modelName}{fileName}", vsCMAccess.vsCMAccessPublic);

            //Repository property
            CodeVariable repository = newClass.AddVariable($"_{modelParametrName}Repository", $"I{modelName}Repository", -1, vsCMAccess.vsCMAccessPrivate, null);
            EditPoint epRepo = repository.StartPoint.CreateEditPoint();
            epRepo.CharRight(7);
            epRepo.Insert(" readonly ");

            //Constructor
            CodeFunction constructor = newClass.AddFunction($"{modelName}{fileName}", vsCMFunction.vsCMFunctionConstructor, null, -1, vsCMAccess.vsCMAccessPublic);
            constructor.AddParameter($"{modelParametrName}Repository", $"I{modelName}Repository", -1);
            AddCodeToFunction(constructor, $"\t\t\t_{modelParametrName}Repository = {modelParametrName}Repository;\n");

            //Create
            CodeFunction createFunction = newClass.AddFunction($"Create{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            createFunction.AddParameter(modelParametrName, modelName, -1);
            string code = $"\t\t\t_{modelParametrName}Repository.Create{modelName}({modelParametrName});";
            AddCodeToFunction(createFunction, code);

            //Get by id
            CodeFunction getByIdFunction = newClass.AddFunction($"Get{modelName}ById", vsCMFunction.vsCMFunctionFunction, modelName, -1, vsCMAccess.vsCMAccessPublic);
            getByIdFunction.AddParameter(idParameterName, "Guid", -1);
            code = $"\t\t\treturn _{modelParametrName}Repository.Get{modelName}ById({idParameterName});";
            AddCodeToFunction(getByIdFunction, code);

            //Get all
            CodeFunction getAllFunction = newClass.AddFunction($"GetAll", vsCMFunction.vsCMFunctionFunction, $"IEnumerable<{modelName}>", -1, vsCMAccess.vsCMAccessPublic);
            code = $"\t\t\treturn _{modelParametrName}Repository.GetAll();";
            AddCodeToFunction(getAllFunction, code);

            //Update
            CodeFunction updateFunction = newClass.AddFunction($"Update{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            updateFunction.AddParameter(modelParametrName, modelName, -1);
            code = $"\t\t\t_{modelParametrName}Repository.Update{modelName}({modelParametrName});";
            AddCodeToFunction(updateFunction, code);

            //Delete
            CodeFunction deleteFunction = newClass.AddFunction($"Delete{modelName}", vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1, vsCMAccess.vsCMAccessPublic);
            deleteFunction.AddParameter(idParameterName, "Guid", -1);
            code = $"\t\t\t_{modelParametrName}Repository.Delete{modelName}({idParameterName});";
            AddCodeToFunction(deleteFunction, code);
        }

        private void AddCodeToFunction(CodeFunction function, string code)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EditPoint editPoint = function.GetStartPoint(vsCMPart.vsCMPartBody).CreateEditPoint();
            editPoint.Insert(code);
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

        private List<string> FindDbContexNames(ProjectItems items, string[] dbContextBaseClassNames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<string> dbContexts = new List<string>();

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
                                    foreach(string dbContextName in dbContextBaseClassNames)
                                    {
                                        if (((CodeClass)classElement).Bases.Item(1).Name.ToLower().Equals(dbContextName.ToLower()))
                                        {
                                            dbContexts.Add(((CodeClass)classElement).FullName);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    List<string> contexts = FindDbContexNames(item.ProjectItems, dbContextBaseClassNames);
                    if(contexts.Count > 0)
                    {
                        dbContexts.AddRange(contexts);
                    }
                }
            }
            return dbContexts;
        }

        private ProjectItem FindDbContextFile(ProjectItems items, string folderName, string dbContextName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in items)
            {
                if (item.Name.ToLower().Equals(folderName.ToLower()))
                {
                    return item.ProjectItems.Item($"{dbContextName}.cs");
                }
                else
                {
                    ProjectItem projectItem = FindDbContextFile(item.ProjectItems, folderName, dbContextName);
                    if (projectItem != null)
                    {
                        return projectItem.ProjectItems.Item($"{dbContextName}.cs");
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

            foreach (ProjectItem item in modelsFolder.ProjectItems)
            {
                if (item.FileCodeModel != null)
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

        private string GetDbsetName(ProjectItem dbContextFile, string modelName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (CodeElement codeElem in dbContextFile.FileCodeModel.CodeElements)
            {
                if (codeElem.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement namespaceElem in ((CodeNamespace)codeElem).Members)
                    {
                        if (namespaceElem.Kind == vsCMElement.vsCMElementClass)
                        {
                            foreach (CodeElement classElem in ((CodeClass)namespaceElem).Members)
                            {
                                string typeName = null;
                                if (classElem.Kind == vsCMElement.vsCMElementProperty)
                                {
                                    typeName = ((CodeProperty)classElem).Type.AsFullName;
                                }
                                else if (classElem.Kind == vsCMElement.vsCMElementVariable)
                                {
                                    typeName = ((CodeVariable)classElem).Type.AsFullName;
                                }
                                if (typeName != null && typeName.Contains($"DbSet<{modelName}>"))
                                {
                                    return classElem.Name;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
