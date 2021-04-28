using SharpPdb.Windows;
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
            if (string.IsNullOrWhiteSpace(AssemblyPath))
            {
                return "操作失败，未能找到编译后的文件，请重新生成。";
            }
            FileInfo assemFile = new FileInfo(AssemblyPath);
            if (!assemFile.Exists)
            {
                return "操作失败，未能找到编译后的文件，请重新生成。";
            }
            FileInfo pdbFile = new FileInfo(PdbPath);
            if (!assemFile.Exists)
            {
                return "操作失败，未能找到.pdb文件，打开debug-full模式。";
            }
            Assembly asm = Assembly.LoadFile(AssemblyPath);
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
                foreach (var module in Pdb.DbiStream.Modules)
                {
                    if (module.ModuleName.String == cls.FullName)
                    {
                        cls.FilePath = module.Files.ToList();
                    }
                }
                ClassList.Add(cls);
            }
            return "";
        }
    }
}
