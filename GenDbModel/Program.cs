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
            string strConn = "";
            {
                foreach (var cls in assembly.ClassList)
                {
                    if (cls.Interfaces.Contains(typeof(IConnectionString).FullName))
                    {
                        var assm = AppDomain.CurrentDomain.GetAssemblies();
                        var select = assm.FirstOrDefault(f => f.Location.ToUpper() == assembly.AssemblyPath.ToUpper());
                        if (select == null)
                        {
                            Console.WriteLine("未找到程序集");
                            return;
                        }

                        dynamic conn = select.CreateInstance(cls.FullName);
                        strConn = conn.ConnectionString;
                        break;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(strConn))
            {
                Console.WriteLine("未找到数据库连接方式");
                return;
            }

            // 连接数据库
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


                        // 写入db access内容
                        List<string> newFile = new List<string>();
                        newFile.Add("        public int " + method.Name + "(" + dbmodel.Name + " obj)");
                        newFile.Add("        {");
                        newFile.Add("            /// 该方法的代码由插件自动生成，请勿修改。");
                        newFile.Add("            string sql = @\"" + sql.ToString() + "\";");
                        newFile.Add("            return Helper.ExecuteNonQuery(sql, " + string.Join(", ", columns.Select(f => "obj." + f.Name)) + ");");
                        WriteMethod(cls.FilePath[0], cls.MethodList, method.MinLine, method.MaxLine, newFile.ToArray());
                    }
                    // update
                    if (method.AttributeList.Exists(f => f.TypeFullName == typeof(DbUpdate).FullName))
                    {
                        string tableName = attr.ArgumentList[0];
                        List<PropertyInfo> keys = new List<PropertyInfo>();
                        List<PropertyInfo> values = new List<PropertyInfo>();
                        foreach (var item in method.AttributeList)
                        {
                            if (item.TypeFullName == typeof(SqlKey).FullName)
                            {
                                var t = dbmodel.PropertyList.FirstOrDefault(f => f.Name == item.ArgumentList[0]);
                                if (t != null)
                                {
                                    keys.Add(t);
                                }
                            }
                            if (item.TypeFullName == typeof(SqlValue).FullName)
                            {
                                var t = dbmodel.PropertyList.FirstOrDefault(f => f.Name == item.ArgumentList[0]);
                                if (t != null)
                                {
                                    values.Add(t);
                                }
                            }
                        }

                        StringBuilder sql = new StringBuilder();
                        sql.Append("UPDATE ");
                        sql.Append(tableName);
                        sql.Append(" SET ");
                        sql.Append(string.Join(",", values.Select(f => f.Name + "=@" + f.Name)));
                        if (keys.Count > 0)
                        {
                            sql.Append(" WHERE ");
                            sql.Append(string.Join(" and ", keys.Select(f => f.Name + "=@" + f.Name)));
                        }


                        // 写入db access内容
                        List<string> newFile = new List<string>();
                        newFile.Add("        public int " + method.Name + "(" + dbmodel.Name + " obj)");
                        newFile.Add("        {");
                        newFile.Add("            /// 该方法的代码由插件自动生成，请勿修改。");
                        newFile.Add("            string sql = @\"" + sql.ToString() + "\";");
                        newFile.Add("            return Helper.ExecuteNonQuery(sql, " + string.Join(", ", values.Select(f => "obj." + f.Name)) + (keys.Count > 0 ? ", " : "") + string.Join(", ", keys.Select(f => "obj." + f.Name)) + ");");
                        WriteMethod(cls.FilePath[0], cls.MethodList, method.MinLine, method.MaxLine, newFile.ToArray());
                    }
                    // delete
                    if (method.AttributeList.Exists(f => f.TypeFullName == typeof(DbDelete).FullName))
                    {
                        string tableName = attr.ArgumentList[0];
                        List<PropertyInfo> keys = new List<PropertyInfo>();
                        foreach (var item in method.AttributeList)
                        {
                            if (item.TypeFullName == typeof(SqlKey).FullName)
                            {
                                var t = dbmodel.PropertyList.FirstOrDefault(f => f.Name == item.ArgumentList[0]);
                                if (t != null)
                                {
                                    keys.Add(t);
                                }
                            }
                        }

                        StringBuilder sql = new StringBuilder();
                        sql.Append("DELETE ");
                        sql.Append(tableName);
                        if (keys.Count > 0)
                        {
                            sql.Append(" WHERE ");
                            sql.Append(string.Join(" and ", keys.Select(f => f.Name + "=@" + f.Name)));
                        }


                        // 写入db access内容
                        List<string> newFile = new List<string>();
                        newFile.Add("        public int " + method.Name + "(" + dbmodel.Name + " obj)");
                        newFile.Add("        {");
                        newFile.Add("            /// 该方法的代码由插件自动生成，请勿修改。");
                        newFile.Add("            string sql = @\"" + sql.ToString() + "\";");
                        newFile.Add("            return Helper.ExecuteNonQuery(sql" + (keys.Count > 0 ? ", " : "") + string.Join(", ", keys.Select(f => "obj." + f.Name)) + ");");
                        WriteMethod(cls.FilePath[0], cls.MethodList, method.MinLine, method.MaxLine, newFile.ToArray());
                    }
                    // read
                    if (method.AttributeList.Exists(f => f.TypeFullName == typeof(DbRead).FullName))
                    {
                        string tableName = attr.ArgumentList[0];
                        List<PropertyInfo> keys = new List<PropertyInfo>();
                        foreach (var item in method.AttributeList)
                        {
                            if (item.TypeFullName == typeof(SqlKey).FullName)
                            {
                                var t = dbmodel.PropertyList.FirstOrDefault(f => f.Name == item.ArgumentList[0]);
                                if (t != null)
                                {
                                    keys.Add(t);
                                }
                            }
                        }

                        StringBuilder sql = new StringBuilder();
                        sql.Append("SELECT * FROM ");
                        sql.Append(tableName);
                        if (keys.Count > 0)
                        {
                            sql.Append(" WHERE ");
                            sql.Append(string.Join(" and ", keys.Select(f => f.Name + "=@" + f.Name)));
                        }


                        // 写入db access内容
                        List<string> newFile = new List<string>();
                        newFile.Add("        public List<" + dbmodel.Name + "> " + method.Name + "()");
                        newFile.Add("        {");
                        newFile.Add("            /// 该方法的代码由插件自动生成，请勿修改。");
                        newFile.Add("            string sql = @\"" + sql.ToString() + "\";");
                        newFile.Add("            DataTable dt = Helper.Query(sql" + (keys.Count > 0 ? ", " : "") + string.Join(", ", keys.Select(f => "obj." + f.Name)) + ");");
                        newFile.Add("            List<" + dbmodel.Name + "> rst = new List<" + dbmodel.Name + ">();");
                        newFile.Add("            foreach (DataRow row in dt.Rows)");
                        newFile.Add("            {");
                        newFile.Add("                " + dbmodel.Name + " t = new " + dbmodel.Name + "();");
                        foreach (var col in dbmodel.PropertyList.Where(f => f.AttributeList.Exists(g => g.TypeFullName == typeof(DbColumn).FullName)))
                        {
                            var colAttr = col.AttributeList.FirstOrDefault(t => t.TypeFullName == typeof(DbColumn).FullName);
                            DataType type = (DataType)int.Parse(colAttr.ArgumentList[0]);
                            switch (type)
                            {
                                case (DataType.VARCHAR):
                                case (DataType.VARCHAR2):
                                    newFile.Add("                t." + col.Name + " = row[nameof(" + dbmodel.Name + "." + col.Name + ")].TryToString();");
                                    break;
                                case (DataType.NUMBER):
                                    newFile.Add("                t." + col.Name + " = row[nameof(" + dbmodel.Name + "." + col.Name + ")].TryToInt();");
                                    break;
                                case (DataType.FLOAT):
                                    newFile.Add("                t." + col.Name + " = row[nameof(" + dbmodel.Name + "." + col.Name + ")].TryToFload();");
                                    break;
                                case (DataType.DATE):
                                case (DataType.TIMESTAMP_6):
                                    newFile.Add("                t." + col.Name + " = row[nameof(" + dbmodel.Name + "." + col.Name + ")].TryToDateTime();");
                                    break;
                                default:
                                    newFile.Add("                t." + col.Name + " = row[nameof(" + dbmodel.Name + "." + col.Name + ")].TryToString();");
                                    break;
                            }

                        }
                        newFile.Add("                rst.Add(t);");
                        newFile.Add("            }");
                        newFile.Add("            return rst;");
                        WriteMethod(cls.FilePath[0], cls.MethodList, method.MinLine, method.MaxLine, newFile.ToArray());
                    }
                }
            }
        }

        private static void WriteMethod(string path, List<MethodInfo> methodList, int minLine, int maxLine, string[] methodStr)
        {
            // 写文件
            var oldFile = File.ReadAllLines(path);
            var newFile = new List<string>();
            // 根据']'找到函数开始行
            for (int i = minLine - 1; i > 0; i--)
            {
                var idx = oldFile[i].IndexOf(']');
                if (idx >= 0)
                {
                    // 补全上方无关内容
                    for (int j = 0; j < i; j++)
                    {
                        newFile.Add(oldFile[j]);
                    }
                    newFile.Add(oldFile[i].Substring(0, idx + 1));
                    break;
                }
            }
            // 函数体。 不包含'}'
            newFile.AddRange(methodStr);
            // }所在行处理
            var endIdx = oldFile[maxLine - 1].IndexOf("}");
            if (endIdx >= 0)
            {
                newFile.Add("        " + oldFile[maxLine - 1].Substring(endIdx));
            }
            var delta = newFile.Count - maxLine;
            // 补回剩余行
            for (int i = maxLine; i < oldFile.Length; i++)
            {
                newFile.Add(oldFile[i]);
            }
            File.WriteAllLines(path, newFile.ToArray());

            // 行号更新
            foreach (var item in methodList)
            {
                if (item.MinLine > maxLine)
                {
                    item.MinLine += delta;
                }
                if (item.MaxLine > maxLine)
                {
                    item.MaxLine += delta;
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
                        var mth = Regex.Match(txt, @"class[\s\n]*?" + cls.Name + @"[\s\S]*?IDbModel");
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
                            else if (row["DATA_TYPE"].ToString().Contains("TIMESTAMP"))
                            {
                                newFile.AppendLine("        public DateTime " + row["COLUMN_NAME"] + " { get; set; }");
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
