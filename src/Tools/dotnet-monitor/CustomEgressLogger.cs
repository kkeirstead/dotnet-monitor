// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class CustomEgressLogger : ILogger
    {
        private readonly IServiceProvider _serviceProvider;
        public CustomEgressLogger(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;


        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            
        }
    }
}
