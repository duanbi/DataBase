using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using DataBase.Data;

namespace DataBase.Data
{
    public static class DbContextExtensions
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> KeyProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> ComputedProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> GetQueries = new ConcurrentDictionary<RuntimeTypeHandle, string>();
        private static readonly ConcurrentDictionary<string, string> TypeTableName = new ConcurrentDictionary<string, string>();

        private static IEnumerable<PropertyInfo> ComputedPropertiesCache(Type type)
        {
            IEnumerable<PropertyInfo> pi;
            if (ComputedProperties.TryGetValue(type.TypeHandle, out pi))
            {
                return pi;
            }

            var computedProperties = TypePropertiesCache(type).Where(p => p.GetCustomAttributes(true).Any(a => a is ComputedAttribute)).ToList();

            ComputedProperties[type.TypeHandle] = computedProperties;
            return computedProperties;
        }

        private static IEnumerable<PropertyInfo> KeyPropertiesCache(Type type)
        {
            IEnumerable<PropertyInfo> pi;
            if (KeyProperties.TryGetValue(type.TypeHandle, out pi))
            {
                return pi;
            }

            var allProperties = TypePropertiesCache(type);
            var keyProperties = allProperties.Where(p => p.GetCustomAttributes(true).Any(a => a is KeyAttribute)).ToList();

            if (keyProperties.Count == 0)
            {
                var idProp = allProperties.Where(p => p.Name.ToLower() == "id").FirstOrDefault();
                if (idProp != null)
                {
                    keyProperties.Add(idProp);
                }
            }

            KeyProperties[type.TypeHandle] = keyProperties;
            return keyProperties;
        }

        private static IEnumerable<PropertyInfo> TypePropertiesCache(Type type)
        {
            IEnumerable<PropertyInfo> pis;
            if (TypeProperties.TryGetValue(type.TypeHandle, out pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsIgnore).ToArray();
            TypeProperties[type.TypeHandle] = properties;
            return properties;
        }

        public static bool IsIgnore(PropertyInfo pi)
        {
            object[] attributes = pi.GetCustomAttributes(typeof(IgnoreAttribute), false);
            if (attributes.Length == 1)
            {
                return false;
            }
            return true;
        }

        public static IDbTransaction GetTransaction(DbContext context, IDbTransaction transaction)
        {
            if (transaction != null) return transaction;
            if (context.Transaction == null) return null;
            if (context.Transaction.DbTransaction != null) return context.Transaction.DbTransaction;
            return null;
        }

        /// <summary>
        /// 根据主键获取实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="id">主键</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>实体</returns>
        public static T Get<T>(this DbContext context, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            string sql;
            if (!GetQueries.TryGetValue(type.TypeHandle, out sql))
            {
                var keys = KeyPropertiesCache(type);
                if (keys.Count() > 1)
                    throw new DataException("Get<T> 只支持单个主键");
                if (keys.Count() == 0)
                    throw new DataException("Get<T> 必须有一个主键");

                var onlyKey = new PropertyMetadata(keys.First());

                var tableName = GetTableName(type);

                var allProperties = type.GetProperties();

                //Load all the "primitive" entity properties
                var props = allProperties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

                //Filter the non Ignore properties
                var baseProperties = props.Where(p => !p.GetCustomAttributes<IgnoreAttribute>().Any()).Select(p => new PropertyMetadata(p));

                var columns = GetFieldsSelect(tableName, baseProperties);

                sql = "select " + columns + " from " + tableName + " where " + onlyKey.ColumnName + " = @id";

                GetQueries[type.TypeHandle] = sql;
            }

            var dynParms = new DynamicParameters();
            dynParms.Add("@id", id);
            T obj = context.Connection.Query<T>(sql, dynParms, GetTransaction(context, transaction), commandTimeout: commandTimeout).FirstOrDefault();
            return obj;
        }

        /// <summary>
        /// 获取所有记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetAll<T>(this DbContext context, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            string sql;
            if (!GetQueries.TryGetValue(type.TypeHandle, out sql))
            {
                var tableName = GetTableName(type);

                var allProperties = type.GetProperties();

                //Load all the "primitive" entity properties
                var props = allProperties.Where(ExpressionHelper.GetPrimitivePropertiesPredicate()).ToArray();

                //Filter the non stored properties
                var baseProperties = props.Where(p => !p.GetCustomAttributes<IgnoreAttribute>().Any()).Select(p => new PropertyMetadata(p));

                var column = GetFieldsSelect(tableName, baseProperties);

                sql = $"SELECT {column} FROM {tableName}";
                GetQueries[type.TypeHandle] = sql;
            }

            return context.Connection.Query<T>(sql, null, GetTransaction(context, transaction), commandTimeout: commandTimeout);
        }

        /// <summary>
        /// 新增记录，返回自增Id
        /// </summary>
        /// <typeparam name="T">类型参数</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="entityToInsert">实体</param>
        /// <param name="returnIdentity">是否返回自增Id</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>自增ID或影响记录数</returns>
        public static long InsertIdentity<T>(this DbContext context, T entityToInsert, bool returnIdentity = true, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var tableName = GetTableName(type, false);

            var sbColumnList = new StringBuilder(null);
            var allProperties = TypePropertiesCache(type);
            var keyProperties = KeyPropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);

            var allPropertiesExceptKeyAndComputed = allProperties.Except(keyProperties.Union(computedProperties)).Select(p => new PropertyMetadata(p)).ToList();

            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count(); i++)
            {
                var property = allPropertiesExceptKeyAndComputed.ElementAt(i);
                sbColumnList.AppendFormat("{0}", property.ColumnName);
                if (i < allPropertiesExceptKeyAndComputed.Count() - 1)
                    sbColumnList.Append(", ");
            }

            var sbParameterList = new StringBuilder(null);
            for (var i = 0; i < allPropertiesExceptKeyAndComputed.Count(); i++)
            {
                var property = allPropertiesExceptKeyAndComputed.ElementAt(i);
                sbParameterList.AppendFormat("@{0}", property.Name);
                if (i < allPropertiesExceptKeyAndComputed.Count() - 1)
                    sbParameterList.Append(", ");
            }

            int id = InsertSql(context.Connection, GetTransaction(context, transaction), commandTimeout, tableName, sbColumnList.ToString(), sbParameterList.ToString(), keyProperties, entityToInsert, returnIdentity);
            return id;
        }

        /// <summary>
        /// 新增记录
        /// </summary>
        /// <typeparam name="T">类型参数</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="entityToInsert">实体</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>是否成功</returns>
        public static bool Insert<T>(this DbContext context, T entityToInsert, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            return InsertIdentity<T>(context, entityToInsert, false, transaction: transaction, commandTimeout: commandTimeout) > 0;
        }

        /// <summary>
        /// 根据主键更新记录
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="entityToUpdate">更新实体</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>是否成功</returns>
        public static bool Update<T>(this DbContext context,T entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);

            var keyProperties = KeyPropertiesCache(type);
            if (!keyProperties.Any())
                throw new ArgumentException("实体中没有主键");

            var name = GetTableName(type, false);

            var sb = new StringBuilder();
            sb.AppendFormat("update {0} set ", name);

            var allProperties = TypePropertiesCache(type);
            var computedProperties = ComputedPropertiesCache(type);
            var nonIdProps = allProperties.Except(keyProperties.Union(computedProperties)).Select(p => new PropertyMetadata(p)).ToList();
            var keys = keyProperties.Select(p => new PropertyMetadata(p)).ToList();

            for (var i = 0; i < nonIdProps.Count(); i++)
            {
                var property = nonIdProps.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.ColumnName, property.Name);
                if (i < nonIdProps.Count() - 1)
                    sb.AppendFormat(", ");
            }
            sb.Append(" where ");
            for (var i = 0; i < keys.Count(); i++)
            {
                var property = keys.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.ColumnName, property.Name);
                if (i < keys.Count() - 1)
                    sb.AppendFormat(" and ");
            }
            var updated = context.Connection.Execute(sb.ToString(), entityToUpdate, GetTransaction(context, transaction), commandTimeout);
            return updated > 0;
        }

        /// <summary>
        /// 根据主键删除记录
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="entityToDelete">删除实体</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>是否成功</returns>
        public static bool Delete<T>(this DbContext context,T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            if (entityToDelete == null)
                throw new ArgumentException("实体不能为空", "entityToDelete");

            var type = typeof(T);

            var keyProperties = KeyPropertiesCache(type);
            if (keyProperties.Count() == 0)
                throw new ArgumentException("实体至少有一个主键");

            var name = GetTableName(type, false);

            var sb = new StringBuilder();
            sb.AppendFormat("delete from {0} where ", name);

            var keys = keyProperties.Select(p => new PropertyMetadata(p)).ToList();

            for (var i = 0; i < keys.Count(); i++)
            {
                var property = keys.ElementAt(i);
                sb.AppendFormat("{0} = @{1}", property.ColumnName, property.Name);
                if (i < keys.Count() - 1)
                    sb.AppendFormat(" and ");
            }
            var deleted = context.Connection.Execute(sb.ToString(), entityToDelete, GetTransaction(context, transaction), commandTimeout: commandTimeout);
            return deleted > 0;
        }

        /// <summary>
        /// 执行SQL
        /// </summary>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>是否成功</returns>
        public static bool Execute(this DbContext context, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return context.Connection.Execute(sql, param as object, GetTransaction(context, transaction), commandTimeout: commandTimeout) > 0;
        }

        /// <summary>
        /// 执行SQL
        /// </summary>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="transaction">事务</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>是否成功</returns>
        public static int ExecuteNonQuery(this DbContext context,string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return context.Connection.Execute(sql, param as object, GetTransaction(context, transaction), commandTimeout: commandTimeout);
        }

        /// <summary>
        /// 查询记录
        /// </summary>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        public static IEnumerable<dynamic> Query(this DbContext context,string sql, dynamic param = null)
        {
            return context.Connection.Query(sql, param as object);
        }

        /// <summary>
        /// 查询记录
        /// </summary>
        /// <typeparam name="T">类型参数</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        public static IEnumerable<T> Query<T>(this DbContext context, string sql, dynamic param = null)
        {
            return context.Connection.Query<T>(sql, param as object);
        }

        /// <summary>
        /// 查询Top1
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="sqlNoteInfo"></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static T QueryTop<T>(this DbContext context, string sql, dynamic param = null)
        {
            return context.Connection.Query<T>(sql, param as object).FirstOrDefault();
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">类型参数</typeparam>
        /// <param name="context">上下文<see cref="context"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sqlSearchInfo">内容查询SQL</param>
        /// <param name="sqlOrderby">Orderby</param>
        /// <param name="nCount">记录数</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        public static IEnumerable<T> QueryPager<T>(this DbContext context,  string sqlSearchInfo,
            string sqlOrderby, ref int nCount, int pageIndex = 1, int pageSize = 10,
            dynamic param = null)
        {
            if (string.IsNullOrEmpty(sqlSearchInfo))
            {
                throw new ArgumentNullException("参数sqlSearchInfo不能为空");
            }

            if (string.IsNullOrEmpty(sqlOrderby))
            {
                throw new ArgumentNullException("sqlOrderby", "分页函数必须有order by");
            }


            var dbFactory = new DataBaseFactory(context.DatabaseType);
            string strSql = dbFactory.GetSqlPages(sqlSearchInfo, pageIndex, pageSize, sqlOrderby);
            string sqlSearchCount = SqlDialectBase.GetSqlCount(sqlSearchInfo);

            nCount = (int)SqlMapper.ExecuteScalar(context.Connection, sqlSearchCount.ToString(), param);
            return context.Connection.Query<T>(strSql.ToString(), param as object);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">类型参数</typeparam>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sqlSearchCount">记录数查询SQL</param>
        /// <param name="sqlSearchInfo">内容查询SQL</param>
        /// <param name="sqlOrderby">Orderby</param>
        /// <param name="nCount">记录数</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        public static IEnumerable<dynamic> QueryPager(this DbContext context, string sqlSearchInfo,
            string sqlOrderby, ref int nCount, int pageIndex = 1, int pageSize = 10,
            dynamic param = null)
        {
            return QueryPager<dynamic>(context, sqlSearchInfo, sqlOrderby, ref nCount, pageIndex, pageSize, param as object);
        }

        /// <summary>
        /// 执行SQL并返回第一行第一列
        /// </summary>
        /// <param name="context">上下文<see cref="DbContext"/></param>
        /// <param name="sqlNoteInfo">SQL注释<see cref="SqlNoteInfo"/></param>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public static object ExecuteScalar(this DbContext context, string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            try
            {
                return SqlMapper.ExecuteScalar(context.Connection, sql, param, GetTransaction(context, transaction), commandTimeout);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 查询返回一个DataTable
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sql"></param>
        /// <param name="sqlNote"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static DataTable QueryTable(this DbContext context,string sql, object param = null)
        {
            var sb = new StringBuilder(sql);
            if (Debugger.IsAttached)
            {
                Trace.WriteLine(string.Format("QueryTable: {0}", sb));
            }
            var dataReader = context.Connection.ExecuteReader(sb.ToString(), param);
            return ConvertDataReaderToDataTable(dataReader);
        }

        private static int InsertSql(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, String tableName, string columnList, string parameterList, IEnumerable<PropertyInfo> keyProperties, object entityToInsert, bool returnIdentity)
        {
            var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
            connection.Execute(cmd, entityToInsert, transaction, commandTimeout);
            var r = connection.Query("Select LAST_INSERT_ID() id", transaction: transaction, commandTimeout: commandTimeout);

            var id = r.First().id;
            if (id == null) return 0;
            var propertyInfos = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
            if (!propertyInfos.Any()) return Convert.ToInt32(id);

            var idp = propertyInfos.First();
            idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

            return Convert.ToInt32(id);
        }

        /// <summary>
        /// DataReader读取DataTable
        /// </summary>
        /// <param name="dataReader"></param>
        /// <returns></returns>
        private static DataTable ConvertDataReaderToDataTable(IDataReader dataReader)
        {
            var datatable = new DataTable();
            try
            {
                //动态添加表的数据列
                for (var i = 0; i < dataReader.FieldCount; i++)
                {
                    var myDataColumn = new DataColumn
                    {
                        DataType = dataReader.GetFieldType(i),
                        ColumnName = dataReader.GetName(i)
                    };
                    datatable.Columns.Add(myDataColumn);
                }

                //添加表的数据
                while (dataReader.Read())
                {
                    var myDataRow = datatable.NewRow();
                    for (var i = 0; i < dataReader.FieldCount; i++)
                    {
                        myDataRow[i] = dataReader[i].ToString();
                    }
                    datatable.Rows.Add(myDataRow);
                    myDataRow = null;
                }
                //关闭数据读取器
                dataReader.Close();
                return datatable;
            }
            catch (Exception ex)
            {
                //抛出类型转换错误
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// 获取表名
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isWithNolock"></param>
        /// <returns></returns>
        private static string GetTableName(Type type, bool isWithNolock = true)
        {
            string key = CryptionHelper.Md5(type.FullName + "_" + isWithNolock.ToString());
            string name;
            if (!TypeTableName.TryGetValue(key, out name))
            {
                name = TableUnity.GetTableName(type);
                TypeTableName[key] = name;
            }
            return name;
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
    }
}