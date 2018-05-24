using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public class MySqlDialect : ISqlDialect
    {
        public string GetPagingSql(string sql, int page, int perPage, string orderby)
        {
            if (page <= 0)
            {
                page = 1;
            }
            int startValue = (page - 1) * perPage;
            return GetSetSql(sql, startValue, perPage, orderby);
        }

        public string GetSetSql(string sql, int firstResult, int maxResults, string orderby)
        {
            string result = string.Format("{0} {1} LIMIT {2}, {3}", sql, string.IsNullOrEmpty(orderby) ? string.Empty : orderby, firstResult, maxResults);

            return result;
        }
    }
}