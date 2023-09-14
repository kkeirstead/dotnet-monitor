// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Pipeline
{
    internal sealed class ParameterCapturingPipeline : IDisposable
    {
        private sealed class CapturingRequest
        {
            public CapturingRequest(StartCapturingParametersPayload payload)
            {
                Payload = payload ?? throw new ArgumentNullException(nameof(payload));
                StopRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public StartCapturingParametersPayload Payload { get; }
            public TaskCompletionSource StopRequest { get; }
        }

        private readonly IFunctionProbesManager _probeManager;
        private readonly IMethodDescriptionValidator _methodDescriptionValidator;
        private readonly IParameterCapturingPipelineCallbacks _callbacks;
        private readonly Channel<CapturingRequest> _requestQueue;
        private readonly ConcurrentDictionary<Guid, CapturingRequest> _allRequests = new();

        public ParameterCapturingPipeline(IFunctionProbesManager probeManager, IParameterCapturingPipelineCallbacks callbacks, IMethodDescriptionValidator methodDescriptionValidator)
        {
            _probeManager = probeManager;
            _methodDescriptionValidator = methodDescriptionValidator;
            _callbacks = callbacks;

            _requestQueue = Channel.CreateBounded<CapturingRequest>(new BoundedChannelOptions(capacity: 1)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CapturingRequest request = await _requestQueue.Reader.ReadAsync(stoppingToken);

                void onFault(object? sender, InstrumentedMethod faultingMethod)
                {
                    _callbacks.ProbeFault(request.Payload.RequestId, faultingMethod);
                }

                try
                {
                    _probeManager.OnProbeFault += onFault;

                    if (!await TryStartCapturingAsync(request.Payload, stoppingToken).ConfigureAwait(false))
                    {
                        continue;
                    }

                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(request.Payload.Duration);

                    try
                    {
                        // Spin another task here every (second?) that checks the stub for events

                        CancellationTokenSource source = new();

                        CancellationToken token = source.Token;
                        Task t = new Task(() =>
                        {
                            int hitsCounter = 0;
                            while (!token.IsCancellationRequested) // bad
                            {
                                if (FunctionProbesStub.Instance != null && FunctionProbesStub.Instance.Hits.Count > hitsCounter)
                                {
                                    _callbacks.TestingOnly(request.Payload.RequestId, FunctionProbesStub.Instance.Hits);
                                    hitsCounter = FunctionProbesStub.Instance.Hits.Count;
                                }
                                Task.Delay(1000);
                            }
                        }, token);

                        t.Start();
                        
                        

                        await request.StopRequest.Task.WaitAsync(cts.Token).ConfigureAwait(false);


                        source.Cancel(); // stop the loop
                        
                    }
                    catch (OperationCanceledException)
                    {

                    }

                    //
                    // NOTE:
                    // StopCapturingAsync will request a stop regardless of if the stoppingToken is set.
                    // While we don't support the host & services reloading, the above behavior will ensure
                    // that we don't leave the app in a potentially bad state on a reload.
                    //
                    // See: https://github.com/dotnet/dotnet-monitor/issues/5170
                    //
                    await _probeManager.StopCapturingAsync(stoppingToken).ConfigureAwait(false);

                    _callbacks.CapturingStop(request.Payload.RequestId);
                    _ = _allRequests.TryRemove(request.Payload.RequestId, out _);
                }
                finally
                {
                    _probeManager.OnProbeFault -= onFault;
                }
            }
        }

        // Private method for work that happens inside the pipeline's RunAsync
        // so use callbacks instead of throwing exceptions.
        private async Task<bool> TryStartCapturingAsync(StartCapturingParametersPayload request, CancellationToken token)
        {
            try
            {
                MethodResolver methodResolver = new();

                List<MethodInfo> methods = new(request.Configuration.Methods.Length);

                List<FieldInfo> fields = new(100); //request.Configuration.Fields.Length

                List<FieldDescription> TEST_fieldDescriptions = new()
                {
                    new FieldDescription()
                    {
                        TypeName = "ParameterCapturingTesting.Controllers.HomeController",
                        FieldName = "myGlobalNum",
                        ModuleName = "ParameterCapturingTesting.dll",
                    }

                };

                List<FieldDescription> fieldsFailedToResolve = new();


                for (int i = 0; i < TEST_fieldDescriptions.Count; ++i)
                {
                    FieldDescription fieldDescription = TEST_fieldDescriptions[i];
                    List<FieldInfo> resolvedFields = methodResolver.ResolveFieldDescription(fieldDescription);

                    if (resolvedFields.Count == 0)
                    {
                        fieldsFailedToResolve.Add(fieldDescription);
                    }

                    fields.AddRange(resolvedFields);
                }



                List<MethodDescription> methodsFailedToResolve = new();

                for (int i = 0; i < request.Configuration.Methods.Length; i++)
                {
                    MethodDescription methodDescription = request.Configuration.Methods[i];

                    List<MethodInfo> resolvedMethods = methodResolver.ResolveMethodDescription(methodDescription);
                    if (resolvedMethods.Count == 0)
                    {
                        methodsFailedToResolve.Add(methodDescription);
                    }

                    methods.AddRange(resolvedMethods);
                }

                if (methodsFailedToResolve.Count > 0)
                {
                    UnresolvedMethodsExceptions ex = new(methodsFailedToResolve);
                    throw ex;
                }

                if (fieldsFailedToResolve.Count > 0)
                {
                    throw new Exception("Put something real here.");
                }

                await _probeManager.StartCapturingAsync2(methods, fields, token).ConfigureAwait(false);
                //await _probeManager.StartCapturingAsync(methods, token).ConfigureAwait(false);
                _callbacks.CapturingStart(request, methods);

                return true;
            }
            catch (UnresolvedMethodsExceptions ex)
            {
                _callbacks.FailedToCapture(
                    request.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods,
                    ex.Message);
            }
            catch (Exception ex)
            {
                _callbacks.FailedToCapture(
                    request.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.InternalError,
                    ex.ToString());
            }

            return false;
        }

        public bool TryComplete()
        {
            return _requestQueue.Writer.TryComplete();
        }

        public void SubmitRequest(StartCapturingParametersPayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload.Configuration);

            if (payload.Configuration.Methods.Length == 0)
            {
                throw new ArgumentException(nameof(payload.Configuration.Methods));
            }

            List<MethodDescription> _deniedMethodDescriptions = new();
            foreach (MethodDescription methodDescription in payload.Configuration.Methods)
            {
                if (!_methodDescriptionValidator.IsMethodDescriptionAllowed(methodDescription))
                {
                    _deniedMethodDescriptions.Add(methodDescription);
                }
            }

            if (_deniedMethodDescriptions.Count > 0)
            {
                throw new DeniedMethodsException(_deniedMethodDescriptions);
            }

            CapturingRequest request = new(payload);
            if (!_allRequests.TryAdd(payload.RequestId, request))
            {
                throw new ArgumentException(nameof(payload.RequestId));
            }

            if (!_requestQueue.Writer.TryWrite(request))
            {
                _ = request.StopRequest.TrySetCanceled();
                _ = _allRequests.TryRemove(payload.RequestId, out _);

                throw new TooManyRequestsException(ParameterCapturingStrings.TooManyRequestsErrorMessage);
            }
        }

        public void RequestStop(Guid requestId)
        {
            if (!_allRequests.TryGetValue(requestId, out CapturingRequest? request))
            {
                throw new ArgumentException(nameof(requestId));
            }

            _ = request.StopRequest?.TrySetResult();
        }

        public void Dispose()
        {
            _probeManager.Dispose();
        }
    }
}
