using DatabaseSchemaReader;
using DatabaseSchemaReader.DataSchema;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.T4Template
{
    public class T4Helper
    {
        private readonly Configuration _configuration;
        private readonly ConnectionStringSettings _connectionString;
        private string _indent = string.Empty;
        private readonly string _dbName = string.Empty;
        private DatabaseReader _databaseReader;
        private DatabaseTable _databaseTable;
        private string _providerName = "System.Data.SqlClient";// MySql.Data.MySqlClient
        /// <summary>
        ///     表名
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        ///     命名空间
        /// </summary>
        public string NamespaceStr { get; set; } = string.Empty;

        /// <summary>
        ///     字段前缀
        /// </summary>
        public string ColumnsPrefix { get; set; } = string.Empty;

        /// <summary>
        ///     表前缀
        /// </summary>
        public string TablePrefix { get; set; } = string.Empty;

        /// <summary>
        ///     实体后缀
        /// </summary>
        public string EntitySuffix { get; set; } = "Entity";

        /// <summary>
        /// T4Helper
        /// </summary>
        /// <param name="templateFilePath"></param>
        /// <param name="dbName"></param>
        public T4Helper(string templateFilePath, string dbName)
        {
            _dbName = dbName;
            var directoryName = Path.GetDirectoryName(templateFilePath);
            var exeConfigFilename = Directory.GetFiles(directoryName, "*.config").FirstOrDefault() ?? Directory.GetParent(directoryName).GetFiles("*.config").FirstOrDefault().FullName;
            _configuration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap
            {
                ExeConfigFilename = exeConfigFilename
            }, ConfigurationUserLevel.None);
            _connectionString = _configuration.ConnectionStrings.ConnectionStrings[dbName];
        }

        /// <summary>
        /// 生成代码
        /// </summary>
        /// <returns></returns>
        public string Render()
        {
             _databaseReader = new DatabaseReader(_connectionString.ConnectionString, !string.IsNullOrEmpty(_connectionString.ProviderName) ? _connectionString.ProviderName : _providerName);
            _databaseTable = _databaseReader.Table(TableName);
            _databaseReader.DataTypes();
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(_indent + "using System;");
            stringBuilder.AppendLine(_indent + "using DataBase.Data;");
            stringBuilder.AppendLine(_indent + "namespace " + NamespaceStr);
            stringBuilder.AppendLine(_indent + "{");
            if (_databaseTable != null)
            {
                PushIndent("    ");
                stringBuilder.AppendLine(string.Concat(new object[] { _indent, "///<summary>" }));
                stringBuilder.AppendLine(string.Concat(_indent, "///", !string.IsNullOrEmpty(_databaseTable.Description) ? _databaseTable.Description : _databaseTable.Name));
                stringBuilder.AppendLine(string.Concat(new object[] { _indent, "///</summary>" }));
                stringBuilder.AppendLine(string.Format("{0}[Table(\"{1}\")]", _indent, _databaseTable.Name));
                stringBuilder.AppendLine(string.Concat(new[] { _indent, "public class ", string.Format("{0}:{1}", GetTableName(_databaseTable.Name) + EntitySuffix, GetBaseEntity(_databaseTable)) }));
                stringBuilder.AppendLine(_indent + "{");
                PushIndent("    ");
                BuildDefaultValue(stringBuilder, _databaseTable);
                stringBuilder.Append("\n");
                BuildProperty(stringBuilder, _databaseTable);
                PopIndent();
                stringBuilder.AppendLine(_indent + "}");
                PopIndent();
            }
            stringBuilder.AppendLine(_indent + "}");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 构建类名
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public string GetTableName(string tableName)
        {
            if (!string.IsNullOrEmpty(TablePrefix))
            {
                var pre = tableName.Substring(0, TablePrefix.Length);
                if (pre == TablePrefix)
                {
                    tableName = tableName.Remove(0, TablePrefix.Length);
                }
            }
            if (!string.IsNullOrEmpty(tableName))
            {
                tableName = GetNewName(tableName);
            }
            return tableName;
        }

        /// <summary>
        /// 构建属性
        /// </summary>
        /// <param name="outStr"></param>
        /// <param name="databaseTable"></param>
        private void BuildProperty(StringBuilder outStr, DatabaseTable databaseTable)
        {
            foreach (var column in databaseTable.Columns)
            {
                outStr.AppendLine(string.Concat(new object[] { _indent, "///<summary>" }));
                outStr.AppendLine(string.Concat(_indent, "///", !string.IsNullOrEmpty(column.Description) ? column.Description : column.Name));
                outStr.AppendLine(string.Concat(new object[] { _indent, "///</summary>" }));
                if (column.IsPrimaryKey)
                {
                    if (column.IsAutoNumber)
                    {
                        outStr.AppendLine(string.Concat(new object[] { _indent, "[Key]" }));
                        outStr.AppendLine(string.Concat(_indent, "[Column(\"", column.Name, "\",", column.Length ?? 0, ")]"));
                        outStr.AppendLine(string.Concat(_indent, "public ", column.DataType.NetDataTypeCSharpName, " Id { get; set; }"));
                    }
                    else
                    {
                        outStr.AppendLine(string.Concat(new object[] { _indent, "[Key(false)]" }));
                        outStr.AppendLine(string.Concat(_indent, "[Column(\"", column.Name, "\",", column.Length ?? 0,  ")]"));
                        outStr.AppendLine(string.Concat(_indent, "public ", column.DataType.NetDataTypeCSharpName, " Id { get; set; }"));
                    }
                    continue;
                }
                outStr.AppendLine(string.Concat(_indent, "[Column(\"", column.Name, "\",", column.Length ?? 0,  ")]"));
                outStr.AppendLine(string.Concat(_indent, "public virtual ", column.DataType.NetDataTypeCSharpName, " ", GetPropertyName(column), "{ get; set; }"));
            }
        }
        /// <summary>
        /// 构建默认值
        /// </summary>
        /// <param name="outStr"></param>
        /// <param name="databaseTable"></param>
        private void BuildDefaultValue(StringBuilder outStr, DatabaseTable databaseTable)
        {
            outStr.AppendLine(_indent + "#region 默认值");
            outStr.AppendLine(_indent + "public " + GetTableName(databaseTable.Name) + EntitySuffix + "()");
            outStr.AppendLine(_indent + "{");
            foreach (var column in databaseTable.Columns)
            {
                if (column.IsPrimaryKey || column.DbDataType == "uniqueidentifier")
                {
                    continue;
                }
                outStr.AppendLine(string.Concat(_indent, "    ", GetPropertyName(column), " = ", GetDefaultValue(column), ";"));
            }
            outStr.AppendLine(_indent + "}");
            outStr.AppendLine(_indent + "#endregion");
        }

        private void PushIndent(string str)
        {
            _indent += str;
        }

        private void PopIndent()
        {
            _indent = _indent.Remove(_indent.Length - 4);
        }
        /// <summary>
        /// 默认值处理
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private string GetDefaultValue(DatabaseColumn column)
        {
            var value = string.Empty;
            switch (column.DbDataType)
            {
                case "nvarchar":
                    value = "string.Empty";
                    break;
                case "text":
                    value = "string.Empty";
                    break;
                case "datetime":
                    if (column.DefaultValue != null)
                    {
                        if (column.DefaultValue == "(getdate())" || column.DefaultValue == "CURRENT_TIMESTAMP")
                        {
                            value = "DateTime.Now";
                        }
                        else
                        {
                            value = $"DateTime.Parse(\"{column.DefaultValue.Replace("('", "").Replace("')", "")}\")";
                        }

                    }
                    else
                    {
                        value = "new DateTime(1900,01,01)";
                    }
                    break;
                case "bit":
                    if (column.DefaultValue != null)
                    {
                        var defaultValue = column.DefaultValue.Replace("((", "").Replace("))", "");
                        var tempValue = defaultValue.IndexOf('.') > 0 ? defaultValue.Substring(0, defaultValue.IndexOf('.')) : defaultValue;

                        try
                        {
                            if ("1".Equals(tempValue))
                            {
                                value = "true";
                            }
                        }
                        catch (Exception ex)
                        {
                            value = "false";
                        }
                    }
                    else
                    {
                        value = "false";
                    }
                    break;
                default:
                    if (column.DataType.IsNumeric)
                    {
                        if (column.DefaultValue != null)
                        {
                            var defaultValue = column.DefaultValue.Replace("((", "").Replace("))", "");
                            value = defaultValue.IndexOf('.') > 0 ? defaultValue.Substring(0, defaultValue.IndexOf('.')) : defaultValue;
                        }
                        else
                        {
                            value = "0";
                        }
                    }
                    break;
            }
            if (column.DbDataType.Contains("varchar"))
            {
                value = "string.Empty";
            }
            return value;
        }
        /// <summary>
        /// 属性名处理
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private string GetPropertyName(DatabaseColumn column)
        {
            var columnName = column.Name;
            if (!string.IsNullOrEmpty(ColumnsPrefix))
            {
                var pre = columnName.Substring(0, ColumnsPrefix.Length);
                if (pre == ColumnsPrefix)
                {
                    columnName = columnName.Remove(0, ColumnsPrefix.Length);
                }
            }
            return GetNewName(columnName);
        }

        /// <summary>
        /// 根据原来名称转换新名称（去掉“_”，首字母大写）
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetNewName(string name)
        {
            var newName = string.Empty;
            var str = name.Split('_');
            if (str.Any())
            {
                foreach (var item in str)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        newName += item.Substring(0, 1).ToUpper() + item.Substring(1);
                    }
                }
            }
            return newName;
        }
        /// <summary>
        /// 获取继承实体类型
        /// </summary>
        /// <param name="databaseTable"></param>
        /// <returns></returns>
        private string GetBaseEntity(DatabaseTable databaseTable)
        {
            if (databaseTable.PrimaryKeyColumn != null)
            {
                if (databaseTable.PrimaryKeyColumn.DbDataType == "uniqueidentifier")
                {
                    return "Entity<Guid>";
                }
                else
                {
                    return $"Entity<{databaseTable.PrimaryKeyColumn.DataType.NetDataTypeCSharpName}>";
                }
            }
            return "Entity";
        }
    }
}
