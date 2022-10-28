// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.Custom;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the AspNetRequestCount trigger.
    /// </summary>
    internal sealed class CustomOptions
    {
        public string ExtensionName { get; set; }

        public string Args { get; set; }

        public Provider[] Providers { get; set; }

    }
}
