using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DataBase.Data;

namespace DataBase.Data
{
    public class SqlGenerator<TEntity, TPrimaryKey> : ISqlGenerator<TEntity, TPrimaryKey> where TEntity : class, IEntity<TPrimaryKey>
    {
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// 所有公共属性
        /// </summary>
        public PropertyInfo[] AllProperties { get; private set; }

        /// <summary>
        /// 单个主键属性
        /// </summary>
        public PropertyMetadata IdentityProperty { get; private set; }

        /// <summary>
        /// 所有key属性
        /// </summary>
        public IEnumerable<PropertyMetadata> KeyProperties { get; private set; }

        /// <summary>
        /// 所有属性
        /// </summary>
        public IEnumerable<PropertyMetadata> BaseProperties { get; private set; }

        /// <summary>
        /// sql产生器（构造函数）
        /// </summary>
        public SqlGenerator()
        {
            var entityType = typeof(TEntity);
            var entityTypeInfo = entityType.GetTypeInfo();
            var aliasAttribute = entityTypeInfo.GetCustomAttribute<TableAttribute>();

            this.TableName = aliasAttribute != null ? aliasAttribute.TableName : entityTypeInfo.Name;
            AllProperties = entityType.GetProperties();
            //Load all the "primitive" entity properties
            var props = AllProperties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

            //Filter the non stored properties
            this.BaseProperties = props.Where(p => !p.GetCustomAttributes<IgnoreAttribute>().Any()).Select(p => new PropertyMetadata(p));

            //Filter key properties
            this.KeyProperties = props.Where(p => p.GetCustomAttributes<KeyAttribute>().Any()).Select(p => new PropertyMetadata(p));

            //Use identity as key pattern
            var identityProperty = props.FirstOrDefault(p => p.GetCustomAttributes<KeyAttribute>().Any());
            this.IdentityProperty = identityProperty != null ? new PropertyMetadata(identityProperty) : null;
        }

        /// <summary>
        /// Lambda查询语句
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual SqlQuery GetSelect(Expression<Func<TEntity, bool>> predicate)
        {
            return this.GetSelect(predicate, false);
        }

        public SqlQuery GetSelect(TPrimaryKey id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 构造Count sql语句
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual SqlQuery GetCount(Expression<Func<TEntity, bool>> predicate)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(string.Format("SELECT COUNT(1) FROM {0} ", this.TableName));
            return this.GetSqlQuery(predicate, stringBuilder);
        }

        /// <summary>
        /// 构造Delete
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual SqlQuery GetDelete(Expression<Func<TEntity, bool>> predicate)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(string.Format("DELETE FROM {0} ", this.TableName));
            return this.GetSqlQuery(predicate, stringBuilder);
        }

        /// <summary>
        /// 构造Update
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public virtual SqlQuery GetUpdate(HashSet<string> fields,TEntity t)
        {
            StringBuilder stringBuilder = new StringBuilder();
            IDictionary<string, object> dictionary = new ExpandoObject();
            stringBuilder.AppendFormat(" update {0} set ", this.TableName);
            Func<PropertyMetadata, string> projectionFunction = p =>string.Format(" {0} = @{1} ", p.ColumnName, p.Name);

            var fieldList = BaseProperties.Where(p => fields.Contains(p.Name)).ToList();
            stringBuilder.Append(string.Join(", ", fieldList.Select(projectionFunction)));
            stringBuilder.Append(" where ");
            stringBuilder.Append(string.Join(", ", KeyProperties.Select(projectionFunction)));
            var propertyMetadatas =new List<PropertyMetadata>();
            propertyMetadatas.AddRange(fieldList);
            propertyMetadatas.AddRange(KeyProperties);
            foreach (var propertyMetadata in propertyMetadatas)
            {
                dictionary.Add(propertyMetadata.Name, propertyMetadata.PropertyInfo.GetValue(t));
            }

            return new SqlQuery(stringBuilder.ToString().TrimEnd(new char[0]), dictionary);
        }

        /// <summary>
        /// 构建select字段
        /// </summary>
        /// <param name="firstOnly"></param>
        /// <returns></returns>
        private StringBuilder InitBuilderSelect(bool firstOnly)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string text = "SELECT ";
            if (firstOnly)
            {
                text += "TOP 1 ";
            }
            stringBuilder.Append(string.Format("{0} {1}", text, GetFieldsSelect(this.TableName, this.BaseProperties)));
            return stringBuilder;
        }

        /// <summary>
        /// sql语句Select+字段
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static string GetFieldsSelect(string tableName, IEnumerable<PropertyMetadata> properties)
        {
            Func<PropertyMetadata, string> projectionFunction = p =>
             !string.IsNullOrEmpty(p.Alias)
             ? string.Format(" {0}.{1} AS {2} ", tableName, p.ColumnName, p.Name)
             : string.Format("{0}.{1} ", tableName, p.ColumnName);

            return string.Join(", ", properties.Select(projectionFunction));
        }

        /// <summary>
        /// sql语句From
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="firstOnly"></param>
        /// <returns></returns>
        private SqlQuery GetSelect(Expression<Func<TEntity, bool>> predicate, bool firstOnly)
        {
            StringBuilder stringBuilder = this.InitBuilderSelect(firstOnly);
            stringBuilder.Append(string.Format(" FROM {0} ", this.TableName));
            return this.GetSqlQuery(predicate, stringBuilder);
        }

        /// <summary>
        /// 讲Lambda表达式转化为SQL语句以及参数化值
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        private SqlQuery GetSqlQuery(Expression<Func<TEntity, bool>> predicate, StringBuilder builder)
        {
            IDictionary<string, object> dictionary = new ExpandoObject();
            if (predicate != null)
            {
                List<QueryParameter> list = new List<QueryParameter>();
                this.FillQueryProperties(ExpressionHelper.GetBinaryExpression(predicate.Body), ExpressionType.Default, ref list);
                builder.Append(" WHERE ");
                for (int i = 0; i < list.Count; i++)
                {
                    QueryParameter queryParameter = list[i];
                    if (!string.IsNullOrEmpty(queryParameter.LinkingOperator) && i > 0)
                    {
                        builder.Append(string.Format("{0} {1}.{2} {3} @{2} ", new object[]
                        {
                            queryParameter.LinkingOperator,
                            this.TableName,
                            queryParameter.PropertyName,
                            queryParameter.QueryOperator
                        }));
                    }
                    else
                    {
                        builder.Append(string.Format("{0}.{1} {2} @{1} ", this.TableName, queryParameter.PropertyName, queryParameter.QueryOperator));
                    }
                    dictionary[queryParameter.PropertyName] = queryParameter.PropertyValue;
                }
            }
            return new SqlQuery(builder.ToString().TrimEnd(new char[0]), dictionary);
        }

        private void FillQueryProperties(BinaryExpression body, ExpressionType linkingType, ref List<QueryParameter> queryProperties)
        {
            if (body.NodeType != ExpressionType.AndAlso && body.NodeType != ExpressionType.OrElse)
            {
                string columnName = this.BaseProperties.First((PropertyMetadata e) => e.Name == ExpressionHelper.GetPropertyName(body)).ColumnName;
                object value = ExpressionHelper.GetValue(body.Right);
                string sqlOperator = ExpressionHelper.GetSqlOperator(body.NodeType);
                string sqlOperator2 = ExpressionHelper.GetSqlOperator(linkingType);
                queryProperties.Add(new QueryParameter(sqlOperator2, columnName, value, sqlOperator));
            }
            else
            {
                this.FillQueryProperties(ExpressionHelper.GetBinaryExpression(body.Left), body.NodeType, ref queryProperties);
                this.FillQueryProperties(ExpressionHelper.GetBinaryExpression(body.Right), body.NodeType, ref queryProperties);
            }
        }
    }
}