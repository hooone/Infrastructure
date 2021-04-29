using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.DB
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbColumn : Attribute
    {
        public DbColumn(DataType type)
        {
            Type = type;
        }
        public DbColumn(DataType type, int length)
        {
            Type = type;
            Length = length;
        }
        public DataType Type { get; set; }
        public int Length { get; set; }
    }
    public enum DataType
    {
        /// <summary>
        /// 定长字符串
        /// </summary>
        VARCHAR,
        /// <summary>
        /// 可变长度字符串
        /// </summary>
        VARCHAR2,
        /// <summary>
        /// 整数，默认32位
        /// </summary>
        NUMBER,
        /// <summary>
        /// 浮点数，默认32位
        /// </summary>
        FLOAT,
        /// <summary>
        /// 日期时间
        /// </summary>
        DATE,
        /// <summary>
        /// 时间戳
        /// </summary>
        TIMESTAMP_6,
    }
}
