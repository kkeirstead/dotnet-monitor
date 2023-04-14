// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal static class CollectCommandHandler
    {
        public static async Task<int> Invoke(CancellationToken token, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, FileInfo configurationFilePath)
        {
            try
            {
                StartupAuthenticationMode authMode = HostBuilderHelper.GetStartupAuthenticationMode(noAuth, tempApiKey);
                HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, authMode, configurationFilePath);

                IHost host = HostBuilderHelper.CreateHostBuilder(settings)
                    .Configure(authMode, noHttpEgress, settings)
                    .Build();

                var life = host.Services.GetRequiredService<IHostApplicationLifetime>();
                life.ApplicationStopped.Register(() =>
                {
                    Console.WriteLine("Application stopped.");
                }); 
                life.ApplicationStopping.Register(async () => {

                    Console.WriteLine("Application is shutting down begin");

                    var summaryOptions = host.Services.GetService<IOptions<SummaryOptions>>().Value;

                    var sessionSummary = host.Services.GetService<ISessionSummary>();
                    IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();

                    if (summaryOptions.Enabled)
                    {
                        //CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(100));

                        var action = new Func<Stream, CancellationToken, Task>(async (stream, ct) =>
                        {
                            /*
                            for (int index = 0; index > int.MinValue; --index)
                            {
                                if (index % 1000000 == 0)
                                {
                                    Console.WriteLine("1: " + index);
                                }
                            }*/

                            Console.WriteLine("Begin action.");
                            JsonWriterOptions options = new() { Indented = true };
                            var writer = new Utf8JsonWriter(stream, options);

                            
                            writer.WriteStartObject();

                            //await Task.Delay(400, ct);


                            //writer.WriteStartObject("Configuration");

                            // use existing config show mechanism
                            ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(writer);
                            try
                            {
                                jsonWriter.Write(configuration, full: false, skipNotPresent: false, showSources: false, configurationHeader: true); // could include these as params for SummaryOptions 
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex.Message);
                            }

                            //writer.Flush();

                            //writer = new Utf8JsonWriter(stream, options);

                            writer.WriteStartObject("Operations");

                            //writer.WriteEndObject();
                            foreach (OperationsSummary summary in sessionSummary.Operations)
                            {
                                writer.WriteStartObject(summary.ArtifactName);
                                writer.WriteStartObject("Process: ");
                                writer.WriteString("ProcessName", summary.ProcessInfo.ProcessName);
                                writer.WriteString("ProcessId", summary.ProcessInfo.EndpointInfo.ProcessId.ToString());
                                writer.WriteString("UUID", summary.ProcessInfo.EndpointInfo.RuntimeInstanceCookie.ToString());
                                writer.WriteString("Commandline", summary.ProcessInfo.EndpointInfo.CommandLine);
                                writer.WriteEndObject();
                                writer.WriteString("EgressProvider", summary.EgressProvider);
                                writer.WriteString("StartTime", summary.Time.ToString());
                                writer.WriteString("Duration", summary.Duration.ToString());
                                writer.WriteString("Success", summary.Success.ToString());
                                writer.WriteEndObject();
                            }

                            writer.WriteEndObject();

                            writer.WriteEndObject();

                            writer.Flush();

                            Console.WriteLine("End action.");

                            await Task.Delay(20000, ct); // stalling
                        });

                        var artifactName = "summary" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json";
                        // like what I'm doing in the validation, hook into the EgressAsync method and await it here
                        // add a token for cancellation if it's taking too long or if the user cancels the process?
                        var result = Task.Run(async () => await EgressOperation.EndOfSessionEgressAsync(host.Services, summaryOptions.Egress, action, artifactName, ContentTypes.ApplicationJson, CancellationToken.None));
                        

                        for (int index = 0; index < int.MaxValue; ++index)
                        {
                            if (index % 1000000 == 0)
                            {
                                Console.WriteLine("2: " + index);
                            }
                        }

                        result.Wait(10000);

                        await Task.Delay(1);

                        Console.WriteLine("Application is shutting down end");

                    }
                });

                //.ConfigureHostOptions to configure timeout

                try
                {
                    await host.StartAsync(token);

                    await host.WaitForShutdownAsync(token);

                    //var summaryOptions = host.Services.GetService<SummaryOptions>();

                    // Shut down -> 5 seconds (but can figure that)
                    //Ihostapplicationlifetime -> >ApplicationStopping< / ApplicationStopped -> register callback
                }
                catch (MonitoringException)
                {
                    // It is the responsibility of throwers to ensure that the exceptions are logged.
                    return -1;
                }
                catch (OptionsValidationException ex)
                {
                    host.Services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(CollectCommandHandler))
                        .OptionsValidationFailure(ex);
                    return -1;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Console.WriteLine("Token was cancelled.");
                    // The host will throw a OperationCanceledException if it cannot shut down the
                    // hosted services gracefully within the shut down timeout period. Handle the
                    // exception and let the tool exit gracefully.
                    return 0;
                }
                finally
                {
                    await DisposableHelper.DisposeAsync(host);
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is DeferredAuthenticationValidationException)
            {
                Console.Error.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine(ex.InnerException.Message);
                }

                return -1;
            }

            return 0;
        }

        private static IHostBuilder Configure(this IHostBuilder builder, StartupAuthenticationMode startupAuthMode, bool noHttpEgress, HostBuilderSettings settings)
        {
            return builder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                IAuthenticationConfigurator authConfigurator = AuthConfiguratorFactory.Create(startupAuthMode, context);
                services.AddSingleton<IAuthenticationConfigurator>(authConfigurator);

                //TODO Many of these service additions should be done through extension methods
                services.AddSingleton(RealSystemClock.Instance);

                services.AddSingleton<IEgressOutputConfiguration>(new EgressOutputConfiguration(httpEgressEnabled: !noHttpEgress));

                authConfigurator.ConfigureApiAuth(services, context);

                services.AddSwaggerGen(options =>
                {
                    options.ConfigureMonitorSwaggerGen();
                    authConfigurator.ConfigureSwaggerGenAuth(options);
                });

                services.ConfigureDiagnosticPort(context.Configuration);

                services.AddSingleton<OperationTrackerService>();

                services.ConfigureGlobalCounter(context.Configuration);

                services.ConfigureCollectionRuleDefaults(context.Configuration);

                services.ConfigureTemplates(context.Configuration);

                services.ConfigureSummary(context.Configuration);

                services.AddSingleton<IEndpointInfoSource, FilteredEndpointInfoSource>();
                services.AddSingleton<ServerEndpointInfoSource>();
                services.AddHostedServiceForwarder<ServerEndpointInfoSource>();
                services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                services.AddSingleton<IDumpService, DumpService>();
                services.AddSingleton<IEndpointInfoSourceCallbacks, OperationTrackerServiceEndpointInfoSourceCallback>();
                services.AddSingleton<IRequestLimitTracker, RequestLimitTracker>();
                services.ConfigureOperationStore();
                services.ConfigureExtensions();
                services.ConfigureExtensionLocations(settings);
                services.ConfigureEgress();
                services.ConfigureMetrics(context.Configuration);
                services.ConfigureStorage(context.Configuration);
                services.ConfigureDefaultProcess(context.Configuration);
                services.AddSingleton<ProfilerChannel>();
                services.ConfigureCollectionRules();
                services.ConfigureLibrarySharing();
                services.ConfigureProfiler();
                services.ConfigureExceptions();
                services.ConfigureStartupLoggers(authConfigurator);
                services.AddSingleton<IExperimentalFlags, ExperimentalFlags>();
                services.ConfigureInProcessFeatures(context.Configuration);
                services.AddSingleton<IInProcessFeatures, InProcessFeatures>();
                services.AddSingleton<IDumpOperationFactory, DumpOperationFactory>();
                services.AddSingleton<ILogsOperationFactory, LogsOperationFactory>();
                services.AddSingleton<IMetricsOperationFactory, MetricsOperationFactory>();
                services.AddSingleton<ITraceOperationFactory, TraceOperationFactory>();
                services.AddSingleton<ISessionSummary, SessionSummary>();
            })
            .ConfigureContainer((HostBuilderContext context, IServiceCollection services) =>
            {
                ServerUrlsBlockingConfigurationManager manager =
                    context.Properties[typeof(ServerUrlsBlockingConfigurationManager)] as ServerUrlsBlockingConfigurationManager;
                Debug.Assert(null != manager, $"Expected {typeof(ServerUrlsBlockingConfigurationManager).FullName} to be a {typeof(HostBuilderContext).FullName} property.");
                if (null != manager)
                {
                    // Block reading of the Urls option so that Kestrel is unable to read it from the composed configuration.
                    manager.IsBlocking = true;
                }
            });
        }
    }
}
