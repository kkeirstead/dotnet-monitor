// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class ExceptionsConfiguration
    {
        /// <summary>
        /// The list of exceptions to include.
        /// </summary>
        [JsonPropertyName("include")]
        public List<string> Include { get; set; } = new();

        /// <summary>
        /// The list of exceptions to exclude.
        /// </summary>
        [JsonPropertyName("exclude")]
        public List<string> Exclude { get; set; } = new();
    }
}
