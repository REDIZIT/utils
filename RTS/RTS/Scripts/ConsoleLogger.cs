using System;
using Microsoft.Extensions.Logging;

namespace RTS
{
	public class ConsoleLogger : ILogger
	{
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			string message = formatter(state, exception);
			switch (logLevel)
			{
				case LogLevel.Trace:
				case LogLevel.Debug:
				case LogLevel.Information:
					Console.WriteLine(message);
					break;
				case LogLevel.Warning:
					Console.WriteLine(message);
					break;
				case LogLevel.Error:
				case LogLevel.Critical:
					Console.WriteLine(message);
					break;
				case LogLevel.None:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
			}
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public IDisposable BeginScope<TState>(TState state) where TState : notnull
		{
			throw new NotImplementedException();
		}
	}
}