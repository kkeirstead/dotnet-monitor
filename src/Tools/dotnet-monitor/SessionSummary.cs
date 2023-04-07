// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class SessionSummary : ISessionSummary
    {
        public List<OperationSummary> Operations { get; set; }
        public Dictionary<TimeSpan, ConfigurationSummary> Configuration { get; set; }
        public CollectionRulesSummary CollectionRules { get; set; }
    }

    public class OperationSummary
    {
        public string ArtifactName { get; set; }
        public TimeSpan Time { get; set; }
        public string EgressProvider { get; set; }
        public TimeSpan Duration { get; set; }
        public ProcessFilterDescriptor ProcessFilter { get; set; }
        public bool Success { get; set; }
    }

    public class ConfigurationSummary
    {
        public string Json { get; set; }
        public string ConfigMap { get; set; }
        public string EnvironmentVariables { get; set; }
    }

    public class CollectionRulesSummary
    {
        public List<CollectionRuleSummary> CollectionRule { get; set; }
    }

    public class CollectionRuleSummary
    {
        public Dictionary<ProcessFilterDescriptor, List<CollectionRuleActivity>> Activity { get; set; }
        public string Name { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class CollectionRuleActivity
    {

    }
}
