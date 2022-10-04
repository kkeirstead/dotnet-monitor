// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class NonPRTraitDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            bool nonPR = traitAttribute.GetConstructorArguments().OfType<bool>().Single();
            yield return new KeyValuePair<string, string>("NonPR", nonPR.ToString());
        }
    }
}
