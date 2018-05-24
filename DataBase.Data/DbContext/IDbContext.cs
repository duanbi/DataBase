using System;
using System.Data;

namespace DataBase.Data
{
    public interface IDbContext : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction DbTransaction { get; }

        IDbTransaction BeginTran(IsolationLevel isolation = IsolationLevel.ReadCommitted);

        void InitConnection();

        void Commit();

        void Rollback();
    }
}