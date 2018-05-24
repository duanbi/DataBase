using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public class QueryParameter
    {
        public string LinkingOperator
        {
            get;
            set;
        }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string PropertyName
        {
            get;
            set;
        }

        /// <summary>
        /// 属性值
        /// </summary>
        public object PropertyValue
        {
            get;
            set;
        }

        /// <summary>
        /// Lambda操作节点替换成sql
        /// </summary>
        public string QueryOperator
        {
            get;
            set;
        }

        public QueryParameter(string linkingOperator, string propertyName, object propertyValue, string queryOperator)
        {
            this.LinkingOperator = linkingOperator;
            this.PropertyName = propertyName;
            this.PropertyValue = propertyValue;
            this.QueryOperator = queryOperator;
        }
    }
}
