﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            ValidationContext filtersContext = new(Filters, validationContext, validationContext.Items);
            filtersContext.MemberName = nameof(Filters);
            ValidationHelper.TryValidateItems(Filters, filtersContext, results);

            if (null != Trigger)
            {
                ValidationContext triggerContext = new(Trigger, validationContext, validationContext.Items);
                triggerContext.MemberName = nameof(Trigger);
                Validator.TryValidateObject(Trigger, triggerContext, results);
            }

            ValidationContext actionsContext = new(Actions, validationContext, validationContext.Items);
            actionsContext.MemberName = nameof(Actions);
            ValidationHelper.TryValidateItems(Actions, actionsContext, results);

            var actionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach(CollectionRuleActionOptions option in Actions)
            {
                if (!string.IsNullOrEmpty(option.Name) && !actionNames.Add(option.Name))
                {
                    results.Add(new ValidationResult(
                        string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_DuplicateActionName, option.Name),
                        new[] { nameof(option.Name) }));
                }
            }

            // Always want to evaluate this (even if null) for the defaults
            Limits = (null == Limits) ? new CollectionRuleLimitsOptions() : Limits;
            ValidationContext limitsContext = new(Limits, validationContext, validationContext.Items);
            limitsContext.MemberName = nameof(Trigger);
            Validator.TryValidateObject(Limits, limitsContext, results);

            return results;
        }
    }
}
