// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperationFactory : IExceptionsOperationFactory
    {
        public IArtifactOperation Create(IExceptionsStore store, ExceptionsFormat format, ExceptionsConfiguration configuration)
        {
            return new ExceptionsOperation(store, format, configuration);
        }
    }
}
