using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public class ExpressionHelper
    {
        /// <summary>
        /// 获取属性名称
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static string GetPropertyName(BinaryExpression body)
        {
            string text = body.Left.ToString().Split(new char[]
            {
                '.'
            })[1];
            if (body.Left.NodeType == ExpressionType.Convert)
            {
                text = text.Replace(")", string.Empty);
            }
            return text;
        }

        public static string GetPropertyName<TSource, TField>(Expression<Func<TSource, TField>> field)
        {
            if (object.Equals(field, null))
            {
                throw new NullReferenceException("Field is required");
            }
            MemberExpression memberExpression = field.Body as MemberExpression;
            MemberExpression memberExpression2;
            if (memberExpression != null)
            {
                memberExpression2 = memberExpression;
            }
            else
            {
                UnaryExpression unaryExpression = field.Body as UnaryExpression;
                if (unaryExpression == null)
                {
                    string message = string.Format("Expression '{0}' not supported.", field);
                    throw new ArgumentException(message, field.Name);
                }
                memberExpression2 = (MemberExpression)unaryExpression.Operand;
            }
            return memberExpression2.Member.Name;
        }

        public static object GetValue(Expression member)
        {
            UnaryExpression body = Expression.Convert(member, typeof(object));
            Expression<Func<object>> expression = Expression.Lambda<Func<object>>(body, new ParameterExpression[0]);
            Func<object> func = expression.Compile();
            return func();
        }

        /// <summary>
        /// 根据Lambda表达式替换 其中的and、Equal 为sql语句对应的内容
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSqlOperator(ExpressionType type)
        {
            if (type <= ExpressionType.LessThanOrEqual)
            {
                switch (type)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        {
                            string result = "AND";
                            return result;
                        }
                    default:
                        switch (type)
                        {
                            case ExpressionType.Equal:
                                {
                                    string result = "=";
                                    return result;
                                }
                            case ExpressionType.GreaterThan:
                                {
                                    string result = ">";
                                    return result;
                                }
                            case ExpressionType.GreaterThanOrEqual:
                                {
                                    string result = ">=";
                                    return result;
                                }
                            case ExpressionType.LessThan:
                                {
                                    string result = "<";
                                    return result;
                                }
                            case ExpressionType.LessThanOrEqual:
                                {
                                    string result = "<=";
                                    return result;
                                }
                        }
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case ExpressionType.NotEqual:
                        {
                            string result = "!=";
                            return result;
                        }
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        {
                            string result = "OR";
                            return result;
                        }
                    default:
                        if (type == ExpressionType.Default)
                        {
                            string result = string.Empty;
                            return result;
                        }
                        break;
                }
            }
            throw new NotImplementedException();
        }

        public static BinaryExpression GetBinaryExpression(Expression expression)
        {
            BinaryExpression binaryExpression = expression as BinaryExpression;
            return binaryExpression ?? Expression.MakeBinary(ExpressionType.Equal, expression, Expression.Constant(true));
        }

        public static Func<PropertyInfo, bool> GetPrimitivePropertiesPredicate()
        {
            return (PropertyInfo p) => p.PropertyType.IsValueType || p.PropertyType.Name.Equals("String", StringComparison.OrdinalIgnoreCase);
        }
    }
}
