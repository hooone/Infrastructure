using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CommandBus
{
    internal interface ICommand
    {
        string Name { get; }
        void CallCommand(object session, IRequestInfo requestInfo);
    }
}
