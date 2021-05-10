using Infrastructure.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.CommandBus
{
    public class Dispatcher : ILoggerProvider
    {
        public Dispatcher()
        {
            var discoveredCommands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);
            if (!SetupCommands(discoveredCommands))
                return;

            OnCommandSetup(discoveredCommands);

        }
        internal bool SetupCommands(Dictionary<string, ICommand> discoveredCommands)
        {
            IEnumerable<ICommand> commands;
            if (!TryLoadCommands(out commands))
            {
                Logger.Error("Failed load commands from the command loader.");
                return false;
            }
            if (commands != null && commands.Any())
            {
                foreach (var c in commands)
                {
                    if (discoveredCommands.ContainsKey(c.Name))
                    {
                        Logger.Error("Duplicated name command has been found! Command name: " + c.Name);
                        return false;
                    }

                    var castedCommand = c as ICommand;

                    if (castedCommand == null)
                    {
                        Logger.Error("Invalid command has been found! Command name: " + c.Name);
                        return false;
                    }

                    Logger.Debug(string.Format("The command {0}({1}) has been discovered", castedCommand.Name, castedCommand.ToString()));

                    discoveredCommands.Add(c.Name, castedCommand);
                }
            }
            return true;
        }

        private bool TryLoadCommands(out IEnumerable<ICommand> commands)
        {
            commands = null;

            var commandAssemblies = new List<Assembly>();
            if (!commandAssemblies.Any())
            {
                commandAssemblies.Add(Assembly.GetEntryAssembly());
            }

            var outputCommands = new List<ICommand>();

            foreach (var assembly in commandAssemblies)
            {
                try
                {
                    outputCommands.AddRange(GetImplementedObjectsByInterface<ICommand>(assembly));
                }
                catch (Exception exc)
                {
                    Logger.Error(string.Format("Failed to get commands from the assembly {0}!", assembly.FullName), exc);
                    return false;
                }
            }

            commands = outputCommands;

            return true;
        }

        public static IEnumerable<TBaseInterface> GetImplementedObjectsByInterface<TBaseInterface>(Assembly assembly)
        where TBaseInterface : class
        {
            return GetImplementedObjectsByInterface<TBaseInterface>(assembly, typeof(TBaseInterface));
        }

        public static IEnumerable<TBaseInterface> GetImplementedObjectsByInterface<TBaseInterface>(Assembly assembly, Type targetType)
            where TBaseInterface : class
        {
            Type[] arrType = assembly.GetExportedTypes();

            var result = new List<TBaseInterface>();

            for (int i = 0; i < arrType.Length; i++)
            {
                var currentImplementType = arrType[i];

                if (currentImplementType.IsAbstract)
                    continue;

                if (!targetType.IsAssignableFrom(currentImplementType))
                    continue;

                result.Add((TBaseInterface)Activator.CreateInstance(currentImplementType));
            }

            return result;
        }

        private void OnCommandSetup(IDictionary<string, ICommand> discoveredCommands)
        {
            var commandContainer = new Dictionary<string, CommandInfo<ICommand>>(StringComparer.OrdinalIgnoreCase);

            foreach (var command in discoveredCommands.Values)
            {
                commandContainer.Add(command.Name, new CommandInfo<ICommand>(command));
            }

            Interlocked.Exchange(ref m_CommandContainer, commandContainer);
        }

        private Dictionary<string, CommandInfo<ICommand>> m_CommandContainer;

        public ILog Logger { get; private set; } = new NopLogger();

        public void ExecuteCommand(object sender, IRequestInfo requestInfo)
        {
            var commandProxy = GetCommandByName(requestInfo.Key);
            commandProxy.Command.CallCommand(sender, requestInfo);
        }
        private CommandInfo<ICommand> GetCommandByName(string commandName)
        {
            CommandInfo<ICommand> commandProxy;

            if (m_CommandContainer.TryGetValue(commandName, out commandProxy))
                return commandProxy;
            else
                return null;
        }

        public void WithLogger(ILog logger)
        {
            this.Logger = logger;
        }
    }
}
