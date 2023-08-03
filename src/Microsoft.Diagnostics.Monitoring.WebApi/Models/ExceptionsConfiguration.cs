// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class ExceptionsConfiguration
    {
        /// <summary>
        /// The list of exceptions to include.
        /// </summary>
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CorsConfiguration_AllowedOrigins))]
        public List<string> Include { get; set; } = new();

        /// <summary>
        /// The list of exceptions to exclude.
        /// </summary>
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CorsConfiguration_AllowedOrigins))]
        public List<string> Exclude { get; set; } = new();
    }
}
