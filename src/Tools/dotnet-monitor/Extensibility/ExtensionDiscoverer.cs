﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionDiscoverer
    {
        private readonly ExtensionRepository[] _extensionRepos;
        private readonly ILogger<ExtensionDiscoverer> _logger;

        private const string AzureBlobStorageConfigurationName = "AzureBlobStorage";
        private const string AzureBlobStorageToolName = "dotnet-monitor-egress-azureblobstorage"; // This must be the same as the AzureBlobStorage project's AssemblyName

        public ExtensionDiscoverer(IEnumerable<ExtensionRepository> extensionRepos, ILogger<ExtensionDiscoverer> logger)
        {
            _extensionRepos = extensionRepos.OrderBy(eRepo => eRepo.ResolvePriority).ToArray<ExtensionRepository>();
            _logger = logger;
        }

        /// <summary>
        /// Attempts to locate an extension with the given moniker and return it in the provided type.
        /// </summary>
        /// <typeparam name="TExtensionType">The type of the extension that must be found.</typeparam>
        /// <param name="extensionName">The string moniker used to refer to the extension</param>
        /// <returns></returns>
        /// <exception cref="ExtensionException">Thrown when the target extension is not found.</exception>
        public TExtensionType FindExtension<TExtensionType>(string extensionName) where TExtensionType : class, IExtension
        {
            _logger.ExtensionProbeStart(extensionName);
            foreach (ExtensionRepository repo in _extensionRepos)
            {
                bool found = false;
                IExtension genericResult = null;

                // Configuration written for the in-box AzureBlobStorage egress provider
                // should work automatically with the extension; this translates the
                // existing name (AzureBlobStorage) to our extension's AssemblyName
                if (extensionName == AzureBlobStorageConfigurationName)
                {
                    found = repo.TryFindExtension(AzureBlobStorageToolName, out genericResult);
                }

                if (!found)
                {
                    found = repo.TryFindExtension(extensionName, out genericResult);
                }

                if (found)
                {
                    bool isOfType = genericResult is TExtensionType;
                    if (isOfType)
                    {
                        _logger.ExtensionProbeSucceeded(extensionName, genericResult);
                        return (TExtensionType)genericResult;
                    }
                    else
                    {
                        _logger.ExtensionNotOfType(extensionName, genericResult, typeof(TExtensionType));
                    }
                }
            }
            _logger.ExtensionProbeFailed(extensionName);
            ExtensionException.ThrowNotFound(extensionName);

            // This will never get hit because the statement above should always throw
            return null;
        }
    }
}
