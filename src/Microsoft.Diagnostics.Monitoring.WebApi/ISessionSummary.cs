// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public interface ISessionSummary
    {
        List<OperationsSummary> Operations { get; set; }
        Dictionary<TimeSpan, ConfigurationSummary> Configuration { get; set; }
        CollectionRulesSummary CollectionRules { get; set; }
    }

    public class OperationsSummary
    {
        public string ArtifactName { get; set; }
        public TimeSpan Time { get; set; }
        public string EgressProvider { get; set; }
        public TimeSpan Duration { get; set; }
        internal IProcessInfo ProcessInfo { get; set; }
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
