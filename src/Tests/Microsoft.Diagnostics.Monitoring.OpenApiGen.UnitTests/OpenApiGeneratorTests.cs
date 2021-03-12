// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Validations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen.UnitTests
{
    public class OpenApiGeneratorTests
    {
        private const int GenerationTimemoutMs = 30_000;

        private const string OpenApiBaselineName = "openapi.json";
        private const string OpenApiGenName = "Microsoft.Diagnostics.Monitoring.OpenApiGen";

        private static readonly string CurrentExecutingAssemblyPath =
            Assembly.GetExecutingAssembly().Location;

        private static readonly string OpenApiBaselinePath =
            Path.Combine(Path.GetDirectoryName(CurrentExecutingAssemblyPath), OpenApiBaselineName);

        private static readonly string OpenApiGenPath =
            CurrentExecutingAssemblyPath.Replace(Assembly.GetExecutingAssembly().GetName().Name, OpenApiGenName);

        private readonly ITestOutputHelper _outputHelper;

        public OpenApiGeneratorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Test that the committed OpenAPI document for dotnet-monitor
        /// is the same that is generated from the dotnet-monitor binaries.
        /// </summary>
        [Fact]
        public async Task BaselineDifferenceTest()
        {
            using FileStream stream = await GenerateDocumentAsync();
            using StreamReader reader = new StreamReader(stream);

            // Renormalize line endings due to git checkout normalizing to the operating system preference.
            string baselineContent = File.ReadAllText(OpenApiBaselinePath).Replace("\r\n", "\n");
            string generatedContent = reader.ReadToEnd();

            Assert.True(
                string.Equals(baselineContent, generatedContent, StringComparison.Ordinal),
                "The generated OpenAPI description is different than the documented baseline.");
        }

        /// <summary>
        /// Test that the committed OpenAPI document is valid.
        /// </summary>
        [Fact]
        public void BaselineIsValidTest()
        {
            using FileStream stream = new FileStream(OpenApiBaselinePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            ValidateDocument(stream);
        }

        /// <summary>
        /// Test that the generated OpenAPI document is valid.
        /// </summary>
        [Fact]
        public async Task GeneratedIsValidTest()
        {
            using FileStream stream = await GenerateDocumentAsync();

            ValidateDocument(stream);
        }

        private async Task<FileStream> GenerateDocumentAsync()
        {
            string path = Path.GetTempFileName();

            _outputHelper.WriteLine($"OpenAPI Document: {path}");

            DotNetRunner runner = new DotNetRunner();
            runner.EntryAssemblyPath = OpenApiGenPath;
            runner.Arguments = path;

            using CancellationTokenSource cancellation = new CancellationTokenSource(GenerationTimemoutMs);

            await runner.StartAsync(cancellation.Token);

            int exitCode = await runner.WaitForExitAsync(cancellation.Token);

            string line;
            _outputHelper.WriteLine("Standard Output:");
            while (null != (line = runner.StandardOutput.ReadLine()))
            {
                _outputHelper.WriteLine(line);
            }

            _outputHelper.WriteLine("Standard Error:");
            while (null != (line = runner.StandardError.ReadLine()))
            {
                _outputHelper.WriteLine(line);
            }

            Assert.Equal(0, exitCode);

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
        }

        private static void ValidateDocument(FileStream stream)
        {
            OpenApiStreamReader reader = new OpenApiStreamReader();
            OpenApiDocument document = reader.Read(stream, out OpenApiDiagnostic diagnostic);
            Assert.Empty(diagnostic.Errors);

            IEnumerable<OpenApiError> errors = document.Validate(ValidationRuleSet.GetDefaultRuleSet());
            Assert.Empty(errors);
        }
    }
}
