using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowEngine
{
    public class Precondition
    {
        private int time = 0;
        public void RegisterSignal()
        {
            Interlocked.Increment(ref time);
        }
        public void SetSignal()
        {
            Interlocked.Decrement(ref time);
        }
        public bool IsReady()
        {
            return time <= 0;
        }
    }
}
