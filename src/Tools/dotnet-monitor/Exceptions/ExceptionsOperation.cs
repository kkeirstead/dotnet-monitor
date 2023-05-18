﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperation : IArtifactOperation
    {
        private static byte[] JsonRecordDelimiter = new byte[] { (byte)'\n' };

        private static byte[] JsonSequenceRecordSeparator = new byte[] { 0x1E };

        private readonly ExceptionsFormat _format;
        private readonly IExceptionsStore _store;

        public ExceptionsOperation(IExceptionsStore store, ExceptionsFormat format)
        {
            _store = store;
            _format = format;
        }

        public string ContentType => _format switch
        {
            ExceptionsFormat.PlainText => ContentTypes.TextPlain,
            ExceptionsFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
            ExceptionsFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
            _ => ContentTypes.TextPlain
        };

        public bool IsStoppable => false;

        public async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            startCompletionSource?.TrySetResult(null);


            IEnumerable<IExceptionInstance> exceptions = _store.GetSnapshot();

            switch (_format)
            {
                case ExceptionsFormat.JsonSequence:
                case ExceptionsFormat.NewlineDelimitedJson:
                    await WriteJson(outputStream, exceptions, token);
                    break;
                case ExceptionsFormat.PlainText:
                    await WriteText(outputStream, exceptions, token);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public string GenerateFileName()
        {
            throw new NotSupportedException();
        }

        public Task StopAsync(CancellationToken token)
        {
            throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
        }

        private async Task WriteJson(Stream stream, IEnumerable<IExceptionInstance> instances, CancellationToken token)
        {
            foreach (IExceptionInstance instance in instances)
            {
                await WriteJsonInstance(stream, instance, token);
            }
        }

        private async Task WriteJsonInstance(Stream stream, IExceptionInstance instance, CancellationToken token)
        {
            if (_format == ExceptionsFormat.JsonSequence)
            {
                await stream.WriteAsync(JsonSequenceRecordSeparator, token);
            }

            // Make sure dotnet-monitor is self-consistent with other features that print type and stack information.
            // For example, the stacks and exceptions features should print structured stack traces exactly the same way.
            // CONSIDER: Investigate if other tools have "standard" formats for printing structured stacks and exceptions.
            await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions() { Indented = false }))
            {
                writer.WriteStartObject();
                // Writes the timestamp in ISO 8601 format
                writer.WriteString("timestamp", instance.Timestamp);
                writer.WriteString("typeName", instance.TypeName);
                writer.WriteString("moduleName", instance.ModuleName);
                writer.WriteString("message", instance.Message);

                MemoryStream tempStream = new();
                await FormatStack(tempStream, instance.CallStackResult, token);

                tempStream.Position = 0;
                StreamReader reader = new StreamReader(tempStream);
                string callStackText = reader.ReadToEnd();

                writer.WritePropertyName("callStack");
                writer.WriteRawValue(callStackText);

                writer.WriteEndObject();
            }

            await stream.WriteAsync(JsonRecordDelimiter, token);
        }

        // Copy pasted and modified from JsonStacksFormatter
        public static async Task FormatStack(Stream outputStream, Monitoring.WebApi.Stacks.CallStackResult result, CancellationToken token)
        {
            // We know the result only has a single stack
            Monitoring.WebApi.Stacks.CallStack stack = result.Stacks.First(); // is this safe?

            var builder = new StringBuilder();

            Monitoring.WebApi.Models.CallStack stackModel = new Monitoring.WebApi.Models.CallStack();
            stackModel.ThreadId = stack.ThreadId;
            stackModel.ThreadName = stack.ThreadName;

            foreach (Monitoring.WebApi.Stacks.CallStackFrame frame in stack.Frames)
            {
                Monitoring.WebApi.Models.CallStackFrame frameModel = new Monitoring.WebApi.Models.CallStackFrame()
                {
                    ClassName = Monitoring.WebApi.Stacks.NameFormatter.UnknownClass,
                    MethodName = Monitoring.WebApi.Stacks.StacksFormatter.UnknownFunction,
                    //TODO Bring this back once we have a useful offset value
                    //Offset = frame.Offset,
                    ModuleName = Monitoring.WebApi.Stacks.NameFormatter.UnknownModule
                };
                if (frame.FunctionId == 0)
                {
                    frameModel.MethodName = Monitoring.WebApi.Stacks.StacksFormatter.NativeFrame;
                    frameModel.ModuleName = Monitoring.WebApi.Stacks.StacksFormatter.NativeFrame;
                    frameModel.ClassName = Monitoring.WebApi.Stacks.StacksFormatter.NativeFrame;
                }
                else if (result.NameCache.FunctionData.TryGetValue(frame.FunctionId, out Monitoring.WebApi.Stacks.FunctionData functionData))
                {
                    frameModel.ModuleName = Monitoring.WebApi.Stacks.NameFormatter.GetModuleName(result.NameCache, functionData.ModuleId);
                    frameModel.MethodName = functionData.Name;

                    builder.Clear();
                    Monitoring.WebApi.Stacks.NameFormatter.BuildClassName(builder, result.NameCache, functionData);
                    frameModel.ClassName = builder.ToString();

                    if (functionData.TypeArgs.Length > 0)
                    {
                        builder.Clear();
                        builder.Append(functionData.Name);
                        Monitoring.WebApi.Stacks.NameFormatter.BuildGenericParameters(builder, result.NameCache, functionData.TypeArgs);
                        frameModel.MethodName = builder.ToString();
                    }
                }

                stackModel.Frames.Add(frameModel);
            }

            await JsonSerializer.SerializeAsync(outputStream, stackModel, cancellationToken: token);
        }

        private static async Task WriteText(Stream stream, IEnumerable<IExceptionInstance> instances, CancellationToken token)
        {
            foreach (IExceptionInstance instance in instances)
            {
                await WriteTextInstance(stream, instance, token);
            }
        }

        private static async Task WriteTextInstance(Stream stream, IExceptionInstance instance, CancellationToken token)
        {
            // This format is similar of that which is written to the console when an unhandled exception occurs. Each
            // exception will appear as:

            // First chance exception. <TypeName>: <Message>

            await using StreamWriter writer = new(stream, leaveOpen: true);

            await writer.WriteLineAsync(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Strings.OutputFormatString_FirstChanceException,
                    instance.TypeName,
                    instance.Message));

            await writer.FlushAsync();
        }
    }
}
