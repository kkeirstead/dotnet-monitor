// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class SessionSummary : ISessionSummary
    {
        public List<OperationsSummary> Operations { get; set; } = new();
        public Dictionary<TimeSpan, ConfigurationSummary> Configuration { get; set; }
        public CollectionRulesSummary CollectionRules { get; set; } = new CollectionRulesSummary();
    }
}
