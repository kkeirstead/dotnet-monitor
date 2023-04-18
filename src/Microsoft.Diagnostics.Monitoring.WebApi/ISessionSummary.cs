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
        public Dictionary<string, CollectionRuleSummary> CollectionRule { get; set; } = new Dictionary<string, CollectionRuleSummary>();
    }

    public class CollectionRuleSummary
    {
        public Dictionary<string, CollectionRuleSessionSummary> Summary { get; set; } = new Dictionary<string, CollectionRuleSessionSummary>();
        internal Dictionary<Guid, List<CollectionRuleActivity>> Activity { get; set; } = new Dictionary<Guid, List<CollectionRuleActivity>>(); // simplifying to just use runtime guid for now
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class CollectionRuleActivity
    {
        public TimeSpan Timestamp { get; set; }
        public string State { get; set; } // might use enum
        public string CollectionRuleName { get; set; }

    }

    public class CollectionRuleSessionSummary
    {
        public int TotalCount { get; set; }
        internal Dictionary<IProcessInfo, int> CountPerProcess { get; set; } = new Dictionary<IProcessInfo, int>();
    }
}
