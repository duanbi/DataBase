using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DataBase.Data;

namespace DataBase.Data
{
    public interface ISqlGenerator<TEntity, TPrimaryKey> where TEntity : class, IEntity<TPrimaryKey>
    {
        string TableName { get; }

        IEnumerable<PropertyMetadata> KeyProperties { get; }

        IEnumerable<PropertyMetadata> BaseProperties { get; }

        PropertyMetadata IdentityProperty { get; }

        SqlQuery GetSelect(Expression<Func<TEntity, bool>> predicate);

        SqlQuery GetSelect(TPrimaryKey id);

        SqlQuery GetCount(Expression<Func<TEntity, bool>> predicate);

        SqlQuery GetDelete(Expression<Func<TEntity, bool>> predicate);

        SqlQuery GetUpdate(HashSet<string> fields, TEntity t);


        //SqlQuery GetUpdate(Expression<Func<TEntity, bool>> predicate);

    }
}
