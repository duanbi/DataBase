using System.Data;
using System.Data.SqlClient;
using DataBase.Data;

namespace DataBase.Data
{
    public class DbManagerFactory
    {
        public DbManagerFactory()
        {
        }
        // todo
        public static IDbConnection GetConnection(DataBaseTypeEnum providerType)
        {
            IDbConnection iDbConnection;
            switch (providerType)
            {
                case DataBaseTypeEnum.SqlServer:
                    iDbConnection = new SqlConnection();
                    break;

                default:
                    return null;
            }

            return iDbConnection;
        }
    }
}