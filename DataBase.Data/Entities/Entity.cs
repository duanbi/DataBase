using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBase.Data.Helper;

namespace DataBase.Data
{
    [Serializable]
    public abstract class Entity<TPrimaryKey> : IEntity<TPrimaryKey>
    {
    }

    [Serializable]
    public abstract class Entity : Entity<int>, IEntity, IEntity<int>
    {
    }
}
