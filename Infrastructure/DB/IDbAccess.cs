using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.DB
{
    public interface IDbAccess
    {
        SqlHelper Helper { get; set; }
    }
}
