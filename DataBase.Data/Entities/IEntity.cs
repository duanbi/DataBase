﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBase.Data
{
    public interface IEntity<TPrimaryKey>
    {
    }

    public interface IEntity : IEntity<int>
    {
    }
}
