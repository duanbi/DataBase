using DataBase.Data;
using DataBase.Test.TableEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Test.Repository
{
    public class BankRepository : Repository<BankEntity, int>
    {
        public BankRepository() : base("Data Source =.; Initial Catalog = CarrefourEC; User ID = sa; Password=mrf@2018")
        {
        }
    }
}
