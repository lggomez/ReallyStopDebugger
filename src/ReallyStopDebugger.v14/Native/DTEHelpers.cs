// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System.Runtime.InteropServices;

using EnvDTE;

namespace ReallyStopDebugger.Native
{
    internal static class DteHelpers
    {
        public static void TryExecuteCommand(this DTE dte, string commandText)
        {
            try
            {
                dte?.ExecuteCommand(commandText);
            }
            catch (COMException)
            {
                // The command Debug.StopDebugging is not available (aka not in debug mode)
            }
        }
    }
}
