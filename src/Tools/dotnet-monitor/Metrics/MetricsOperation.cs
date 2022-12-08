// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class MetricsOperation : PipelineArtifactOperation<EventCounterPipeline>
    {
        private readonly EventPipeCounterPipelineSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public MetricsOperation(IEndpointInfo endpointInfo, EventPipeCounterPipelineSettings settings, ILogger logger, IServiceProvider serviceProvider)
            : base(logger, Utils.ArtifactType_Metrics, endpointInfo)
        {
            _settings = settings;
            _serviceProvider = serviceProvider;
        }

        protected override EventCounterPipeline CreatePipeline(Stream outputStream)
        {
            var client = new DiagnosticsClient(EndpointInfo.Endpoint);

            var service = _serviceProvider.GetService<MetricsService>();

            var pipeline = service._counterPipeline;

            pipeline.AddPipeline(client,
                _settings,
                loggers: new[] { new JsonCounterLogger(outputStream, Logger) });

            return pipeline;
        }

        protected override Task<Task> StartPipelineAsync(EventCounterPipeline pipeline, CancellationToken token)
        {
            return pipeline.StartAsync(token);
        }

        public override string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.metrics.json");
        }

        public override string ContentType => ContentTypes.ApplicationJsonSequence;
    }
}
