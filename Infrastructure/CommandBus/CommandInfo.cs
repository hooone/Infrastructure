using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.CommandBus
{
    class CommandInfo<TCommand> where TCommand : ICommand
    {
        public TCommand Command { get; private set; }
        public CommandInfo(TCommand command)
        {
            Command = command;

        }
    }
}
