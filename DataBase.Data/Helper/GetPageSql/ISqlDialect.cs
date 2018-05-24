using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public interface ISqlDialect
    {
        string GetPagingSql(string sql, int page, int perPage, string orderby);
    }
}
