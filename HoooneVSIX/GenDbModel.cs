using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Infrastructure.Code;
using Infrastructure.DB;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Oracle.ManagedDataAccess.Client;
using Task = System.Threading.Tasks.Task;

namespace HoooneVSIX
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenDbModel
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("5afb8f91-d25b-49cd-9123-3d562a7876cb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenDbModel"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenDbModel(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenDbModel Instance
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
            // Switch to the main thread - the call to AddCommand in GenDbModel's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenDbModel(package, commandService);
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
            // 二次确认弹窗
            var confirm = VsShellUtilities.ShowMessageBox(
                this.package,
                "在此项目中，所有包含DbTable特性的类都将根据数据库结构自动生成代码",
                "生成DbModel",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            if (confirm != 1)
            {
                return;
            }
            // 获得dte
            var dteT = Instance.ServiceProvider.GetServiceAsync(typeof(DTE));
            var dte = (DTE2)dteT.Result;

            // 获得解决方案资源管理器的选中项目
            var selectedItems = dte.ToolWindows.SolutionExplorer.SelectedItems as UIHierarchyItem[];
            if (selectedItems == null || selectedItems.Length == 0)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "未找到选中项目",
                    "生成失败",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            string projectName = selectedItems[0].Name;

            // 获得项目路径
            string projPath = "";
            {
                var sln = File.ReadAllText(dte.Solution.FullName).ToUpper().Replace(" ", "").Replace("\r", "").Replace("\n", "");
                var projs = Regex.Matches(sln, @"PROJECT[\s\S,\n]*?ENDPROJECT");
                foreach (var proj in projs)
                {
                    var projName = Regex.Match(proj.ToString(), @"=[\s\S]*?\" + "\"" + @"[\s\S]*?" + "\"");
                    if (projName == null)
                        continue;
                    string projNameStr = projName.ToString().Replace("=", "").Replace("\"", "");
                    var attrs = Regex.Matches(proj.ToString(), "\"" + @"[\s\S]*?" + "\"");
                    foreach (var attr in attrs)
                    {
                        if (attr.ToString().Contains(".CSPROJ"))
                        {
                            string path = attr.ToString();
                            if (projNameStr == projectName.ToUpper())
                            {
                                FileInfo slnFile = new FileInfo(dte.Solution.FullName);
                                projPath = Path.Combine(slnFile.Directory.FullName, path.ToString().Replace("\"", ""));
                                break;
                            }
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(projPath))
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "未找到项目路径",
                        "生成失败",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }
            }

            // 调用进程生成DbModel
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            FileInfo cur = new FileInfo(this.GetType().Assembly.Location);
            myProcess.StartInfo.FileName = Path.Combine(cur.Directory.FullName, "GenDbModel.exe");
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardInput = true;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.RedirectStandardError = true;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.StartInfo.Arguments = projPath;
            myProcess.Start();
            var str = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();

            VsShellUtilities.ShowMessageBox(
                this.package,
                str,
                "代码生成",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }
    }
}
