using System;
using System.Linq;

namespace DataBase.Data
{
    public class TableUnity
    {
        public static string GetTableName(Type type)
        {
            string name = string.Empty;
            var tableattr = type.GetCustomAttributes(false).Where(attr => attr.GetType().Name == "TableAttribute").SingleOrDefault() as
                dynamic;
            if (tableattr != null)
            {
                name = string.Format("{0}", tableattr.TableName);
            }

            return name;
        }
    }
}