﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReallyStopDebugger {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ReallyStopDebugger.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Can not create tool window.
        /// </summary>
        internal static string CanNotCreateWindow {
            get {
                return ResourceManager.GetString("CanNotCreateWindow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not resolve IMenuCommandService.
        /// </summary>
        internal static string CommandServiceFail {
            get {
                return ResourceManager.GetString("CommandServiceFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error.
        /// </summary>
        internal static string ErrorTitle {
            get {
                return ResourceManager.GetString("ErrorTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exception: {0}{1}Inside {2}.MenuItemCallback().
        /// </summary>
        internal static string Exception_General {
            get {
                return ResourceManager.GetString("Exception_General", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exception: {0}{1}Inside {2}.MenuItemCallbackLite().
        /// </summary>
        internal static string Exception_General_Lite {
            get {
                return ResourceManager.GetString("Exception_General_Lite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Visual Studio instance not found. Please close this window and try again.
        /// </summary>
        internal static string InvalidInstanceError_1 {
            get {
                return ResourceManager.GetString("InvalidInstanceError_1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please reopen this window and try again.
        /// </summary>
        internal static string InvalidInstanceError_2 {
            get {
                return ResourceManager.GetString("InvalidInstanceError_2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not kill child processes due to an error.
        /// </summary>
        internal static string ProcessesDefaultMessage {
            get {
                return ResourceManager.GetString("ProcessesDefaultMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ReallyStopDebugger&gt;------ Could not kill child processes due to an error.
        /// </summary>
        internal static string ProcessesDefaultMessageLite {
            get {
                return ResourceManager.GetString("ProcessesDefaultMessageLite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Processes killed..
        /// </summary>
        internal static string ProcesseskillsuccessMessage {
            get {
                return ResourceManager.GetString("ProcesseskillsuccessMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ReallyStopDebugger&gt;------ Processes killed.
        /// </summary>
        internal static string ProcesseskillsuccessMessageLite {
            get {
                return ResourceManager.GetString("ProcesseskillsuccessMessageLite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find any matching processes.
        /// </summary>
        internal static string ProcessesnotfoundMessage {
            get {
                return ResourceManager.GetString("ProcessesnotfoundMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ReallyStopDebugger&gt;------ Could not find any matching processes.
        /// </summary>
        internal static string ProcessesnotfoundMessageLite {
            get {
                return ResourceManager.GetString("ProcessesnotfoundMessageLite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An exception has occurred: .
        /// </summary>
        internal static string ProcessKillError_General {
            get {
                return ResourceManager.GetString("ProcessKillError_General", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The process has already exited or was not found.
        /// </summary>
        internal static string ProcessKillError_InvalidOperation {
            get {
                return ResourceManager.GetString("ProcessKillError_InvalidOperation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot kill a process running on a remote computer. Aborting.
        /// </summary>
        internal static string ProcessKillError_NotSupported {
            get {
                return ResourceManager.GetString("ProcessKillError_NotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error killing process: .
        /// </summary>
        internal static string ProcessKillError_Prompt {
            get {
                return ResourceManager.GetString("ProcessKillError_Prompt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The associated process could not be terminated, is terminating or is an invalid Win32 process.
        /// </summary>
        internal static string ProcessKillError_Win32 {
            get {
                return ResourceManager.GetString("ProcessKillError_Win32", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Target processes not found.
        /// </summary>
        internal static string ProcessNotFoundExceptionMessage {
            get {
                return ResourceManager.GetString("ProcessNotFoundExceptionMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loading process dependencies.
        /// </summary>
        internal static string ProgressReport_1 {
            get {
                return ResourceManager.GetString("ProgressReport_1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mapping results to grid.
        /// </summary>
        internal static string ProgressReport_2 {
            get {
                return ResourceManager.GetString("ProgressReport_2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        internal static System.Drawing.Icon reallystopDebugger16 {
            get {
                object obj = ResourceManager.GetObject("reallystopDebugger16", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        internal static System.Drawing.Icon reallystopDebugger32 {
            get {
                object obj = ResourceManager.GetObject("reallystopDebugger32", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap reallystopDebugger48 {
            get {
                object obj = ResourceManager.GetObject("reallystopDebugger48", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        internal static System.Drawing.Icon reallystopDebuggerIcons16 {
            get {
                object obj = ResourceManager.GetObject("reallystopDebuggerIcons16", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find settings manager.
        /// </summary>
        internal static string SettingsManagerNotFound {
            get {
                return ResourceManager.GetString("SettingsManagerNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ToolWindow initialization failed: .
        /// </summary>
        internal static string ToolWindowInitFail {
            get {
                return ResourceManager.GetString("ToolWindowInitFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ReallyStopDebugger Config.
        /// </summary>
        internal static string ToolWindowTitle {
            get {
                return ResourceManager.GetString("ToolWindowTitle", resourceCulture);
            }
        }
    }
}
