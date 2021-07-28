using Infrastructure.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Model
{
    [DbTable("LINK")]
    public class Link : IDbModel
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }
}
