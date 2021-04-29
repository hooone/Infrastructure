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

            VsShellUtilities.ShowMessageBox(
             this.package,
             "step1",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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

            VsShellUtilities.ShowMessageBox(
             this.package,
             "step2",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            // 获得项目信息
            AssemblyInfo assembly = new AssemblyInfo(projPath);
            VsShellUtilities.ShowMessageBox(
             this.package,
             "step3",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            string loadRst = assembly.Load();
            if (!string.IsNullOrWhiteSpace(loadRst))
            {
                VsShellUtilities.ShowMessageBox(
                     this.package,
                     loadRst,
                     "生成失败",
                     OLEMSGICON.OLEMSGICON_INFO,
                     OLEMSGBUTTON.OLEMSGBUTTON_OK,
                     OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            VsShellUtilities.ShowMessageBox(
             this.package,
             "step4",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            VsShellUtilities.ShowMessageBox(
             this.package,
             "step5",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            // 获得项目文件并检查时间
            List<string> csFiles = new List<string>();
            List<string> iniFiles = new List<string>();
            {
                var proj = File.ReadAllText(projPath).ToUpper().Replace(" ", "").Replace("\r", "").Replace("\n", "");
                FileInfo fileInfo = new FileInfo(projPath);
                string dir = fileInfo.Directory.FullName;
                var files = Regex.Matches(proj, @"INCLUDE" + @"[\S\s]*?" + "=" + @"[\S\s]*?" + "\"" + @"[\S\s]*?" + "\"");
                foreach (var item in files)
                {
                    if (item.ToString().EndsWith(".CS\""))
                    {
                        var file = Regex.Match(item.ToString(), "\"" + @"[\S\s]*?" + "\"").ToString().Replace("\"", "");
                        var filePath = Path.Combine(dir, file);
                        FileInfo csInfo = new FileInfo(filePath);
                        if (csInfo.LastWriteTime > assembly.LastModifyTime)
                        {
                            VsShellUtilities.ShowMessageBox(
                             this.package,
                             "代码与编译后的文件不匹配，请重新生成项目。",
                             "生成失败",
                             OLEMSGICON.OLEMSGICON_INFO,
                             OLEMSGBUTTON.OLEMSGBUTTON_OK,
                             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            return;
                        }
                        csFiles.Add(filePath);
                    }
                    else if (item.ToString().EndsWith(".INI\""))
                    {
                        var file = Regex.Match(item.ToString(), "\"" + @"[\S\s]*?" + "\"").ToString().Replace("\"", "");
                        var filePath = Path.Combine(dir, file);
                        FileInfo csInfo = new FileInfo(filePath);
                        if (csInfo.LastWriteTime > assembly.LastModifyTime)
                        {
                            VsShellUtilities.ShowMessageBox(
                             this.package,
                             "代码与编译后的文件不匹配，请重新生成项目。",
                             "生成失败",
                             OLEMSGICON.OLEMSGICON_INFO,
                             OLEMSGBUTTON.OLEMSGBUTTON_OK,
                             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            return;
                        }
                        iniFiles.Add(filePath);
                    }
                }
            }
            if (csFiles.Count == 0)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "未找到CS文件",
                    "生成失败",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            VsShellUtilities.ShowMessageBox(
             this.package,
             "step6",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            // 搜索数据库连接字符串
            string connectionDB = "";
            string connectionUser = "";
            string connectionPass = "";
            {
                foreach (var cls in assembly.ClassList)
                {

                    foreach (var prop in cls.PropertyList)
                    {
                        foreach (var attr in prop.AttributeList)
                        {
                            if (attr.TypeFullName == typeof(DbConnectionString).FullName && attr.ArgumentList.Count == 3)
                            {
                                connectionDB = attr.ArgumentList[0];
                                connectionUser = attr.ArgumentList[1];
                                connectionPass = attr.ArgumentList[2];
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(connectionDB))
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "未找到数据库连接方式",
                    "生成失败",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            VsShellUtilities.ShowMessageBox(
             this.package,
             "step7",
             "step",
             OLEMSGICON.OLEMSGICON_INFO,
             OLEMSGBUTTON.OLEMSGBUTTON_OK,
             OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            // 连接数据库
            string strConn = "Data Source = " + connectionDB + "; User ID = " + connectionUser + "; Password = " + connectionPass + ";Connection Lifetime=3;Connection Timeout=3;";
            var oracleConnection = new OracleConnection(strConn);
            try
            {
                oracleConnection.KeepAliveTime = 1000;
                oracleConnection.Open();
            }
            catch
            {
                VsShellUtilities.ShowMessageBox(
                 this.package,
                 strConn,
                 "生成失败-连接数据库失败",
                 OLEMSGICON.OLEMSGICON_INFO,
                 OLEMSGBUTTON.OLEMSGBUTTON_OK,
                 OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // 生成dbmodel代码
            int count = 0;
            int fail = 0;
            {
                foreach (var cls in assembly.ClassList)
                {
                    if (!cls.AttributeList.Exists(f => f.TypeFullName == typeof(DbTable).FullName))
                    {
                        continue;
                    }
                    count++;
                    if (cls.FilePath.Count > 1)
                    {
                        fail++;
                        continue;
                    }

                    StringBuilder newFile = new StringBuilder();
                    string oldFile = File.ReadAllText(cls.FilePath[0]);
                    var className = Regex.Match(oldFile, @"class[\s\n]*?" + cls.Name);
                    if (!className.Success)
                    {
                        fail++;
                        continue;
                    }
                    int begin = className.Index + className.Length;
                    bool keep = true;
                    int signLevel = 0;
                    // 扫描文件
                    for (int i = 0; i < oldFile.Length; i++)
                    {
                        if (i == begin)
                        {
                            keep = false;
                            signLevel = 0;
                            // 重写class
                            newFile.AppendLine();
                            newFile.AppendLine("    {");
                            //// 填入字段
                            //string sql = "select t.table_name,t.column_name,t.data_type,t.data_length,t.nullable,c.comments " +
                            //    "from user_tab_columns t,user_col_comments c " +
                            //    "where t.column_name = c.column_name " +
                            //    "and t.table_name = c.table_name " +
                            //    "and t.Table_Name ='" + tbName + "' " +
                            //    " order by t.Table_Name,t.column_id";
                            //// 调试
                            //VsShellUtilities.ShowMessageBox(
                            //    this.package,
                            //    sql,
                            //    tbName,
                            //    OLEMSGICON.OLEMSGICON_INFO,
                            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            //DataSet dSet = new DataSet();
                            //OracleDataAdapter oda = new OracleDataAdapter(sql, oracleConnection);
                            //oda.Fill(dSet);
                            //DataTable dTable = dSet.Tables[0];
                            //foreach (DataRow row in dTable.Rows)
                            //{
                            //    if (!string.IsNullOrWhiteSpace(row["COMMENTS"].ToString()))
                            //    {
                            //        newFile.AppendLine("        /// <summary>");
                            //        newFile.AppendLine("        /// " + row["COMMENTS"].ToString());
                            //        newFile.AppendLine("        /// </summary>");
                            //    }
                            //    newFile.AppendLine("        [DbColumn(DataType." + row["DATA_TYPE"].ToString().Replace("(", "_").Replace(")", "") + ")]");
                            //    if (row["DATA_TYPE"].ToString() == "NUMBER")
                            //    {
                            //        newFile.AppendLine("        public int " + row["COLUMN_NAME"] + " { get; set; }");
                            //    }
                            //    else
                            //    {
                            //        newFile.AppendLine("        public string " + row["COLUMN_NAME"] + " { get; set; }");
                            //    }
                            //    newFile.AppendLine("");
                            //}
                            newFile.AppendLine("    }");
                        }

                        // 删除原有class的所有内容
                        if (!keep)
                        {
                            if (oldFile[i] == '{')
                            {
                                signLevel++;
                            }
                            if (oldFile[i] == '}')
                            {
                                signLevel--;
                                // class结束
                                if (signLevel <= 0)
                                {
                                    keep = true;
                                }
                            }
                        }

                        if (keep)
                        {
                            newFile.Append(oldFile[i]);
                        }
                    }
                    File.WriteAllText(cls.FilePath[0], newFile.ToString());
                }
            }
            VsShellUtilities.ShowMessageBox(
                this.package,
                "共找到" + count.ToString() + "个文件" + (fail == 0 ? "" : (",失败" + fail + "个文件")) + "!",
                "代码生成成功",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }
    }
}
