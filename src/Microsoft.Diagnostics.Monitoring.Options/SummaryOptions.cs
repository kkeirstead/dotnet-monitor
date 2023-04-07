// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed partial class SummaryOptions
    {
/*        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AuthenticationOptions_MonitorApiKey))]*/
        public bool Enabled { get; set; }

/*        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AuthenticationOptions_AzureAd))]*/
        public string Egress { get; set; }
    }
}
