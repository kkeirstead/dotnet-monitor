﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    internal sealed class DefaultCollectionRulePostConfigureOptions :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;
        private static readonly TimeSpan SlidingWindowDurationDefault = TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default);

        public DefaultCollectionRulePostConfigureOptions(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions
            )
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            ConfigureEgress(options);
            ConfigureLimits(options);
            ConfigureRequestCounts(options);
            ConfigureResponseCounts(options);
            ConfigureSlidingWindowDurations(options);
        }

        public void ConfigureEgress(CollectionRuleOptions options)
        {
            var actionDefaults = _defaultOptions.CurrentValue.Actions;

            if (actionDefaults == null)
            {
                return;
            }

            foreach (var action in options.Actions)
            {
                if (action.Settings is IEgressProviderProperties egressProviderProperties)
                {
                    if (string.IsNullOrEmpty(egressProviderProperties.Egress))
                    {
                        egressProviderProperties.Egress = actionDefaults.Egress;
                    }
                }
            }
        }

        public void ConfigureLimits(CollectionRuleOptions options)
        {
            var limitsDefaults = _defaultOptions.CurrentValue.Limits;

            if (limitsDefaults == null)
            {
                return;
            }

            if (null == options.Limits)
            {
                if (!limitsDefaults.ActionCount.HasValue
                    && !limitsDefaults.ActionCountSlidingWindowDuration.HasValue
                    && !limitsDefaults.RuleDuration.HasValue)
                {
                    return;
                }

                options.Limits = new CollectionRuleLimitsOptions();
            }

            if (null == options.Limits.ActionCount)
            {
                options.Limits.ActionCount = limitsDefaults.ActionCount ?? CollectionRuleLimitsOptionsDefaults.ActionCount;
            }

            if (null == options.Limits.ActionCountSlidingWindowDuration)
            {
                options.Limits.ActionCountSlidingWindowDuration = limitsDefaults.ActionCountSlidingWindowDuration;
            }

            if (null == options.Limits.RuleDuration)
            {
                options.Limits.RuleDuration = limitsDefaults.RuleDuration;
            }
        }

        public void ConfigureRequestCounts(CollectionRuleOptions options)
        {
            var triggerDefaults = _defaultOptions.CurrentValue.Triggers;

            if (triggerDefaults == null)
            {
                return;
            }

            if (null != options.Trigger)
            {
                if (options.Trigger.Settings is IRequestCountProperties requestCountProperties)
                {
                    if (0 == requestCountProperties.RequestCount && triggerDefaults.RequestCount.HasValue)
                    {
                        requestCountProperties.RequestCount = triggerDefaults.RequestCount.Value;
                    }
                }
            }
        }

        public void ConfigureResponseCounts(CollectionRuleOptions options)
        {
            var triggerDefaults = _defaultOptions.CurrentValue.Triggers;

            if (triggerDefaults == null)
            {
                return;
            }

            if (null != options.Trigger)
            {
                if (options.Trigger.Settings is AspNetResponseStatusOptions responseStatusProperties)
                {
                    if (0 == responseStatusProperties.ResponseCount && triggerDefaults.ResponseCount.HasValue)
                    {
                        responseStatusProperties.ResponseCount = triggerDefaults.ResponseCount.Value;
                    }
                }
            }
        }

        public void ConfigureSlidingWindowDurations(CollectionRuleOptions options)
        {
            var triggerDefaults = _defaultOptions.CurrentValue.Triggers;

            if (triggerDefaults == null)
            {
                return;
            }

            if (null != options.Trigger)
            {
                if (options.Trigger.Settings is ISlidingWindowDurationProperties slidingWindowDurationProperties)
                {
                    if (null == slidingWindowDurationProperties.SlidingWindowDuration)
                    {
                        slidingWindowDurationProperties.SlidingWindowDuration = triggerDefaults.SlidingWindowDuration ?? SlidingWindowDurationDefault;
                    }
                }
            }
        }
    }
}
