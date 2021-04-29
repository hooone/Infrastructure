using SharpPdb.Windows;
using SharpPdb.Windows.DebugSubsections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Code
{
    public class AssemblyInfo
    {
        public string AssemblyName { get; set; }
        public string Message { get; set; }
        public DateTime LastModifyTime { get; set; }
        public List<ClassInfo> ClassList { get; set; }
        private string AssemblyPath { get; }
        private string PdbPath { get; }
        public AssemblyInfo(string projPath)
        {
            if (string.IsNullOrWhiteSpace(projPath))
            {
                return;
            }
            if (!projPath.ToUpper().Contains(".CSPROJ"))
            {
                return;
            }
            string proj = File.ReadAllText(projPath).Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("	", "").ToUpper();
            var groups = Regex.Matches(proj, @"<PROPERTYGROUP[\S\s]*?</PROPERTYGROUP>");
            string exePath = "";
            foreach (var grp in groups)
            {
                if (grp.ToString().Contains(@"<DEBUGTYPE>FULL</DEBUGTYPE>"))
                {
                    exePath = Regex.Match(grp.ToString(), @"<OUTPUTPATH>[\S\s]*?</OUTPUTPATH>").ToString().Replace("<OUTPUTPATH>", "").Replace("</OUTPUTPATH>", "");
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(exePath))
            {
                return;
            }
            string filename = Regex.Match(proj, @"<ASSEMBLYNAME>[\S\s]*?</ASSEMBLYNAME>").ToString().Replace("<ASSEMBLYNAME>", "").Replace("</ASSEMBLYNAME>", "");
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }
            AssemblyName = filename;
            string suffix = Regex.Match(proj, @"<OUTPUTTYPE>[\S\s]*?</OUTPUTTYPE>").ToString().Replace("<OUTPUTTYPE>", "").Replace("</OUTPUTTYPE>", "");
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return;
            }
            suffix = suffix.Replace("LIBRARY", "DLL");
            suffix = suffix.Replace("WINEXE", "EXE");
            FileInfo projFile = new FileInfo(projPath);
            string dirPath = projFile.Directory.FullName;
            string exepath = Path.Combine(dirPath, exePath, filename + "." + suffix);
            AssemblyPath = exepath;
            FileInfo exeFile = new FileInfo(exepath);
            if (!exeFile.Exists)
            {
                return;
            }
            DirectoryInfo exportDir = new DirectoryInfo(Path.Combine(dirPath, exePath));
            PdbPath = Path.Combine(dirPath, exePath, filename + ".pdb");
            ClassList = new List<ClassInfo>();
        }
        public string Load()
        {
            if (string.IsNullOrWhiteSpace(AssemblyPath))
            {
                this.Message = "操作失败，未能找到编译后的文件，请重新生成。";
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }
            FileInfo assemFile = new FileInfo(AssemblyPath);
            if (!assemFile.Exists)
            {
                this.Message = "操作失败，未能找到编译后的文件，请重新生成。";
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }
            LastModifyTime = assemFile.LastWriteTime;
            FileInfo pdbFile = new FileInfo(PdbPath);
            if (!assemFile.Exists)
            {
                this.Message = "操作失败，未能找到.pdb文件，打开debug-full模式。";
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }

            // 加载pdb
            PdbFile Pdb = new PdbFile(PdbPath);
            try
            {
                Assembly asm = Assembly.LoadFrom(AssemblyPath);
                foreach (var typ in asm.DefinedTypes)
                {
                    var cls = new ClassInfo();
                    cls.Name = typ.Name;
                    cls.FullName = typ.FullName;
                    cls.NameSpace = typ.Namespace;
                    foreach (var item in typ.CustomAttributes)
                    {
                        var attr = new AttributeInfo();
                        attr.Description = item.ToString();
                        attr.TypeName = item.AttributeType.Name;
                        attr.TypeFullName = item.AttributeType.FullName;
                        foreach (var arg in item.ConstructorArguments)
                        {
                            attr.ArgumentList.Add(arg.Value.ToString());
                        }
                        cls.AttributeList.Add(attr);
                    }
                    foreach (var prop in typ.DeclaredProperties)
                    {
                        var property = new PropertyInfo();
                        property.Name = prop.Name;
                        property.Type = prop.PropertyType.FullName;
                        foreach (var item in prop.CustomAttributes)
                        {

                            var attr = new AttributeInfo();
                            attr.Description = item.ToString();
                            attr.TypeName = item.AttributeType.Name;
                            attr.TypeFullName = item.AttributeType.FullName;
                            foreach (var arg in item.ConstructorArguments)
                            {
                                attr.ArgumentList.Add(arg.Value.ToString());
                            }

                            property.AttributeList.Add(attr);
                        }
                        cls.PropertyList.Add(property);
                    }
                    // 从pdb文件中取出代码路径和位置
                    foreach (var module in Pdb.DbiStream.Modules)
                    {
                        if (module.ModuleName.String == cls.FullName)
                        {
                            cls.FilePath = module.Files.ToList();
                            if (cls.FilePath.Count > 1)
                            {
                                continue;
                            }
                            var lines = module.DebugSubsectionStream[DebugSubsectionKind.Lines].OfType<LinesSubsection>().ToArray();
                            uint min = 999999;
                            uint max = 0;
                            foreach (var lin in lines)
                            {
                                var elins = lin.Files[0].Lines;
                                foreach (var elin in elins)
                                {
                                    min = Math.Min(min, elin.LineStart);
                                    max = Math.Max(max, elin.LineEnd);
                                }
                            }
                            if (min < max)
                            {
                                cls.MinLine = min;
                                cls.MaxLine = max;
                            }
                        }
                    }
                    ClassList.Add(cls);
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }
            catch (Exception e)
            {
                this.Message = e.Message + Environment.NewLine + e.StackTrace;
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }
            finally
            {
                Pdb.Dispose();
            }
        }
    }
}
