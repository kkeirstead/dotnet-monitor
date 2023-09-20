// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal interface IFunctionProbes
    {
        public List<string> Hits { get; set; } // figure out a good type (not List<string>)
        public void EnterProbe(ulong uniquifier, object[] args);
        public void EnterProbe2(ulong uniquifier, object[] args);
    }
}
