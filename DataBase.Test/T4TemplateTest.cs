using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataBase.T4Template;

namespace DataBase.Test
{
    [TestClass]
    public class T4TemplateTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            T4Helper dbRender = new T4Helper(@"D:\duanbi\DataBase\DataBase.Test\", "CarrefourEC");//数据库名
            dbRender.NamespaceStr = "DataBase.Test.TableEntity";//命名空间
            dbRender.TableName = "Bank";//生成的实体表名
            dbRender.ColumnsPrefix = "";//生成属性要忽略的前缀
            dbRender.EntitySuffix = "Entity";//生成实体名后缀，可不传默认为Entity
            dbRender.Render();//执行生成
        }
    }
}
