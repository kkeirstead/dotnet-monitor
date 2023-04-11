﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
{
    /// <summary>
    /// Egress provider for egressing stream data to the file system.
    /// </summary>
    internal class FileSystemEgressExtension :
        IEgressExtension
    {
        private readonly IEgressConfigurationProvider _configurationProvider;
        private readonly ILogger<FileSystemEgressExtension> _logger;

        public string DisplayName => EgressProviderTypes.FileSystem;

        public FileSystemEgressExtension(IEgressConfigurationProvider configurationProvider, ILogger<FileSystemEgressExtension> logger)
        {
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        public async Task<EgressArtifactResult> EgressArtifact(
            string providerName,
            EgressArtifactSettings settings,
            Func<Stream, CancellationToken, Task> action,
            CancellationToken token)
        {
            return await EgressArtifact(providerName, settings, action, false, token);
        }

        public async Task<EgressArtifactResult> EgressArtifact(
            string providerName,
            EgressArtifactSettings settings,
            Func<Stream, CancellationToken, Task> action,
            bool validateOnly,
            CancellationToken token)
        {
            IConfigurationSection configuration = _configurationProvider.GetProviderConfigurationSection(EgressProviderTypes.FileSystem, providerName);

            FileSystemEgressProviderOptions options = new();
            configuration.Bind(options);

            ValidationContext context = new(options);
            List<ValidationResult> results = new();
            if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
            {
                IList<string> failures = new List<string>();
                foreach (ValidationResult result in results)
                {
                    if (ValidationResult.Success != result)
                    {
                        failures.Add(result.ErrorMessage);
                    }
                }

                if (failures.Count > 0)
                {
                    throw new OptionsValidationException(string.Empty, typeof(FileSystemEgressProviderOptions), failures);
                }
            }

            if (!Directory.Exists(options.DirectoryPath))
            {
                if (results.Count > 0)
                {
                    throw new EgressException(results.First().ErrorMessage);
                }
            }

            string targetPath = string.Empty;

            if (!validateOnly)
            {
                if (!Directory.Exists(options.DirectoryPath))
                {
                    WrapException(() => Directory.CreateDirectory(options.DirectoryPath));
                }

                targetPath = Path.Combine(options.DirectoryPath, settings.Name);

                if (!string.IsNullOrEmpty(options.IntermediateDirectoryPath))
                {
                    if (!Directory.Exists(options.IntermediateDirectoryPath))
                    {
                        WrapException(() => Directory.CreateDirectory(options.IntermediateDirectoryPath));
                    }

                    string intermediateFilePath = null;
                    try
                    {
                        int remainingAttempts = 10;
                        bool intermediatePathExists;
                        do
                        {
                            intermediateFilePath = Path.Combine(options.IntermediateDirectoryPath, Path.GetRandomFileName());
                            intermediatePathExists = File.Exists(intermediateFilePath);
                            remainingAttempts--;
                        }
                        while (intermediatePathExists && remainingAttempts > 0);

                        if (intermediatePathExists)
                        {
                            throw CreateException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressUnableToCreateIntermediateFile, options.IntermediateDirectoryPath));
                        }

                        await WriteFileAsync(action, intermediateFilePath, token);

                        WrapException(() => File.Move(intermediateFilePath, targetPath));
                    }
                    finally
                    {
                        // Attempt to delete the intermediate file if it exists.
                        try
                        {
                            if (File.Exists(intermediateFilePath))
                            {
                                File.Delete(intermediateFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.IntermediateFileDeletionFailed(intermediateFilePath, ex);
                        }
                    }
                }
                else
                {
                    await WriteFileAsync(action, targetPath, token);
                }

                _logger?.EgressProviderSavedStream(EgressProviderTypes.FileSystem, targetPath);
            }

            return new EgressArtifactResult() { Succeeded = true, ArtifactPath = targetPath };
        }

        private async Task WriteFileAsync(Func<Stream, CancellationToken, Task> action, string filePath, CancellationToken token)
        {
            using Stream fileStream = WrapException(
                () => new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None));

            _logger?.EgressProviderInvokeStreamAction(EgressProviderTypes.FileSystem);

            await action(fileStream, token);

            await fileStream.FlushAsync(token);
        }

        private static void WrapException(Action action)
        {
            WrapException(() => { action(); return true; });
        }

        private static T WrapException<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (DirectoryNotFoundException ex)
            {
                throw CreateException(ex);
            }
            catch (PathTooLongException ex)
            {
                throw CreateException(ex);
            }
            catch (IOException ex)
            {
                throw CreateException(ex);
            }
            catch (NotSupportedException ex)
            {
                throw CreateException(ex);
            }
            catch (SecurityException ex)
            {
                throw CreateException(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw CreateException(ex);
            }
        }

        private static EgressException CreateException(string message)
        {
            return new EgressException(WrapMessage(message));
        }

        private static EgressException CreateException(Exception innerException)
        {
            return new EgressException(WrapMessage(innerException.Message), innerException);
        }

        private static string WrapMessage(string innerMessage)
        {
            if (!string.IsNullOrEmpty(innerMessage))
            {
                return string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressFileFailedDetailed, innerMessage);
            }
            else
            {
                return Strings.ErrorMessage_EgressFileFailedGeneric;
            }
        }

        public async Task<EgressArtifactResult> ValidateProviderAsync(string providerName, EgressArtifactSettings settings, Func<Stream, CancellationToken, Task> action, CancellationToken token)
        {
            EgressArtifactResult result = new();
            try
            {
                result = await EgressArtifact(providerName, settings, action, true, token);
            }
            catch (Exception ex)
            {
                result.Succeeded = false;
                result.FailureMessage = ex.Message;
            }

            return result;
        }
    }
}
