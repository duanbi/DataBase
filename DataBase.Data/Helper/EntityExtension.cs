/*======================================================================
 Copyright (c) 同程网络科技股份有限公司. All rights reserved.
 所属项目：TC.Dis.SOA.Order.Core
 创 建 人：hw10194
 创建日期：2015/12/23 16:10:27
 用    途：
========================================================================*/

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace DbBase.Helper
{
    public static class EntityExtension
    {
        public static IEnumerable<TDestObject> Clone<TDestObject>(this IEnumerable srcCollection, params string[] excludeProperties) where TDestObject : new()
        {
            var result = new List<TDestObject>();
            if (srcCollection == null) return result;
            foreach (var srcItem in srcCollection)
            {
                var destItem = new TDestObject();
                srcItem.CopyTo(destItem, excludeProperties);
                result.Add(destItem);
            }
            return result;
        }

        public static void CopyTo(this object srcObj, object destObj, params string[] excludedProperties)
        {
            if (srcObj == null | destObj == null)
            {
                return;
            }

            var srcType = srcObj.GetType();
            var destType = destObj.GetType();

            var srcProperties = srcType.GetProperties().Where(p => p.CanRead);
            var destProperties = destType.GetProperties().Where(p => p.CanWrite
                    && (excludedProperties != null && !excludedProperties.Contains(p.Name)));

            foreach (var prop in destProperties)
            {
                var srcProp = srcProperties.FirstOrDefault(p => p.Name == prop.Name && p.PropertyType == prop.PropertyType);

                if (srcProp == null)
                {
                    continue;
                }

                var value = srcProp.GetValue(srcObj, null);
                prop.SetValue(destObj, value, null);
            }
        }

        /// <summary>
        /// 转换为Expando
        /// </summary>
        /// <param name="value"></param>
        /// <param name="blacklist">黑名单</param>
        /// <returns></returns>
        public static dynamic ToDynamic(this object value, IEnumerable<string> blacklist = null)
        {
            IDictionary<string, object> expando = new ExpandoObject();
            var excludeLookup = blacklist == null ? new[] { "EntityType", "Namespace", "UrlConfig" }.ToLookup(x => x) : blacklist.ToLookup(x => x);
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
            {
                if (!excludeLookup.Contains(property.Name))
                {
                    GetChildProperty(property, expando, value, "");
                }
            }
               

            return (ExpandoObject) expando;
        }


        private static void GetChildProperty(PropertyDescriptor p, IDictionary<string, object> expando, object obj,
            string pre)
        {
            string prefix = (string.IsNullOrEmpty(pre) ? "" : pre + "_");
            if (p.GetChildProperties().Count == 0 ||
                ((!p.PropertyType.IsClass && p.PropertyType.IsInterface) || p.PropertyType.Module.ScopeName.Equals("CommonLanguageRuntimeLibrary")))
            {
                expando.Add(prefix + p.Name, p.GetValue(obj));
            }
            else
            {
                foreach (PropertyDescriptor pp in p.GetChildProperties())
                {
                    var value = p.GetValue(obj);

                    { GetChildProperty(pp, expando, value, prefix + p.Name); }
                }
            }
        }
    }
}
