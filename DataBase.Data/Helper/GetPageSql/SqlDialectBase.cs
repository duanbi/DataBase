using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public static class SqlDialectBase
    {
        public static string GetSqlCount(string sql)
        {
            sql = sql.Trim();
            var sqlCount = new StringBuilder();
            sqlCount.Append(" SELECT COUNT(1) ");
            sqlCount.Append(sql.Substring(GetFromStart(sql)));

            return sqlCount.ToString();
        }

        public static int GetSelectEnd(string sql)
        {
            if (sql.StartsWith("SELECT DISTINCT", StringComparison.InvariantCultureIgnoreCase))
            {
                return 15;
            }

            if (sql.StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase))
            {
                return sql.IndexOf("SELECT", StringComparison.InvariantCultureIgnoreCase)+6;
            }

            throw new ArgumentException("SQL must be a SELECT statement.", "sql");
        }
        
        public static IList<string> GetColumnNames(string sql)
        {
            int start = GetSelectEnd(sql);
            int stop = GetFromStart(sql);
            string[] columnSql = sql.Substring(start, stop - start).Split(',');
            List<string> result = new List<string>();
            foreach (string c in columnSql)
            {
                var cl = c.Trim();
                string[] clArr = cl.Split(' ');
                if (clArr.Length > 1)
                {
                    result.Add(clArr[clArr.Length - 1].Trim());
                    continue;
                }
                string[] colParts = cl.Split('.');
                result.Add(colParts[colParts.Length - 1].Trim());
            }

            return result;
        }

        public static int GetFromStart(string sql)
        {
            int selectCount = 0;
            string[] words = sql.Split(' ');
            int fromIndex = 0;
            foreach (var word in words)
            {
                if (word.IndexOf("SELECT", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    selectCount++;
                }
                else if (word.IndexOf("FROM", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    selectCount--;
                    if (selectCount == 0)
                    {
                        break;
                    }
                }

                fromIndex += word.Length + 1;
            }

            return fromIndex;
        }
    }
}
