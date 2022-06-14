// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing a user's anomaly detection preferences.
    /// </summary>
    internal sealed partial class AnomalyDetectionOptions
    {
        // Manually choose which of the available counters to perform anomaly detection on (by default, all are turned on)
        public List<string> CounterNames { get; set; }

        // Choose how many artifacts can be collected automatically for each counter (by default, 5)
        public int ArtifactCount { get; set; }

        // An abstracted means of letting users determine how aggressive the anomaly detection should be
        public AnomalyConfidence Confidence { get; set; }

        // The processes that this should be applied to
        public List<ProcessFilterDescriptor> Filters { get; } = new List<ProcessFilterDescriptor>(0);

        // For space constrained environments, can just report back times/instances where a condition was observed without collecting an artifact
        public bool LoggingOnly { get; set; }

        // Where artifacts should be egressed; required UNLESS a default is set, OR LoggingOnly is true
        public string Egress { get; set; }
    }

    // Low Confidence -> Only slightly above/below what's typical
    // High Confidence -> Greatly above/below what's typical
    public enum AnomalyConfidence
    {
        Low, Medium, High
    }
}
