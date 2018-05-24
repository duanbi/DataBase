using System.Data;

namespace DataBase.Data
{
    public interface ITransaction
    {
        IDbConnection DbConnection
        {
            get;
            set;
        }

        IDbTransaction DbTransaction
        {
            get;
            set;
        }

        void Commit();
    }
}