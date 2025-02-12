// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Monitoring.Options
{

    public class InfoConfigurator
    {
        private readonly IOptions<CallStacksOptions> _callStacksOptions;
        private readonly IOptions<ParameterCapturingOptions> _parameterCapturingOptions;
        private readonly IOptions<ExceptionsOptions> _exceptionsOptions;
        private readonly IOptionsMonitor<MetricsOptions> _metricsOptions;

        public InfoConfigurator(IServiceProvider serviceProvider)
        {
            _callStacksOptions = serviceProvider.GetRequiredService<IOptions<CallStacksOptions>>();
            _parameterCapturingOptions = serviceProvider.GetRequiredService<IOptions<ParameterCapturingOptions>>();
            _exceptionsOptions = serviceProvider.GetRequiredService<IOptions<ExceptionsOptions>>();
            _metricsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsOptions>>();
        }

        public FeatureAvailabilityInfo GetFeatureAvailability()
        {
            return new FeatureAvailabilityInfo
            {
                Exceptions = new ExceptionsInfo
                {
                    Enabled = _exceptionsOptions.Value.GetEnabled()
                },
                Parameters = new ParametersInfo
                {
                    Enabled = _parameterCapturingOptions.Value.GetEnabled()
                },
                CallStacks = new CallStacksInfo
                {
                    Enabled = _callStacksOptions.Value.GetEnabled()
                },
                Metrics = new MetricsInfo
                {
                    Enabled = _metricsOptions.CurrentValue.GetEnabled()
                }
            };
        }
    }
}
