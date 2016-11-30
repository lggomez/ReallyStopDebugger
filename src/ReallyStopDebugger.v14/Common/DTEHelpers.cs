using EnvDTE;
using System.Runtime.InteropServices;

namespace ReallyStopDebugger.Common
{
    internal static class DTEHelpers
    {
        public static void TryExecuteCommand(this DTE dte, string commandText)
        {
            try
            {
                if (dte != null)
                {
                    dte.ExecuteCommand(commandText);
                }
            }
            catch (COMException)
            {
                // The command Debug.StopDebugging is not available (aka not in debug mode)
            }
        }
    }
}
