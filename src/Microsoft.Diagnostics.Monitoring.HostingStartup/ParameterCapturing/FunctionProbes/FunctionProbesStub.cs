// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
{
    public static class FunctionProbesStub
    {
        private delegate void EnterProbeDelegate(ulong uniquifier, object[] args);
        private delegate void EnterProbeDelegate2(ulong uniquifier, object[] args);
        private static readonly EnterProbeDelegate2 s_fixedEnterProbeDelegate = EnterProbeStub2;

        [ThreadStatic]
        private static bool s_inProbe;

        internal static FunctionProbesCache? Cache { get; set; }

        internal static IFunctionProbes? Instance { get; set; }

        internal static ulong GetProbeFunctionId()
        {
            return s_fixedEnterProbeDelegate.Method.GetFunctionId();
        }

        // Is this the ProbeFunction referred to by the profiler, being reverse p-invoked (or something like that)?
        public static void EnterProbeStub(ulong uniquifier, object[] args)
        {
            IFunctionProbes? probes = Instance;
            if (probes == null || s_inProbe)
            {
                return;
            }

            try
            {
                s_inProbe = true;
                //probes.EnterProbe(uniquifier, args);
                probes.EnterProbe(uniquifier, args);
            }
            finally
            {
                s_inProbe = false;
            }
        }

        // Experimenting with line numbers
        public static void EnterProbeStub2(ulong uniquifier, object[] args)
        {
            IFunctionProbes? probes = Instance;
            if (probes == null || s_inProbe)
            {
                return;
            }

            try
            {
                s_inProbe = true;
                probes.EnterProbe2(uniquifier, args);
            }
            finally
            {
                s_inProbe = false;
            }
        }
    }
}
