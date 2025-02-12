// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public abstract class IFeatureAvailabilityInfo
    {
        //public ExceptionsInfo Exceptions { get; set; }
        //public ParametersInfo Parameters { get; set; }
        //public CallStacksInfo CallStacks { get; set; }
        //public MetricsInfo Metrics { get; set; }
    }

    public class FeatureAvailabilityInfo : IFeatureAvailabilityInfo
    {
        [JsonPropertyName("exceptions")]
        public required ExceptionsInfo Exceptions { get; set; }
        [JsonPropertyName("parameters")]
        public required ParametersInfo Parameters { get; set; }
        [JsonPropertyName("callStacks")]
        public required CallStacksInfo CallStacks { get; set; }
        [JsonPropertyName("metrics")]
        public required MetricsInfo Metrics { get; set; }
    }

    public class ExceptionsInfo
    {
        public bool Enabled { get; set; }
    }

    public class ParametersInfo
    {
        public bool Enabled { get; set; }
    }

    public class CallStacksInfo
    {
        public bool Enabled { get; set; }
    }

    public class MetricsInfo
    {
        public bool Enabled { get; set; }
    }
}
