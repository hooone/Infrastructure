using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine
{
    public class Postcondition
    {
        List<Precondition> dests = new List<Precondition>();

        public void RegisterDest(Precondition dest)
        {
            if (dests.Contains(dest))
                return;
            dest.RegisterSignal();
            dests.Add(dest);
        }
        public void SetSignal()
        {
            foreach (var item in dests)
            {
                item.SetSignal();
            }
        }
    }
}
