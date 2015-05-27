using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace lggomez.ReallyStopDebugger.Common
{
    internal static class PackageExtensions
    {
        public static void ShowErrorMessage(this Package package, string message, string title){
            ErrorHandler.ThrowOnFailure(VsShellUtilities.ShowMessageBox(
                package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST));
        }

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
