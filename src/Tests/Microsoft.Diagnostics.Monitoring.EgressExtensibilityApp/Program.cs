﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();

            Command egressCmd = new Command("Egress");

            egressCmd.SetHandler(Egress);

            rootCommand.Add(egressCmd);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> Egress()
        {
            EgressArtifactResult result = new();
            try
            {
                string jsonConfig = Console.ReadLine();

                ExtensionEgressPayload configPayload = JsonSerializer.Deserialize<ExtensionEgressPayload>(jsonConfig);
                TestEgressProviderOptions options = BuildOptions(configPayload);

                await Task.Delay(1000);

                if (options.ShouldSucceed)
                {
                    result.Succeeded = true;
                    result.ArtifactPath = EgressExtensibilityTests.SampleArtifactPath;
                }
                else
                {
                    result.Succeeded = false;
                    result.FailureMessage = EgressExtensibilityTests.SampleFailureMessage;
                }
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.FailureMessage = ex.Message;
            }

            string jsonBlob = JsonSerializer.Serialize<EgressArtifactResult>(result);
            Console.Write(jsonBlob);

            // return non-zero exit code when failed
            return result.Succeeded ? 0 : 1;
        }

        private static TestEgressProviderOptions BuildOptions(ExtensionEgressPayload configPayload)
        {
            TestEgressProviderOptions options = new TestEgressProviderOptions()
            {
                ShouldSucceed = GetConfig(configPayload.Configuration, nameof(TestEgressProviderOptions.ShouldSucceed)),
            };

            return options;
        }

        private static bool GetConfig(IDictionary<string, string> configDict, string propKey)
        {
            if (configDict.ContainsKey(propKey))
            {
                return bool.Parse(configDict[propKey]);
            }

            // throw?

            return false;
        }
    }

}
