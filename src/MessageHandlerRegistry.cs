using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Thon.Hotels.FishBus
{
    public class MessageHandlerRegistry
    {
        private Dictionary<Type, ICollection<Type>> MessageHandlers { get; }
        private IServiceProvider ServiceProvider { get; }

        public MessageHandlerRegistry(IServiceProvider serviceProvider, Func<IEnumerable<Type>> messageHandlerTypes)
        {
            ServiceProvider = serviceProvider;
            MessageHandlers = new Dictionary<Type, ICollection<Type>>();
            Init(messageHandlerTypes);
        }

        private void Init(Func<IEnumerable<Type>> messageHandlerTypes)
        {
            messageHandlerTypes()
                .SelectMany(t => GetHandledCommands(t))
                .ToList()
                .ForEach(AddHandledCommand);
        }

        private IEnumerable<(Type handler, Type message)> GetHandledCommands(Type messageHandlerType)
        {
            return messageHandlerType
                            .GetInterfaces()
                            .Where(i => typeof(IHandleMessage<>).IsAssignableFrom(i.GetGenericTypeDefinition()))
                            .Select(i => (handler: messageHandlerType, message: i.GenericTypeArguments.Single()));
        }

        private void AddHandledCommand((Type handler, Type message) x)
        {
            if (!MessageHandlers.ContainsKey(x.message))
                MessageHandlers.Add(x.message, new List<Type>());
            MessageHandlers[x.message].Add(x.handler);
        }

        public IEnumerable<object> GetHandlers(Type messageType, IServiceScope scope)
        {
            return MessageHandlers.ContainsKey(messageType) ?
                MessageHandlers[messageType]
                    .Select(t => scope.ServiceProvider.GetRequiredService(t)) :
                    new List<object>();
        }
    }
}
