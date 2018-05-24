using System;
using System.Linq;
using System.Text;

namespace DataBase.Data
{
    public class SqlServerDialect : ISqlDialect
    {
        public string GetPagingSql(string sql, int page, int resultsPerPage, string orderby)
        {
            if (page <= 0)
            {
                page = 1;
            }
            int startValue = ((page - 1) * resultsPerPage) + 1;
            return GetSetSql(sql, startValue, resultsPerPage, orderby);
        }

        private string GetSetSql(string sql, int firstResult, int maxResults, string orderby)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentNullException("sql");
            }
            sql = sql.TrimStart();
            int selectIndex = SqlDialectBase.GetSelectEnd(sql) + 1;
            string orderByClause = string.IsNullOrEmpty(orderby) ? "CURRENT_TIMESTAMP" : orderby;

            string projectedColumns = SqlDialectBase.GetColumnNames(sql).Aggregate(new StringBuilder(), (sb, s) => (sb.Length == 0 ? sb : sb.Append(", ")).Append(GetColumnName("_proj", s, null)), sb => sb.ToString());
            string newSql = sql.Insert(selectIndex, string.Format("ROW_NUMBER() OVER({0}) AS {1}, ", orderByClause, "_row_number"));

            string result = string.Format("SELECT TOP({0}) {1} FROM ({2}) [_proj] WHERE {3} >= {4}",
                maxResults, projectedColumns.Trim(), newSql, "_proj._row_number", firstResult);

            return result;
        }

        public virtual string GetColumnName(string prefix, string columnName, string alias)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentNullException("ColumnName", "columnName cannot be null or empty.");
            }

            StringBuilder result = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                result.AppendFormat(prefix + ".");
            }

            if (columnName != "*")
            {
                result.AppendFormat(columnName);
            }
            else
            {
                result.AppendFormat(columnName);
            }

            if (!string.IsNullOrWhiteSpace(alias))
            {
                result.AppendFormat(" AS {0}", alias);
            }

            return result.ToString();
        }
    }
}