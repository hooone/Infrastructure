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
            string projPath = args[0];
            //string projPath = @"C:\Users\techsun\Desktop\Infrastructure\Demo\Demo.csproj";

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

            // 生成dbmodel代码
            int count = 0;
            int fail = 0;
            {
                foreach (var cls in assembly.ClassList)
                {
                    // 处理DbTable特性
                    var attr = cls.AttributeList.FirstOrDefault(f => f.TypeFullName == typeof(DbTable).FullName);
                    if (attr == null || attr.ArgumentList.Count == 0)
                    {
                        continue;
                    }
                    count++;
                    string tbName = attr.ArgumentList[0];
                    if (string.IsNullOrWhiteSpace(tbName))
                    {
                        fail++;
                        continue;
                    }

                    // 查找文件
                    if (cls.FilePath.Count > 1)
                    {
                        fail++;
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
                            newFile.AppendLine("        /// 该类型代码由插件自动生成，请勿修改。");
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
            Console.WriteLine("共找到" + count.ToString() + "个文件" + (fail == 0 ? "" : (",失败" + fail + "个文件")) + "!");
            return;
        }
    }
}
