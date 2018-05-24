using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBase.Data;
namespace DataBase.Data
{
    public static class GetDataBaseType
    {
        public static DataBaseTypeEnum GetDbType(string providerName)
        {
            DataBaseTypeEnum dbType = DataBaseTypeEnum.SqlServer;
            if (string.IsNullOrEmpty(providerName))
            {
                throw new Exception("连接字符串未定义 ProviderName");
            }
            else if (providerName == "System.Data.SqlClient")
            {
                dbType = DataBaseTypeEnum.SqlServer;
            }
            else if (providerName == "Oracle.DataAccess.Client")
            {
                dbType = DataBaseTypeEnum.Oracle;
            }
            else if (providerName == "System.Data.OracleClient")
            {
                dbType = DataBaseTypeEnum.Oracle;
            }
            else if (providerName == "MySql.Data.MySqlClient")
            {
                dbType = DataBaseTypeEnum.MySql;
            }
            else if (providerName == "System.Data.OleDb")
            {
                dbType = DataBaseTypeEnum.Aceess;
            }
            else
            {
                throw new Exception("连接字符串未识别 ProviderName");
            }
            return dbType;
        }
    }
}
