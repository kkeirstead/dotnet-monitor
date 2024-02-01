﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    internal sealed class FunctionProbesState
    {
        public ReadOnlyDictionary<ulong, InstrumentedMethod> InstrumentedMethods { get; }

        public IFunctionProbes Probes { get; }

        // add workflow id to this
        public FunctionProbesState(ReadOnlyDictionary<ulong, InstrumentedMethod> instrumentedMethods, IFunctionProbes probes)
        {
            InstrumentedMethods = instrumentedMethods;
            Probes = probes;
        }
    }
}
