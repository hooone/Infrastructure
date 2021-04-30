using Infrastructure.Code;
using Infrastructure.DB;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenDbModel
{
    class Program
    {
        static void Main(string[] args)
        {
            //string projPath = args[0];
            string projPath = @"C:\Users\techsun\Desktop\Infrastructure\Demo\Demo.csproj";

            // 获得项目信息
            var assembly = new AssemblyInfo(projPath);
            var loadRst = assembly.Load();
            if (!string.IsNullOrWhiteSpace(loadRst))
            {
                Console.WriteLine(loadRst);
                return;
            }

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
                            Console.WriteLine("代码与编译后的文件不匹配，请重新生成项目。" + Environment.NewLine +
                             csInfo.FullName + Environment.NewLine +
                             csInfo.LastWriteTime);
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
                            Console.WriteLine("代码与编译后的文件不匹配，请重新生成项目。" + Environment.NewLine +
                            csInfo.FullName + Environment.NewLine +
                            csInfo.LastWriteTime);
                            return;
                        }
                        iniFiles.Add(filePath);
                    }
                }
            }
            if (csFiles.Count == 0)
            {
                Console.WriteLine("未找到CS文件");
                return;
            }

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
                Console.WriteLine("未找到数据库连接方式");
                return;
            }

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
                Console.WriteLine("生成失败-连接数据库失败");
                return;
            }

            var count = 0;
            var fail = 0;
            string failStr = "";
            GenDbModel(assembly, csFiles, oracleConnection, ref count, ref fail, ref failStr);
            GenDbAccess(assembly, csFiles, ref count, ref fail, ref failStr);
            Console.WriteLine("共找到" + count.ToString() + "个文件" + (fail == 0 ? "" : (",失败" + fail + "个文件")) + "!");
            return;
        }


        // 生成dbAccess代码
        private static void GenDbAccess(AssemblyInfo assembly, List<string> csFiles, ref int count, ref int fail, ref string failStr)
        {
            foreach (var cls in assembly.ClassList)
            {
                // 处理DbTable特性
                var attr = cls.AttributeList.FirstOrDefault(f => f.TypeFullName == typeof(DbTable).FullName);
                if (attr == null || attr.ArgumentList.Count == 0)
                {
                    continue;
                }
                if (!cls.Interfaces.Contains(typeof(IDbAccess).FullName))
                {
                    continue;
                }
                count++;

                // 文件路径处理
                if (cls.FilePath.Count > 1)
                {
                    fail++;
                    failStr += ("partial class " + cls.Name + " is not support" + Environment.NewLine);
                    continue;
                }
                if (cls.FilePath.Count == 0)
                {
                    fail++;
                    failStr += ("File of class " + cls.Name + " not found" + Environment.NewLine);
                    continue;
                }

                // 找dbmodel
                ClassInfo dbmodel = null;
                foreach (var cls2 in assembly.ClassList)
                {
                    var attr2 = cls2.AttributeList.FirstOrDefault(f => f.TypeFullName == typeof(DbTable).FullName);
                    if (attr2 == null || attr.ArgumentList.Count == 0)
                    {
                        continue;
                    }
                    if (!cls2.Interfaces.Contains(typeof(IDbModel).FullName))
                    {
                        continue;
                    }
                    // 判断同一个表
                    if (attr2.ArgumentList[0] == attr.ArgumentList[0])
                    {
                        dbmodel = cls2;
                    }
                }
                if (dbmodel == null)
                {
                    fail++;
                    failStr += ("DbModel " + attr.ArgumentList[0] + " not fount " + Environment.NewLine);
                    continue;
                }
                foreach (var method in cls.MethodList)
                {
                    // intert
                    if (method.AttributeList.Exists(f => f.TypeFullName == typeof(DbInsert).FullName))
                    {
                        string tableName = attr.ArgumentList[0];
                        var columns = dbmodel.PropertyList.Where(f => f.AttributeList.Exists(g => g.TypeFullName == typeof(DbColumn).FullName)).ToList();

                        StringBuilder sql = new StringBuilder();
                        sql.Append("INSERT INTO ");
                        sql.Append(tableName);
                        sql.Append(" (");
                        sql.Append(string.Join(",", columns.Select(f => f.Name)));
                        sql.Append(" ) values (");
                        sql.Append(string.Join(",", columns.Select(f => "@" + f.Name)));
                        sql.Append(")");

                        // 写文件
                        var oldFile = File.ReadAllLines(cls.FilePath[0]);
                        var newFile = new List<string>();
                        // 补全上方无关内容
                        for (int i = 0; i < method.MinLine - 1; i++)
                        {
                            newFile.Add(oldFile[i]);
                        }
                        // {所在行处理
                        var beginIdx = oldFile[method.MinLine - 1].IndexOf("{");
                        if (beginIdx < 0)
                        {
                            fail++;
                            failStr += "方法" + cls.Name + "." + method.Name + "格式解析错误";
                            continue;
                        }
                        newFile.Add(oldFile[method.MinLine - 1].Substring(0, beginIdx+1));
                        // 写入db access内容
                        newFile.Add("            /// 该方法的代码由插件自动生成，请勿修改。");
                        newFile.Add("            string sql=@\""+sql.ToString()+"\";");
                        newFile.Add("            return helper.ExecuteNonQuery(sql);");
                        // }所在行处理
                        var endIdx = oldFile[method.MaxLine - 1].IndexOf("}");
                        if (endIdx < 0)
                        {
                            fail++;
                            failStr += "方法" + cls.Name + "." + method.Name + "格式解析错误";
                            continue;
                        }
                        newFile.Add("        " + oldFile[method.MaxLine - 1].Substring(endIdx));
                        for (int i = (int)method.MaxLine; i < oldFile.Length; i++)
                        {
                            newFile.Add(oldFile[i]);
                        }
                        File.WriteAllLines(cls.FilePath[0], newFile.ToArray());
                    }
                }
            }
        }

        // 生成dbModel代码
        private static void GenDbModel(AssemblyInfo assembly, List<string> csFiles, OracleConnection oracleConnection, ref int count, ref int fail, ref string failStr)
        {
            foreach (var cls in assembly.ClassList)
            {
                // 处理DbTable特性
                var attr = cls.AttributeList.FirstOrDefault(f => f.TypeFullName == typeof(DbTable).FullName);
                if (attr == null || attr.ArgumentList.Count == 0)
                {
                    continue;
                }
                if (!cls.Interfaces.Contains(typeof(IDbModel).FullName))
                {
                    continue;
                }
                count++;
                string tbName = attr.ArgumentList[0];
                if (string.IsNullOrWhiteSpace(tbName))
                {
                    fail++;
                    failStr += ("Table name is empty in class " + cls.Name + Environment.NewLine);
                    continue;
                }

                // 查找文件
                if (cls.FilePath.Count > 1)
                {
                    fail++;
                    failStr += ("partial class " + cls.Name + " is not support" + Environment.NewLine);
                    continue;
                }
                // pdb中不记录空类信息
                if (cls.FilePath.Count == 0)
                {
                    foreach (var csfile in csFiles)
                    {
                        var txt = File.ReadAllText(csfile);
                        var mth = Regex.Match(txt, @"class[\s\n]*?" + cls.Name);
                        if (mth.Success)
                        {
                            cls.FilePath.Add(csfile);
                            break;
                        }
                    }
                }
                if (cls.FilePath.Count == 0)
                {
                    fail++;
                    failStr += ("File of class " + cls.Name + " not found" + Environment.NewLine);
                    continue;
                }

                StringBuilder newFile = new StringBuilder();
                string oldFile = File.ReadAllText(cls.FilePath[0]);
                var className = Regex.Match(oldFile, @"class[\s\n]*?" + cls.Name + @"[\S\s\n]*?\{");
                var tt = Regex.Match(oldFile, @"class[\s\n]*?" + cls.Name);
                if (!className.Success)
                {
                    fail++;
                    failStr += ("class " + cls.Name + " not found" + Environment.NewLine);
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
                        signLevel = 1;
                        // 重写class
                        newFile.AppendLine();
                        newFile.AppendLine("        /// 该类型的代码由插件自动生成，请勿修改。");
                        newFile.AppendLine();
                        // 填入字段
                        string sql = "select t.table_name,t.column_name,t.data_type,t.data_length,t.nullable,c.comments " +
                            "from user_tab_columns t,user_col_comments c " +
                            "where t.column_name = c.column_name " +
                            "and t.table_name = c.table_name " +
                            "and t.Table_Name ='" + tbName + "' " +
                            " order by t.Table_Name,t.column_id";

                        DataSet dSet = new DataSet();
                        OracleDataAdapter oda = new OracleDataAdapter(sql, oracleConnection);
                        oda.Fill(dSet);
                        DataTable dTable = dSet.Tables[0];
                        foreach (DataRow row in dTable.Rows)
                        {
                            if (!string.IsNullOrWhiteSpace(row["COMMENTS"].ToString()))
                            {
                                newFile.AppendLine("        /// <summary>");
                                newFile.AppendLine("        /// " + row["COMMENTS"].ToString());
                                newFile.AppendLine("        /// </summary>");
                            }
                            newFile.AppendLine("        [DbColumn(DataType." + row["DATA_TYPE"].ToString().Replace("(", "_").Replace(")", "") + ")]");
                            if (row["DATA_TYPE"].ToString() == "NUMBER")
                            {
                                newFile.AppendLine("        public int " + row["COLUMN_NAME"] + " { get; set; }");
                            }
                            else
                            {
                                newFile.AppendLine("        public string " + row["COLUMN_NAME"] + " { get; set; }");
                            }
                            newFile.AppendLine("");
                        }
                        newFile.Append("    }");
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
                                continue;
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
    }
}
