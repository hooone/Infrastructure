using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.DB
{
    public interface IConnectionString
    {
        string ConnectionString { get; }
    }
}
