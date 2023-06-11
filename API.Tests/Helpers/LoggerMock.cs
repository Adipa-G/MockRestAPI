using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace API.Tests.Helpers
{
    public class LoggerMock<T> : ILogger<T>
    {
        readonly Stack<ReceivedLogEvent> _events = new Stack<ReceivedLogEvent>();

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _events.Push(new ReceivedLogEvent { Level = logLevel, Message = state.ToString() });
        }

        public void ReceivedOnce(LogLevel level, string messageFragment)
        {
            var matchedEventsCount = _events.Count(e => e.Level == level && e.Message.Contains(messageFragment));

            if (matchedEventsCount != 1)
            {
                throw new Exception($"Expected one call to Log with the following arguments: {level}, containing \"{messageFragment}\". Actual received count: {matchedEventsCount}");
            }
        }
    }
}
