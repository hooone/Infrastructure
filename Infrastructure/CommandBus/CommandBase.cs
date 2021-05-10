using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CommandBus
{
    public abstract class CommandBase<TRequestInfo> : ICommand
       where TRequestInfo : IRequestInfo
    {
        public abstract void ExecuteCommand(object session, TRequestInfo requestInfo);
        public virtual string Name
        {
            get { return this.GetType().Name; }
        }
        public override string ToString()
        {
            return this.GetType().AssemblyQualifiedName;
        }

        public void CallCommand(object session, IRequestInfo requestInfo)
        {
            ExecuteCommand(session, (TRequestInfo)requestInfo);
        }
    }
}
