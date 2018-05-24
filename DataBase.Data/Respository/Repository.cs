using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataBase.Data;
using DataBase.Data.Helper;

namespace DataBase.Data
{
    public class Repository<TEntity, TPrimaryKey> : IRepository<TEntity, TPrimaryKey> where TEntity : class, IEntity<TPrimaryKey>
    {
        private ISqlGenerator<TEntity, TPrimaryKey> _sqlGenerator;
       
        private string _connectionString;

        private ConnectionStringSettings _connectionSeting;

        public Repository(string connectionString)
        {
            _connectionSeting = new ConnectionStringSettings()
            {
                ConnectionString = connectionString,
                ProviderName = "System.Data.SqlClient"
            };

            _sqlGenerator = new SqlGenerator<TEntity, TPrimaryKey>();
        }

        //public Repository(ConnectionStringSettings connectionSeting)
        //{
        //    _connectionSeting = connectionSeting;

        //    _sqlGenerator = new SqlGenerator<TEntity, TPrimaryKey>();
        //}

        

        public virtual TEntity GetById(TPrimaryKey id)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString))
            {
                return dbContext.Connection.Get<TEntity>(id);
            }
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString))
            {
                return dbContext.Connection.GetAll<TEntity>();
            }
        }

        public virtual long InsertIdentity(TEntity entity, ITransaction transaction = null)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString, transaction))
            {
                return dbContext.Connection.Insert(entity, dbContext.DbTransaction);
            }
        }

        public virtual bool Insert(TEntity entity, ITransaction transaction = null)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString, transaction))
            {
                return dbContext.Connection.Insert(entity, dbContext.DbTransaction) > 0;
            }
        }

        public virtual TEntity FindFirst(Expression<Func<TEntity, bool>> expression)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString))
            {
                SqlQuery selectFirst = _sqlGenerator.GetSelect(expression);
                return dbContext.Connection.QueryFirst<TEntity>(selectFirst.Sql, selectFirst.Param);
            }
        }

        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString))
            {
                SqlQuery select = _sqlGenerator.GetSelect(expression);
                return dbContext.Connection.Query<TEntity>(select.Sql, select.Param);
            }
        }

        public virtual int Count(Expression<Func<TEntity, bool>> expression)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString))
            {
                SqlQuery select = _sqlGenerator.GetCount(expression);
                return dbContext.Connection.ExecuteScalar<int>(select.Sql, select.Param);
            }
        }

        public virtual bool Delete(TEntity entity, ITransaction transaction = null)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString, transaction))
            {
                return dbContext.Connection.Delete(entity, dbContext.DbTransaction);
            }
        }

        public virtual bool Delete(Expression<Func<TEntity, bool>> expression, ITransaction transaction = null)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString, transaction))
            {
                SqlQuery select = _sqlGenerator.GetDelete(expression);
                return dbContext.Connection.Execute(select.Sql, select.Param, dbContext.DbTransaction) > 0;
            }
        }

        public virtual bool Update(TEntity entity, ITransaction transaction = null)
        {
            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString, transaction))
            {
                return dbContext.Connection.Update(entity, dbContext.DbTransaction);
            }
        }

        /// <summary>
        /// 根据主键生成代理类,代理类只监控调用了Set方法的属性,并保存属性名到列表中,最后通过GetModifiedProperties获取变更的属性名
        /// 可以根据修改的属性调用支持部分字段更新的ORM框架,或自动生成sql语句进行更新
        /// </summary>
        /// <returns></returns>
        public TEntity GenerateProxy()
        {
            return DynamicProxyGenerator.CreateDynamicProxy<TEntity>();
        }

        /// <summary>
        /// 执行修改的数据字段
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public bool UpdateModify(TEntity entity, ITransaction transaction = null)
        {
            HashSet<string> modifiedPropertyNames = DynamicProxyGenerator.GetModifiedProperties(entity);

            if (modifiedPropertyNames == null || !modifiedPropertyNames.Any())
            {
                return false;
            }

            SqlQuery select = _sqlGenerator.GetUpdate(modifiedPropertyNames, entity);

            using (DbContext dbContext = new DbContext(_connectionSeting.ConnectionString, transaction))
            {
                return dbContext.Connection.Execute(select.Sql,select.Param, dbContext.DbTransaction)>0;
            }
        }

        ///// <summary>
        ///// 分页查询
        ///// </summary>
        ///// <typeparam name="T">返回实体</typeparam>
        ///// <param name="sqlSearchInfo">sql语句</param>
        ///// <param name="sqlOrderby">排序字段（不需要order by）</param>
        ///// <param name="nCount">总个数</param>
        ///// <param name="pageIndex">第几页</param>
        ///// <param name="pageSize">每页的个数</param>
        ///// <param name="param">参数化</param>
        ///// <param name="isWrite">默认读库</param>
        ///// <returns></returns>
        //public virtual IEnumerable<T> QueryPager<T>(string sqlSearchInfo, string sqlOrderby, ref int nCount, int pageIndex = 1, int pageSize = 10, dynamic param = null, bool isWrite = false)
        //{
        //    IEnumerable<T> result;

        //    using (DbContext dbContext = new DbContext(_dbName, isWrite))
        //    {
        //        var dbFactory = new DataBaseFactory(dbContext.DatabaseType);
        //        string sql = dbFactory.GetSqlPages(sqlSearchInfo, pageIndex, pageSize, sqlOrderby);
        //        string sqlCount = SqlDialectBase.GetSqlCount(sqlSearchInfo);

        //        result = dbContext.Connection.Query<T>(sql, param as object);
        //        nCount = dbContext.Connection.ExecuteScalar<int>(sqlCount, param as object);
        //    }

        //    return result;
        //}
    }
}