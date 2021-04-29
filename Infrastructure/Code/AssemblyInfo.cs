using SharpPdb.Windows;
using SharpPdb.Windows.DebugSubsections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Code
{
    public class AssemblyInfo
    {
        public DateTime LastModifyTime { get; set; }
        public List<ClassInfo> ClassList { get; set; }
        private string AssemblyPath { get; }
        private string PdbPath { get; }
        public AssemblyInfo(string projPath)
        {

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
            string suffix = Regex.Match(proj, @"<OUTPUTTYPE>[\S\s]*?</OUTPUTTYPE>").ToString().Replace("<OUTPUTTYPE>", "").Replace("</OUTPUTTYPE>", "");
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return;
            }
            FileInfo projFile = new FileInfo(projPath);
            string dirPath = projFile.Directory.FullName;
            string exepath = Path.Combine(dirPath, exePath, filename + "." + suffix);
            FileInfo exeFile = new FileInfo(exepath);
            if (!exeFile.Exists)
            {
                return;
            }
            AssemblyPath = exepath;
            PdbPath = Path.Combine(dirPath, exePath, filename + ".pdb");
            ClassList = new List<ClassInfo>();
        }
        public string Load()
        {
            try
            {

                if (string.IsNullOrWhiteSpace(AssemblyPath))
                {
                    return "操作失败，未能找到编译后的文件，请重新生成。";
                }
                FileInfo assemFile = new FileInfo(AssemblyPath);
                if (!assemFile.Exists)
                {
                    return "操作失败，未能找到编译后的文件，请重新生成。";
                }
                LastModifyTime = assemFile.LastWriteTime;
                FileInfo pdbFile = new FileInfo(PdbPath);
                if (!assemFile.Exists)
                {
                    return "操作失败，未能找到.pdb文件，打开debug-full模式。";
                }

                // 临时使用，会导致程序集的内存副本无法释放
                byte[] fileData = File.ReadAllBytes(AssemblyPath);
                Assembly asm = Assembly.Load(fileData);
                PdbFile Pdb = new PdbFile(PdbPath);
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
                    try
                    {
                        foreach (var prop in typ.DeclaredProperties)
                        {
                            var property = new PropertyInfo();
                            property.Name = prop.Name;
                            property.Type = prop.PropertyType;
                            try
                            {

                                foreach (var item in prop.CustomAttributes)
                                {
                                    try
                                    {

                                        var attr = new AttributeInfo();
                                        attr.Description = item.ToString();
                                        attr.TypeName = item.AttributeType.Name;
                                        attr.TypeFullName = item.AttributeType.FullName;
                                        foreach (var arg in item.ConstructorArguments)
                                        {
                                            attr.ArgumentList.Add(arg.Value.ToString());
                                        }
                                    }
                                    catch
                                    {
                                        return "777";
                                    }
                                    //property.AttributeList.Add(attr);
                                }
                                //cls.PropertyList.Add(property);
                            }
                            catch
                            {
                                return "73";
                            }
                        }
                    }
                    catch
                    {
                        return "7";
                    }

                    try
                    {

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
                    }
                    catch
                    {
                        return "8";
                    }
                    ClassList.Add(cls);
                }
                return "999";

            }
            catch (Exception e)
            {
                return e.StackTrace;
            }
        }
    }
}
