using DataBase.Data;

namespace DataBase.Data
{
    public class DataBaseFactory
    {
        private DataBaseTypeEnum dbType;

        public DataBaseFactory(DataBaseTypeEnum _dbType)
        {
            dbType = _dbType;
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="page"></param>
        /// <param name="perPage"></param>
        /// <param name="orderby"></param>
        /// <returns></returns>
        public string GetSqlPages(string sql, int page, int perPage, string orderby)
        {
            ISqlDialect sqlDialect;

            if (dbType == DataBaseTypeEnum.MySql)
            {
                sqlDialect = new MySqlDialect();
            }
            else if (dbType == DataBaseTypeEnum.SqlServer)
            {
                sqlDialect = new SqlServerDialect();
            }
            else if (dbType == DataBaseTypeEnum.Oracle)
            {
                sqlDialect = null;
            }
            else if (dbType == DataBaseTypeEnum.Aceess)
            {
                sqlDialect = null;
            }
            else
            {
                sqlDialect = null;
            }
            if (sqlDialect == null)
            {
                return string.Empty;
            }
            return sqlDialect.GetPagingSql(sql, page, perPage, orderby);
        }
    }
}