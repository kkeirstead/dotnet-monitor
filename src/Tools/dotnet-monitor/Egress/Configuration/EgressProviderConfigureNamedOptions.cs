// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Configure an <see cref="TOptions"/> by binding to
    /// its associated Egress:{ProviderType}:{Name} section in the configuration.
    /// </summary>
    /// <remarks>
    /// Only named options are support for egress providers.
    /// </remarks>
    internal sealed class EgressProviderConfigureNamedOptions<TOptions> :
        IConfigureNamedOptions<TOptions> where TOptions : class
    {
        private readonly IEgressProviderConfigurationProvider<TOptions> _provider;

        public EgressProviderConfigureNamedOptions(IEgressProviderConfigurationProvider<TOptions> provider)
        {
            _provider = provider;
        }

        public void Configure(string name, TOptions options)
        {
            foreach (string providerType in _provider.ProviderTypes)
            {
                IConfigurationSection providerTypeSection = _provider.GetConfigurationSection(providerType);
                IConfigurationSection providerOptionsSection = providerTypeSection.GetSection(name);
                if (providerOptionsSection.Exists())
                {
                    //((ExtensionEgressProviderOptions)options)["Test"] = new Dictionary<string, string>() { { "K1", "V1" } };

                    var tempOptions = new ExtensionEgressProviderOptions();

                    var tempOptions2 = new Dictionary<string, object>();

                    var children = providerOptionsSection.GetChildren();

                    if (options is ExtensionEgressProviderOptions eepOptions)
                    {
                        foreach (var child in children)
                        {
                            if (child.Value != null)
                            {
                                eepOptions.Add(child.Key, child.Value);
                            }
                        }

                        var dictOptions = providerOptionsSection.Get<Dictionary<string, Dictionary<string, string>>>();

                        foreach (var key in dictOptions.Keys)
                        {
                            eepOptions.Add(key, dictOptions[key]);
                        }
                    }



                    //options = tempOptions as TOptions; // Check if this works...? -> does work but isn't passed along

                    //providerOptionsSection.Bind(options); // Should throw an exception if this doesn't bind
                    return;
                }
            }

            throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderDoesNotExist, name));
        }

        public void Configure(TOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
