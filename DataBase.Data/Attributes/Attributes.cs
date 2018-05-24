using System;

namespace DataBase.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string tableName)
        {
            TableName = tableName;
        }

        public TableAttribute(string dbName, string tableName)
        {
            DbName = dbName;
            TableName = tableName;
        }

        public string TableName { get; set; }

        public string DbName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
        /// <summary>
        /// 是否是自增长
        /// </summary>
        public bool IsIdentity { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isIdentity"></param>
        public KeyAttribute(bool isIdentity = true)
        {
            IsIdentity = isIdentity;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IgnoreAttribute : Attribute
    {
    }

    
}