// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal sealed class ExtensionEgressPayload
    {
        public EgressArtifactSettings Settings { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public IDictionary<string, string> Configuration { get; set; }
        public string ProviderName { get; set; }
        public bool ValidationCheck { get; set; }
    }
}
