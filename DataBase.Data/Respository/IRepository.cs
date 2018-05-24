using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DataBase.Data;

namespace DataBase.Data
{
    public interface IRepository<TEntity, TPrimaryKey> where TEntity : class, IEntity<TPrimaryKey>
    {
        TEntity GetById(TPrimaryKey id);

        IEnumerable<TEntity> GetAll();

        long InsertIdentity(TEntity entity, ITransaction transaction = null);

        bool Insert(TEntity entity, ITransaction transaction = null);

        TEntity FindFirst(Expression<Func<TEntity, bool>> expression);

        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> expression);

        int Count(Expression<Func<TEntity, bool>> expression);

        bool Delete(TEntity entity, ITransaction transaction = null);

        bool Delete(Expression<Func<TEntity, bool>> expression, ITransaction transaction = null);

        bool Update(TEntity entity, ITransaction transaction = null);

        //IEnumerable<T> QueryPager<T>(string sqlSearchInfo, string sqlOrderby, ref int nCount, int pageIndex = 1, int pageSize = 10, dynamic param = null, bool isWrite = false);
    }
}