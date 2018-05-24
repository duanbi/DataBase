using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public class PropertyMetadata
    {
        /// <summary>
        /// 属性
        /// </summary>
        public PropertyInfo PropertyInfo
        {
            get;
            set;
        }

        /// <summary>
        /// 自定义属性名
        /// </summary>
        public string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name
        {
            get
            {
                return this.PropertyInfo.Name;
            }
        }

        /// <summary>
        /// 属性名（如果有自定义则使用自定义名称）
        /// </summary>
        public string ColumnName
        {
            get
            {
                return string.IsNullOrEmpty(this.Alias) ? this.PropertyInfo.Name : this.Alias;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="propertyInfo"></param>
        public PropertyMetadata(PropertyInfo propertyInfo)
        {
            this.PropertyInfo = propertyInfo;
            ColumnAttribute customAttribute = PropertyInfo.GetCustomAttribute<ColumnAttribute>();
            this.Alias = customAttribute != null ? customAttribute.ColumnName : string.Empty;
        }
    }
}
