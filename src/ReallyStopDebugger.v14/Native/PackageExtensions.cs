// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ReallyStopDebugger.Native
{
    internal static class PackageExtensions
    {
        /// <summary>
        /// Shows an error message using the VsShellUtilities helper.
        /// </summary>
        public static void ShowErrorMessage(this Package package, string message, string title)
        {
            ErrorHandler.ThrowOnFailure(VsShellUtilities.ShowMessageBox(
                package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST));
        }

        /// <summary>
        /// Shows an informational message using the VsShellUtilities helper.
        /// </summary>
        public static void ShowInfoMessage(this Package package, string message, string title)
        {
            ErrorHandler.ThrowOnFailure(VsShellUtilities.ShowMessageBox(
                package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST));
        }
    }
}
