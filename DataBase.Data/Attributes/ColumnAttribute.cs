using System;

namespace DataBase.Data
{
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// 数据字段名称
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 字段长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        public ColumnAttribute(string columnName,int length=0,string description="")
        {
            this.ColumnName = columnName;
            this.Length = length;
            this.Description = description;
        }

        public ColumnAttribute(string columnName, int length = 0)
        {
            this.ColumnName = columnName;
            this.Length = length;
        }
    }
}
