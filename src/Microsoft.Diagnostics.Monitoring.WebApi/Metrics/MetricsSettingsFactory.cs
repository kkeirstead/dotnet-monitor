// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Utility class to create metric settings (for both configuration and on demand metrics).
    /// </summary>
<<<<<<< HEAD:src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/MetricsSettingsFactory.cs
    internal static class MetricsSettingsFactory
    {
        public static MetricsPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, bool includeDefaults,
=======
    internal static class CounterSettingsFactory
    {
        public static CounterPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, bool includeDefaults,
>>>>>>> 94e5c733 (Rename counterpipeline):src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/CounterSettingsFactory.cs
            int durationSeconds)
        {
            return CreateSettings(includeDefaults,
                durationSeconds,
                counterOptions.GetIntervalSeconds(),
                counterOptions.GetMaxHistograms(),
                counterOptions.GetMaxTimeSeries(),
                () => new List<EventPipeCounterGroup>(0));
        }

<<<<<<< HEAD:src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/MetricsSettingsFactory.cs
        public static MetricsPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, MetricsOptions options)
=======
        public static CounterPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, MetricsOptions options)
>>>>>>> 94e5c733 (Rename counterpipeline):src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/CounterSettingsFactory.cs
        {
            return CreateSettings(options.IncludeDefaultProviders.GetValueOrDefault(MetricsOptionsDefaults.IncludeDefaultProviders),
                Timeout.Infinite, counterOptions.GetIntervalSeconds(),
                counterOptions.GetMaxHistograms(),
                counterOptions.GetMaxTimeSeries(),
                () => ConvertCounterGroups(options.Providers));
        }

<<<<<<< HEAD:src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/MetricsSettingsFactory.cs
        public static MetricsPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, int durationSeconds,
=======
        public static CounterPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, int durationSeconds,
>>>>>>> 94e5c733 (Rename counterpipeline):src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/CounterSettingsFactory.cs
            Models.EventMetricsConfiguration configuration)
        {
            return CreateSettings(configuration.IncludeDefaultProviders,
                durationSeconds,
                counterOptions.GetIntervalSeconds(),
                counterOptions.GetMaxHistograms(),
                counterOptions.GetMaxTimeSeries(),
                () => ConvertCounterGroups(configuration.Providers));
        }

<<<<<<< HEAD:src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/MetricsSettingsFactory.cs
        private static MetricsPipelineSettings CreateSettings(bool includeDefaults,
=======
        private static CounterPipelineSettings CreateSettings(bool includeDefaults,
>>>>>>> 94e5c733 (Rename counterpipeline):src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/CounterSettingsFactory.cs
            int durationSeconds,
            float counterInterval,
            int maxHistograms,
            int maxTimeSeries,
            Func<List<EventPipeCounterGroup>> createCounterGroups)
        {
            List<EventPipeCounterGroup> eventPipeCounterGroups = createCounterGroups();

            if (includeDefaults)
            {
                eventPipeCounterGroups.Add(new EventPipeCounterGroup { ProviderName = MonitoringSourceConfiguration.SystemRuntimeEventSourceName, Type = CounterGroupType.EventCounter });
                eventPipeCounterGroups.Add(new EventPipeCounterGroup { ProviderName = MonitoringSourceConfiguration.MicrosoftAspNetCoreHostingEventSourceName, Type = CounterGroupType.EventCounter });
                eventPipeCounterGroups.Add(new EventPipeCounterGroup { ProviderName = MonitoringSourceConfiguration.GrpcAspNetCoreServer, Type = CounterGroupType.EventCounter });
            }

<<<<<<< HEAD:src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/MetricsSettingsFactory.cs
            return new MetricsPipelineSettings
=======
            return new CounterPipelineSettings
>>>>>>> 94e5c733 (Rename counterpipeline):src/Microsoft.Diagnostics.Monitoring.WebApi/Metrics/CounterSettingsFactory.cs
            {
                CounterGroups = eventPipeCounterGroups.ToArray(),
                Duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds),
                CounterIntervalSeconds = counterInterval,
                MaxHistograms = maxHistograms,
                MaxTimeSeries = maxTimeSeries
            };
        }

        private static List<EventPipeCounterGroup> ConvertCounterGroups(IList<MetricProvider> providers)
        {
            List<EventPipeCounterGroup> counterGroups = new();

            if (providers?.Count > 0)
            {
                foreach (MetricProvider customProvider in providers)
                {
                    var customCounterGroup = new EventPipeCounterGroup { ProviderName = customProvider.ProviderName };
                    if (customProvider.CounterNames.Count > 0)
                    {
                        customCounterGroup.CounterNames = customProvider.CounterNames.ToArray();
                    }

                    customCounterGroup.Type = (CounterGroupType)customProvider.MetricType.GetValueOrDefault(MetricsOptionsDefaults.MetricType);

                    counterGroups.Add(customCounterGroup);
                }
            }

            return counterGroups;
        }

        private static List<EventPipeCounterGroup> ConvertCounterGroups(IList<Models.EventMetricsProvider> providers)
        {
            List<EventPipeCounterGroup> counterGroups = new();

            if (providers?.Count > 0)
            {
                foreach (Models.EventMetricsProvider customProvider in providers)
                {
                    var customCounterGroup = new EventPipeCounterGroup() { ProviderName = customProvider.ProviderName };
                    if (customProvider.CounterNames?.Length > 0)
                    {
                        customCounterGroup.CounterNames = customProvider.CounterNames.ToArray();
                    }

                    customCounterGroup.Type = (CounterGroupType)customProvider.MetricType.GetValueOrDefault(MetricsOptionsDefaults.MetricType);

                    counterGroups.Add(customCounterGroup);
                }
            }

            return counterGroups;
        }
    }
}
