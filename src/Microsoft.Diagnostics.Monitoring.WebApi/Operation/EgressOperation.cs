﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class EgressOperation : IEgressOperation
    {
        private readonly Func<IEgressService, CancellationToken, Task<EgressResult>> _egress;
        private readonly KeyValueLogScope _scope;
        public EgressProcessInfo ProcessInfo { get; private set; }
        public string EgressProviderName { get; private set; }
        public bool IsStoppable { get { return _operation?.IsStoppable ?? false; } }
        public ISet<string> Tags { get; private set; }

        private readonly IArtifactOperation _operation;


        public EgressOperation(Func<CancellationToken, Task<Stream>> action, string endpointName, string artifactName, IProcessInfo processInfo, string contentType, KeyValueLogScope scope, string tags, CollectionRuleMetadata collectionRuleMetadata = null)
        {
            _egress = (service, token) => service.EgressAsync(endpointName, action, artifactName, contentType, processInfo.EndpointInfo, collectionRuleMetadata, token);
            _scope = scope;

            EgressProviderName = endpointName;
            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
            Tags = Utilities.SplitTags(tags);
        }

        public EgressOperation(Func<Stream, CancellationToken, Task> action, string endpointName, string artifactName, IProcessInfo processInfo, string contentType, KeyValueLogScope scope, string tags, CollectionRuleMetadata collectionRuleMetadata = null)
        {
            _egress = (service, token) => service.EgressAsync(endpointName, action, artifactName, contentType, processInfo.EndpointInfo, collectionRuleMetadata, token);
            EgressProviderName = endpointName;
            _scope = scope;

            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
            Tags = Utilities.SplitTags(tags);
        }

        public EgressOperation(IArtifactOperation operation, string endpointName, IProcessInfo processInfo, KeyValueLogScope scope, string tag, CollectionRuleMetadata collectionRuleMetadata = null)
            : this(operation.ExecuteAsync, endpointName, operation.GenerateFileName(), processInfo, operation.ContentType, scope, tag, collectionRuleMetadata)
        {
            _operation = operation;
        }

        // The below constructors don't need EgressProcessInfo as their callers don't store to the operations table.
        public EgressOperation(Func<Stream, CancellationToken, Task> action, string endpointName, string artifactName, IEndpointInfo source, string contentType, KeyValueLogScope scope, CollectionRuleMetadata collectionRuleMetadata)
        {
            _egress = (service, token) => service.EgressAsync(endpointName, action, artifactName, contentType, source, collectionRuleMetadata, token);
            EgressProviderName = endpointName;
            _scope = scope;
        }

        public EgressOperation(Func<CancellationToken, Task<Stream>> action, string endpointName, string artifactName, IEndpointInfo source, string contentType, KeyValueLogScope scope, CollectionRuleMetadata collectionRuleMetadata)
        {
            _egress = (service, token) => service.EgressAsync(endpointName, action, artifactName, contentType, source, collectionRuleMetadata, token);
            EgressProviderName = endpointName;
            _scope = scope;
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            ILogger<EgressOperation> logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<EgressOperation>();

            using var _ = logger.BeginScope(_scope);

            return await ExecutionHelper.InvokeAsync(async (token) =>
            {
                IEgressService egressService = serviceProvider
                    .GetRequiredService<IEgressService>();

                EgressResult egressResult = await _egress(egressService, token);

                logger.EgressedArtifact(egressResult.Value);

                // The remaining code is creating a JSON object with a single property and scalar value
                // that indiates where the stream data was egressed. Because the name of the artifact is
                // automatically generated by the REST API and the caller of the endpoint might not know
                // the specific configuration information for the egress provider, this value allows the
                // caller to more easily find the artifact after egress has completed.
                return ExecutionResult<EgressResult>.Succeeded(egressResult);
            }, logger, token);
        }

        public void Validate(IServiceProvider serviceProvider)
        {
            serviceProvider
                .GetRequiredService<IEgressService>()
                .ValidateProvider(EgressProviderName);
        }

        public Task StopAsync(CancellationToken token)
        {
            if (_operation == null)
            {
                throw new InvalidOperationException();
            }

            return _operation.StopAsync(token);
        }
    }
}
