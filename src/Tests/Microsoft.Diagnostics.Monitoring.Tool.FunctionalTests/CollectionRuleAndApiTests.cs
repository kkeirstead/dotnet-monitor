﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Graphs;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class CollectionRuleAndApiTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleAndApiTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

#if NET5_0_OR_GREATER
        private const string DefaultRuleName = "FunctionalTestRule";

        /// <summary>
        /// Validates that a non-startup rule will complete when it has an action limit specified
        /// without a sliding window duration.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleAndApi_AspNetRequestCountTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedRequestCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), isWebApp: true);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task ruleStartedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

            await appRunner.ExecuteNoCommandsAsync(async () =>
            {
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger(options =>
                        {
                            options.ResponseCount = ExpectedRequestCount;
                            options.StatusCodes = new string[] { "200", "202" };
                        })
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                await toolRunner.WriteUserSettingsAsync(newOptions);
                await toolRunner.StartAsync();

                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                ApiClient apiClient = new(_outputHelper, httpClient);

                try
                {
                    string pathAndQuery = "http://localhost:82";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    string pathAndQuery2 = "http://localhost:82/Privacy";
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery2);
                    string pathAndQuery3 = "http://localhost:82";
                    HttpResponseMessage message3 = await apiClient.ApiCall(pathAndQuery3);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                await ruleStartedTask;
                Assert.True(ruleStartedTask.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                //Directory.Delete(ExpectedFilePath);

                ////////////////////////////

                int processId = await appRunner.ProcessIdTask;

                TimeSpan duration = TimeSpan.FromSeconds(5);
                using ResponseStreamHolder holder = await apiClient.CaptureTraceAsync(processId, duration, WebApi.Models.TraceProfile.Http);
                Assert.NotNull(holder);

                await TraceTestUtilities.ValidateTrace(holder.Stream);

                //await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                ////////////////////////////

                Task ruleStartedTask2 = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

                try
                {
                    string pathAndQuery = "http://localhost:82";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    string pathAndQuery2 = "http://localhost:82/Privacy";
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery2);
                    string pathAndQuery3 = "http://localhost:82";
                    HttpResponseMessage message3 = await apiClient.ApiCall(pathAndQuery3);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                await ruleStartedTask2;
                Assert.True(ruleStartedTask2.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                appRunner.KillProcess();
            });
        }

        private async Task RunAspNetRequestCountCheck(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedRequestCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), isWebApp: true);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task ruleStartedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

            await appRunner.ExecuteNoCommandsAsync(async () =>
            {
                // Validate that the first rule is observed and its actions are run.
                //await originalActionsCompletedTask;

                // Change collection rule configuration to only contain the second rule.
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestCountTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                        })
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                //ruleStartedTask = runner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

                await toolRunner.WriteUserSettingsAsync(newOptions);
                await toolRunner.StartAsync();

                try
                {
                    using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                    ApiClient apiClient = new(_outputHelper, httpClient);

                    string pathAndQuery = "http://localhost:82";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    string pathAndQuery2 = "http://localhost:82/Privacy";
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery2);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                // Validate that only the second rule is observed.
                await ruleStartedTask;
                Assert.True(ruleStartedTask.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                appRunner.KillProcess();
                //await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            //Assert.Equal(0, appRunner.ExitCode);
        }

        /// <summary>
        /// Validates that a collection rule with a command line filter can be matched to the
        /// target process.
        /// </summary>
        [ConditionalTheory(nameof(IsNotNet5OrGreaterOnUnix))]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_CommandLineFilterMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task startedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await startedTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCommandLineFilter(TestAppScenarios.AsyncWait.Name);

                    startedTask = runner.WaitForCollectionRuleStartedAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a command line filter can fail to match the
        /// target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_CommandLineFilterNoMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task filteredTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await filteredTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    // Note that the process name filter is specified as "SpinWait" whereas the
                    // actual command line of the target process will contain "AsyncWait".
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddProcessNameFilter(TestAppScenarios.SpinWait.Name);

                    filteredTask = runner.WaitForCollectionRuleUnmatchedFiltersAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a process name filter can be matched to the
        /// target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ProcessNameFilterMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task startedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await startedTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddProcessNameFilter(DotNetHost.HostExeNameWithoutExtension);

                    startedTask = runner.WaitForCollectionRuleStartedAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a process name filter can fail to match the
        /// target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ProcessNameFilterNoMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task filteredTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await filteredTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddProcessNameFilter("UmatchedName");

                    filteredTask = runner.WaitForCollectionRuleUnmatchedFiltersAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a change in the collection rule configuration is detected and applied correctly.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ConfigurationChangeTest(DiagnosticPortConnectionMode mode)
        {
            const string firstRuleName = "FirstRule";
            const string secondRuleName = "SecondRule";

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            // Create a rule with some settings
            RootOptions originalOptions = new();
            originalOptions.CreateCollectionRule(firstRuleName)
                .SetStartupTrigger();

            await toolRunner.WriteUserSettingsAsync(originalOptions);

            await toolRunner.StartAsync();

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task originalActionsCompletedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(firstRuleName);

            await appRunner.ExecuteAsync(async () =>
            {
                // Validate that the first rule is observed and its actions are run.
                await originalActionsCompletedTask;

                // Set up new observers for the first and second rule.
                originalActionsCompletedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(firstRuleName);
                Task newActionsCompletedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(secondRuleName);

                // Change collection rule configuration to only contain the second rule.
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(secondRuleName)
                    .SetStartupTrigger();

                await toolRunner.WriteUserSettingsAsync(newOptions);

                // Validate that only the second rule is observed.
                await newActionsCompletedTask;
                Assert.False(originalActionsCompletedTask.IsCompleted);

                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            Assert.Equal(0, appRunner.ExitCode);
        }

        /// <summary>
        /// Validates that when a process exits, the collection rules for the process are stopped.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_StoppedOnExitTest(DiagnosticPortConnectionMode mode)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            // Create a rule with some settings
            RootOptions originalOptions = new();
            originalOptions.CreateCollectionRule(DefaultRuleName)
                .SetEventCounterTrigger(options =>
                {
                    options.ProviderName = "System.Runtime";
                    options.CounterName = "cpu-usage";
                    options.GreaterThan = 1000; // Intentionally unobtainable
                    options.SlidingWindowDuration = TimeSpan.FromSeconds(1);
                });

            await toolRunner.WriteUserSettingsAsync(originalOptions);

            await toolRunner.StartAsync();

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task ruleStartedTask = toolRunner.WaitForCollectionRuleStartedAsync(DefaultRuleName);
            Task rulesStoppedTask = toolRunner.WaitForCollectionRulesStoppedAsync();

            await appRunner.ExecuteAsync(async () =>
            {
                await ruleStartedTask;

                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            Assert.Equal(0, appRunner.ExitCode);

            // All of the rules for the process should have stopped. Note that dotnet-monitor has
            // not yet exited at this point in time; this is verification that the rules have stopped
            // for the target process before dotnet-monitor shuts down.
            await rulesStoppedTask;
        }

        // The GetProcessInfo command is not providing command line arguments (only the process name)
        // for .NET 5+ process on non-Windows when suspended. See https://github.com/dotnet/dotnet-monitor/issues/885
        private static bool IsNotNet5OrGreaterOnUnix =>
            DotNetHost.RuntimeVersion.Major < 5 ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#endif
    }
}
