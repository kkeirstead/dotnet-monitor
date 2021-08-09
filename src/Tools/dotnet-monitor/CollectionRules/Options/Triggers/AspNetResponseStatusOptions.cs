﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
#endif
{
    /// <summary>
    /// Options for the AspNetResponseStatus trigger.
    /// </summary>
    internal sealed class AspNetResponseStatusOptions
    {
        [Required]
        public string StatusCodes { get; set; }

        [Required]
        public int ResponseCount { get; set; }

        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        public TimeSpan? SlidingWindowDuration { get; set; }

        public string IncludePaths { get; set; }

        public string ExcludePaths { get; set; }
    }
}
