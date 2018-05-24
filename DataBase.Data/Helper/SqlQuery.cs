namespace DataBase.Data
{
    public class SqlQuery
    {
        /// <summary>
        /// sql字符串
        /// </summary>
        public string Sql
        {
            get;
            private set;
        }

        /// <summary>
        /// 参数
        /// </summary>
        public object Param
        {
            get;
            private set;
        }

        /// <summary>
        /// 构造函数（查询sql语句以及参数化值）
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        public SqlQuery(string sql, dynamic param)
        {
            this.Param = param;
            this.Sql = sql;
        }

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="param"></param>
        public void SetParam(dynamic param)
        {
            this.Param = param;
        }
    }
}