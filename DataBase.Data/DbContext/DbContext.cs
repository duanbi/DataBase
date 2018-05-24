using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using DataBase.Data;

namespace DataBase.Data
{
    public class DbContext : IDbContext
    {
        //private readonly ConnectionStringSettings _connectionSeting;
        private string _connectionSeting;

        //private string _dbName;

        public IDbConnection Connection { private set; get; }

        public IDbTransaction DbTransaction { private set; get; }

        private ITransaction transaction;

        public ITransaction Transaction
        {
            get
            {
                return this.transaction;
            }
        }

        public DataBaseTypeEnum DatabaseType { get; set; }

        public DbContext(string connectionSeting)
        {
            _connectionSeting = connectionSeting;
            Connection = new SqlConnection(_connectionSeting);

            if (Connection == null)
            {
                throw new Exception("Connection为null");
            }

            DatabaseType = GetDataBaseType.GetDbType("System.Data.SqlClient");

            InitConnection();
        }


        public DbContext(string connectionSeting, ITransaction tran, bool isWrite = true)
        {
            _connectionSeting = connectionSeting;
            Connection = new SqlConnection(_connectionSeting.ToString());
            
            if (Connection == null)
            {
                throw new Exception("Connection为null");
            }

            DatabaseType = GetDataBaseType.GetDbType("System.Data.SqlClient"); 

            transaction = tran;
            InitConnection();
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        public void InitConnection()
        {
            if (Connection == null)
            {
                DbProviderFactory dbfactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                if (transaction == null)
                {
                    Connection = dbfactory.CreateConnection();
                    if (Connection != null)
                    {
                        Connection.ConnectionString = _connectionSeting;
                        Connection.Open();
                    }
                }
                else
                {
                    if (transaction.DbConnection == null)
                    {
                        Connection = dbfactory.CreateConnection();
                        if (Connection != null)
                        {
                            Connection.ConnectionString = _connectionSeting;
                            Connection.Open();
                            transaction.DbConnection = Connection;
                            transaction.DbTransaction = Connection.BeginTransaction();
                            DbTransaction = transaction.DbTransaction;
                        }
                    }
                    else
                    {
                        if (transaction.DbConnection != null)
                        {
                            Connection = transaction.DbConnection;
                            DbTransaction = transaction.DbTransaction;
                        }
                    }
                }
            }


       

            if (transaction == null)
            {
                Connection = new SqlConnection(_connectionSeting.ToString());
                Connection.Open();
            }
            else if (transaction != null && transaction.DbConnection == null)
            {
                Connection = new SqlConnection(_connectionSeting.ToString());
                Connection.Open();
                transaction.DbConnection = Connection;
                transaction.DbTransaction = Connection.BeginTransaction();
                DbTransaction = transaction.DbTransaction;
            }
            else if (transaction != null && transaction.DbConnection != null)
            {
                Connection = transaction.DbConnection;
                DbTransaction = transaction.DbTransaction;
            }
        }

        public IDbTransaction BeginTran(IsolationLevel isolation = IsolationLevel.ReadCommitted)
        {
            if (this.Connection == null)
            {
                throw new Exception("Connection为null");
            }
            if (this.Connection.State != ConnectionState.Open)
            {
                this.Connection.Open();
            }
            DbTransaction = this.Connection.BeginTransaction();
            return DbTransaction;
        }

        /// <summary>
        /// 事务提交
        /// </summary>
        public void Commit()
        {
            if (DbTransaction == null)
            {
                throw new Exception("未开启事务");
            }
            DbTransaction.Commit();
            DbTransaction.Dispose();
            DbTransaction = null;
        }

        /// <summary>
        /// 事务回滚
        /// </summary>
        public void Rollback()
        {
            if (DbTransaction == null)
            {
                throw new Exception("未开启事务");
            }
            DbTransaction.Rollback();
            DbTransaction.Dispose();
            DbTransaction = null;
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (transaction != null)
            {
                return;
            }
            if (Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
                Connection = null;
            }
            if (DbTransaction != null)
            {
                DbTransaction = null;
            }
        }
    }
}