﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.AspNet;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.EventCounter;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureGlobalCounter(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<GlobalCounterOptions>(services, configuration, ConfigurationKeys.GlobalCounter);
        }

        public static IServiceCollection ConfigureCollectionRuleDefaults(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<CollectionRuleDefaultOptions>(services, configuration, ConfigurationKeys.CollectionRuleDefaults);
        }

        public static IServiceCollection ConfigureMetrics(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<MetricsOptions>(services, configuration, ConfigurationKeys.Metrics);
        }

        public static IServiceCollection ConfigureMonitorApiKeyOptions(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureOptions<MonitorApiKeyOptions>(services, configuration, ConfigurationKeys.MonitorApiKey);

            // Loads and validates MonitorApiKeyOptions into MonitorApiKeyConfiguration
            services.AddSingleton<IPostConfigureOptions<MonitorApiKeyConfiguration>, MonitorApiKeyPostConfigure>();
            // Notifies that MonitorApiKeyConfiguration is changed when MonitorApiKeyOptions is changed.
            services.AddSingleton<IOptionsChangeTokenSource<MonitorApiKeyConfiguration>, MonitorApiKeyChangeTokenSource>();
            
            return services;
        }

        public static AuthenticationBuilder ConfigureMonitorApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            IConfigurationSection authSection = configuration.GetSection(ConfigurationKeys.Authentication);
            services.ConfigureMonitorApiKeyOptions(authSection);

            // Notifies that the JwtBearerOptions change when MonitorApiKeyConfiguration gets changed.
            services.AddSingleton<IOptionsChangeTokenSource<JwtBearerOptions>, JwtBearerChangeTokenSource>();
            // Adds the JwtBearerOptions configuration source, which will provide the updated JwtBearerOptions when MonitorApiKeyConfiguration updates
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigure>());

            // AddJwtBearer will consume the JwtBearerOptions generated by ConfigureMonitorApiKeyConfiguration
            AuthenticationBuilder builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
            // We need to provide a slightly modified JwtBearerHandler to do auth, the basic JwtBearerHandler does not accept option changes after initialization
            builder.AddScheme<JwtBearerOptions, LiveJwtBearerHandler>(JwtBearerDefaults.AuthenticationScheme, null, _ => { });

            return builder;
        }

        public static IServiceCollection ConfigureCollectionRules(this IServiceCollection services)
        {
            services.RegisterCollectionRuleAction<CollectDumpActionFactory, CollectDumpOptions>(KnownCollectionRuleActions.CollectDump);
            services.RegisterCollectionRuleAction<CollectGCDumpActionFactory, CollectGCDumpOptions>(KnownCollectionRuleActions.CollectGCDump);
            services.RegisterCollectionRuleAction<CollectLogsActionFactory, CollectLogsOptions>(KnownCollectionRuleActions.CollectLogs);
            services.RegisterCollectionRuleAction<CollectTraceActionFactory, CollectTraceOptions>(KnownCollectionRuleActions.CollectTrace);
            services.RegisterCollectionRuleAction<ExecuteActionFactory, ExecuteOptions>(KnownCollectionRuleActions.Execute);
            services.RegisterCollectionRuleAction<LoadProfilerActionFactory, LoadProfilerOptions>(KnownCollectionRuleActions.LoadProfiler);
            services.RegisterCollectionRuleAction<SetEnvironmentVariableActionFactory, SetEnvironmentVariableOptions>(KnownCollectionRuleActions.SetEnvironmentVariable);
            services.RegisterCollectionRuleAction<GetEnvironmentVariableActionFactory, GetEnvironmentVariableOptions>(KnownCollectionRuleActions.GetEnvironmentVariable);

            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.AspNetRequestCountTriggerFactory, AspNetRequestCountOptions>(KnownCollectionRuleTriggers.AspNetRequestCount);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.AspNetRequestDurationTriggerFactory, AspNetRequestDurationOptions>(KnownCollectionRuleTriggers.AspNetRequestDuration);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.AspNetResponseStatusTriggerFactory, AspNetResponseStatusOptions>(KnownCollectionRuleTriggers.AspNetResponseStatus);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.EventCounterTriggerFactory, EventCounterOptions>(KnownCollectionRuleTriggers.EventCounter);
            services.RegisterCollectionRuleTrigger<StartupTriggerFactory>(KnownCollectionRuleTriggers.Startup);

            services.ConfigureCollectionRuleDefaults();

            services.AddSingleton<EventPipeTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<EventCounterTriggerSettings>, Monitoring.EventPipe.Triggers.EventCounter.EventCounterTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<AspNetRequestDurationTriggerSettings>, Monitoring.EventPipe.Triggers.AspNet.AspNetRequestDurationTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<AspNetRequestCountTriggerSettings>, Monitoring.EventPipe.Triggers.AspNet.AspNetRequestCountTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<AspNetRequestStatusTriggerSettings>, Monitoring.EventPipe.Triggers.AspNet.AspNetRequestStatusTriggerFactory>();

            services.AddSingleton<CollectionRulesConfigurationProvider>();
            services.AddSingleton<ICollectionRuleActionOperations, CollectionRuleActionOperations>();
            services.AddSingleton<ICollectionRuleTriggerOperations, CollectionRuleTriggerOperations>();

            services.AddSingleton<IConfigureOptions<CollectionRuleOptions>, CollectionRuleConfigureNamedOptions>();
            services.AddSingleton<IValidateOptions<CollectionRuleOptions>, DataAnnotationValidateOptions<CollectionRuleOptions>>();

            // Register change sources for the options type
            services.AddSingleton<IOptionsChangeTokenSource<CollectionRuleOptions>, CollectionRulesConfigurationChangeTokenSource>();

            // Add custom options cache to override behavior of default named options
            services.AddSingleton<IOptionsMonitorCache<CollectionRuleOptions>, DynamicNamedOptionsCache<CollectionRuleOptions>>();

            services.AddSingleton<ActionListExecutor>();
            services.AddSingleton<CollectionRuleService>();
            services.AddHostedServiceForwarder<CollectionRuleService>();
            services.AddSingleton<IEndpointInfoSourceCallbacks, CollectionRuleEndpointInfoSourceCallbacks>();

            return services;
        }

        public static IServiceCollection ConfigureCollectionRuleDefaults(this IServiceCollection services)
        {
            services.AddSingleton<IValidateOptions<CollectDumpOptions>, EgressValidateOptions<CollectDumpOptions>>();
            services.AddSingleton<IValidateOptions<CollectGCDumpOptions>, EgressValidateOptions<CollectGCDumpOptions>>();
            services.AddSingleton<IValidateOptions<CollectLogsOptions>, EgressValidateOptions<CollectLogsOptions>>();
            services.AddSingleton<IValidateOptions<CollectTraceOptions>, EgressValidateOptions<CollectTraceOptions>>();

            services.AddSingleton<IValidateOptions<AspNetRequestCountOptions>, SlidingWindowDurationValidateOptions<AspNetRequestCountOptions>>();
            services.AddSingleton<IValidateOptions<AspNetRequestDurationOptions>, SlidingWindowDurationValidateOptions<AspNetRequestDurationOptions>>();
            services.AddSingleton<IValidateOptions<AspNetResponseStatusOptions>, SlidingWindowDurationValidateOptions<AspNetResponseStatusOptions>>();
            services.AddSingleton<IValidateOptions<EventCounterOptions>, SlidingWindowDurationValidateOptions<EventCounterOptions>>();

            return services;
        }

        public static IServiceCollection RegisterCollectionRuleAction<TFactory, TOptions>(this IServiceCollection services, string actionName)
            where TFactory : class, ICollectionRuleActionFactory<TOptions>
            where TOptions : class, new()
        {
            services.AddSingleton<TFactory>();
            services.AddSingleton<CollectionRuleActionFactoryProxy<TFactory, TOptions>>();
            services.AddSingleton<ICollectionRuleActionDescriptor, CollectionRuleActionDescriptor<TFactory, TOptions>>(sp => new CollectionRuleActionDescriptor<TFactory, TOptions>(actionName));
            // NOTE: When opening collection rule actions for extensibility, this should not be added for all registered actions.
            // Each action should register its own IValidateOptions<> implementation (if it needs one).
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();
            return services;
        }

        public static IServiceCollection RegisterCollectionRuleTrigger<TFactory>(this IServiceCollection services, string triggerName)
            where TFactory : class, ICollectionRuleTriggerFactory
        {
            services.AddSingleton<TFactory>();
            services.AddSingleton<CollectionRuleTriggerFactoryProxy<TFactory>>();
            services.AddSingleton<ICollectionRuleTriggerDescriptor, CollectionRuleTriggerDescriptor<TFactory>>(
                sp => new CollectionRuleTriggerDescriptor<TFactory>(triggerName));
            return services;
        }

        public static IServiceCollection RegisterCollectionRuleTrigger<TFactory, TOptions>(this IServiceCollection services, string triggerName)
            where TFactory : class, ICollectionRuleTriggerFactory<TOptions>
            where TOptions : class, new()
        {
            services.AddSingleton<TFactory>();
            services.AddSingleton<CollectionRuleTriggerFactoryProxy<TFactory, TOptions>>();
            services.AddSingleton<ICollectionRuleTriggerDescriptor, CollectionRuleTriggerProvider<TFactory, TOptions>>(
                sp => new CollectionRuleTriggerProvider<TFactory, TOptions>(triggerName));
            // NOTE: When opening colletion rule triggers for extensibility, this should not be added for all registered triggers.
            // Each trigger should register its own IValidateOptions<> implementation (if it needs one).
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();
            return services;
        }

        public static IServiceCollection ConfigureStorage(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<StorageOptions>(services, configuration, ConfigurationKeys.Storage);
        }

        public static IServiceCollection ConfigureDefaultProcess(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<ProcessFilterOptions>(services, configuration, ConfigurationKeys.DefaultProcess);
        }

        private static IServiceCollection ConfigureOptions<T>(IServiceCollection services, IConfiguration configuration, string key) where T : class
        {
            return services.Configure<T>(configuration.GetSection(key));
        }

        public static IServiceCollection ConfigureEgress(this IServiceCollection services)
        {
            // Register IEgressService implementation that provides egressing
            // of artifacts for the REST server.
            services.AddSingleton<IEgressService, EgressService>();

            services.AddSingleton<IEgressPropertiesConfigurationProvider, EgressPropertiesConfigurationProvider>();
            services.AddSingleton<IEgressPropertiesProvider, EgressPropertiesProvider>();

            // Register regress providers
            services.RegisterProvider<AzureBlobEgressProviderOptions, AzureBlobEgressProvider>(EgressProviderTypes.AzureBlobStorage);
            services.RegisterProvider<FileSystemEgressProviderOptions, FileSystemEgressProvider>(EgressProviderTypes.FileSystem);

            // Extra registrations for provider specific behavior
            services.AddSingleton<IPostConfigureOptions<AzureBlobEgressProviderOptions>, AzureBlobEgressPostConfigureOptions>();

            return services;
        }

        public static IServiceCollection ConfigureDiagnosticPort(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DiagnosticPortOptions>(configuration.GetSection(ConfigurationKeys.DiagnosticPort));
            services.AddSingleton<IPostConfigureOptions<DiagnosticPortOptions>, DiagnosticPortPostConfigureOptions>();
            services.AddSingleton<IValidateOptions<DiagnosticPortOptions>, DiagnosticPortValidateOptions>();

            return services;
        }

        private static IServiceCollection RegisterProvider<TOptions, TProvider>(this IServiceCollection services, string name)
            where TProvider : class, IEgressProvider<TOptions>
            where TOptions : class
        {
            // Add services to provide raw configuration for the options type
            services.AddSingleton(sp => new EgressProviderConfigurationProvider<TOptions>(sp.GetRequiredService<IConfiguration>(), name));
            services.AddSingletonForwarder<IEgressProviderConfigurationProvider<TOptions>, EgressProviderConfigurationProvider<TOptions>>();
            services.AddSingletonForwarder<IEgressProviderConfigurationProvider, EgressProviderConfigurationProvider<TOptions>>();

            // Add options services for configuring the options type
            services.AddSingleton<IConfigureOptions<TOptions>, EgressProviderConfigureNamedOptions<TOptions>>();
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();

            // Register change sources for the options type
            services.AddSingleton<IOptionsChangeTokenSource<TOptions>, EgressPropertiesConfigurationChangeTokenSource<TOptions>>();
            services.AddSingleton<IOptionsChangeTokenSource<TOptions>, EgressProviderConfigurationChangeTokenSource<TOptions>>();

            // Add custom options cache to override behavior of default named options
            services.AddSingleton<IOptionsMonitorCache<TOptions>, DynamicNamedOptionsCache<TOptions>>();

            // Add egress provider and internal provider wrapper
            services.AddSingleton<IEgressProvider<TOptions>, TProvider>();
            services.AddSingleton<IEgressProviderInternal<TOptions>, EgressProviderInternal<TOptions>>();

            return services;
        }

        private static void AddSingletonForwarder<TService, TImplementation>(this IServiceCollection services) where TImplementation : class, TService where TService : class
        {
            services.AddSingleton<TService, TImplementation>(sp => sp.GetRequiredService<TImplementation>());
        }

        public static void AddHostedServiceForwarder<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService
        {
            services.AddHostedService<THostedService>(sp => sp.GetRequiredService<THostedService>());
        }
    }
}
