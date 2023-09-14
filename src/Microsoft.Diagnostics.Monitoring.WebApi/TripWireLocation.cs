// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal sealed class TripWireLocation : IMethodDescription
    {
        public string ModuleName { get; set; }
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public int LineNumber { get; set; }

        public string VariableName { get; set; }

        public object VariableValue { get; set; }

    }
}
