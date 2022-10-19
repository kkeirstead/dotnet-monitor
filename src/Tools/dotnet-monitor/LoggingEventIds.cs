﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    // The existing EventIds must not be duplicated, reused, or repurposed.
    // New logging events must use the next available EventId.
    internal enum LoggingEventIds
    {
        EgressProviderAdded = 1,
        EgressProviderInvalidOptions = 2,
        EgressProviderInvalidType = 3,
        EgressProviderValidatingOptions = 4,
        EgressCopyActionStreamToEgressStream = 5,
        EgressProviderOptionsValidationFailure = 6,
        EgressProviderOptionValue = 7,
        EgressStreamOptionValue = 8,
        EgressProviderFileName = 9,
        EgressProviderInvokeStreamAction = 11,
        EgressProviderSavedStream = 12,
        NoAuthentication = 13,
        InsecureAuthenticationConfiguration = 14,
        UnableToListenToAddress = 15,
        BoundDefaultAddress = 16,
        BoundMetricsAddress = 17,
        OptionsValidationFailure = 18,
        RunningElevated = 19,
        DisabledNegotiateWhileElevated = 20,
        ApiKeyValidationFailure = 21,
        ApiKeyAuthenticationOptionsChanged = 22,
        LogTempApiKey = 23,
        DuplicateEgressProviderIgnored = 24,
        ApiKeyAuthenticationOptionsValidated = 25,
        NotifyPrivateKey = 26,
        DuplicateCollectionRuleActionIgnored = 27,
        DuplicateCollectionRuleTriggerIgnored = 28,
        CollectionRuleStarted = 29,
        CollectionRuleFailed = 30,
        CollectionRuleCompleted = 31,
        CollectionRulesStarted = 32,
        CollectionRuleActionStarted = 33,
        CollectionRuleActionCompleted = 34,
        CollectionRuleTriggerStarted = 35,
        CollectionRuleTriggerCompleted = 36,
        CollectionRuleActionsThrottled = 37,
        CollectionRuleActionFailed = 38,
        CollectionRuleActionsCompleted = 39,
        CollectionRulesStarting = 40,
        DiagnosticRequestCancelled = 41,
        CollectionRuleUnmatchedFilters = 42,
        CollectionRuleConfigurationChanged = 43,
        CollectionRulesStopping = 44,
        CollectionRulesStopped = 45,
        CollectionRuleCancelled = 46,
        DiagnosticRequestFailed = 47,
        InvalidActionReferenceToken = 48,
        InvalidActionReference = 49,
        InvalidActionResultReference = 50,
        ActionSettingsTokenizationNotSupported = 51,
        EndpointTimeout = 52,
        LoadingProfiler = 53,
        SetEnvironmentVariable = 54,
        GetEnvironmentVariable = 55,
        MonitorApiKeyNotConfigured = 56, // Note the gap - from removing things related to Azure egress
        ExperienceSurvey = 60,
        DiagnosticPortNotInListenModeForCollectionRules = 61,
        RuntimeInstanceCookieFailedToFilterSelf = 62,
        ParsingUrlFailed = 63,
        IntermediateFileDeletionFailed = 64,
        DiagnosticPortDeleteAttempt = 65,
        DiagnosticPortDeleteFailed = 66,
        DiagnosticPortAlteredWhileInUse = 67,
        DiagnosticPortWatchingFailed = 68,
        FailedInitializeSharedLibraryStorage = 73,
        UnableToApplyProfiler = 74,
        SharedLibraryPath = 75,
        ConnectionModeConnect = 76,
        ConnectionModeListen = 77,
        ExperimentalFeatureEnabled = 78,
        ExtensionProbeStart = 79,
        ExtensionProbeRepo = 80,
        ExtensionProbeSucceeded = 81,
        ExtensionProbeFailed = 82,
        ExtensionStarting = 83,
        ExtensionConfigured = 84,
        ExtensionEgressPayloadCompleted = 85,
        ExtensionExited = 86,
        ExtensionOutputMessage = 87,
        ExtensionErrorMessage = 88,
        ExtensionNotOfType = 89,
        ExtensionDeclarationFileBroken = 90,
        ExtensionProgramMissing = 91,
        ExtensionMalformedOutput = 92
    }

    internal static class LoggingEventIdsExtensions
    {
        public static EventId EventId(this LoggingEventIds enumVal)
        {
            string name = Enum.GetName(typeof(LoggingEventIds), enumVal);
            int id = enumVal.Id();
            return new EventId(id, name);
        }
        public static int Id(this LoggingEventIds enumVal)
        {
            int id = (int)enumVal;
            return id;
        }
    }
}
