using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataBase.T4Template;
using DataBase.Test.Repository;

namespace DataBase.Test
{
    [TestClass]
    public class DataTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var repository = new BankRepository();


            repository.GetAll();



            


            var query1 = repository.GetAll();
            var query2 = repository.Find(x => x.Id == 191);

            

        }
    }
}
