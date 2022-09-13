﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Azure blob egress failed: {0}.
        /// </summary>
        internal static string ErrorMessage_EgressAzureFailedDetailed {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressAzureFailedDetailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Azure blob egress failed..
        /// </summary>
        internal static string ErrorMessage_EgressAzureFailedGeneric {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressAzureFailedGeneric", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SharedAccessSignature, AccountKey, or ManagedIdentityClientId must be specified..
        /// </summary>
        internal static string ErrorMessage_EgressMissingCredentials {
            get {
                return ResourceManager.GetString("ErrorMessage_EgressMissingCredentials", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Metadata cannot include duplicate keys; please change or remove the key &apos;{0}&apos;.
        /// </summary>
        internal static string LogFormatString_DuplicateKeyInMetadata {
            get {
                return ResourceManager.GetString("LogFormatString_DuplicateKeyInMetadata", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Copying action stream to egress stream with buffer size {0}.
        /// </summary>
        internal static string LogFormatString_EgressCopyActionStreamToEgressStream {
            get {
                return ResourceManager.GetString("LogFormatString_EgressCopyActionStreamToEgressStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider {0}: Invoking stream action..
        /// </summary>
        internal static string LogFormatString_EgressProviderInvokeStreamAction {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderInvokeStreamAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider {0}: Saved stream to {1}.
        /// </summary>
        internal static string LogFormatString_EgressProviderSavedStream {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderSavedStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provider {0}: Unable to find &apos;{1}&apos; key in egress properties.
        /// </summary>
        internal static string LogFormatString_EgressProviderUnableToFindPropertyKey {
            get {
                return ResourceManager.GetString("LogFormatString_EgressProviderUnableToFindPropertyKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Target framework does not support custom egress metadata..
        /// </summary>
        internal static string LogFormatString_EnvironmentBlockNotSupported {
            get {
                return ResourceManager.GetString("LogFormatString_EnvironmentBlockNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The environment variable &apos;{0}&apos; could not be found on the target process..
        /// </summary>
        internal static string LogFormatString_EnvironmentVariableNotFound {
            get {
                return ResourceManager.GetString("LogFormatString_EnvironmentVariableNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid metadata; custom metadata keys must be valid C# identifiers..
        /// </summary>
        internal static string LogFormatString_InvalidMetadata {
            get {
                return ResourceManager.GetString("LogFormatString_InvalidMetadata", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The queue {0} does not exist; ensure that the {1} and {2} fields are set correctly..
        /// </summary>
        internal static string LogFormatString_QueueDoesNotExist {
            get {
                return ResourceManager.GetString("LogFormatString_QueueDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Queue message egress requires {0} and {1} to be set.
        /// </summary>
        internal static string LogFormatString_QueueOptionsPartiallySet {
            get {
                return ResourceManager.GetString("LogFormatString_QueueOptionsPartiallySet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to send message to the queue {0}..
        /// </summary>
        internal static string LogFormatString_WritingMessageToQueueFailed {
            get {
                return ResourceManager.GetString("LogFormatString_WritingMessageToQueueFailed", resourceCulture);
            }
        }
    }
}
