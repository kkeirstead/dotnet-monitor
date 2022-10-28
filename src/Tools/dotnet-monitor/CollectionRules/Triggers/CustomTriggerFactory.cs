// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.Custom;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new EventCounterTrigger trigger.
    /// </summary>
    internal sealed class CustomTriggerFactory :
        ICollectionRuleTriggerFactory<CustomOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<CustomTriggerSettings> _traceEventTriggerFactory;
        private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;

        public CustomTriggerFactory(
            IOptionsMonitor<GlobalCounterOptions> counterOptions,
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<CustomTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
            _counterOptions = counterOptions;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, CustomOptions options)
        {
            CustomTriggerSettings settings = new()
            {
                ExtensionName = options.ExtensionName,
                Args = options.Args,
                Providers = options.Providers,
                CounterIntervalSeconds = _counterOptions.CurrentValue.GetIntervalSeconds(),
            };

            return EventPipeTriggerFactory.Create(
                endpointInfo,
                CustomTrigger.CreateConfiguration(settings),
                _traceEventTriggerFactory,
                settings,
                callback);
        }
    }
}
