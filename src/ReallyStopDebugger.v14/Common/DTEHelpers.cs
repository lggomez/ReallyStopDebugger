// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using EnvDTE;
using System.Runtime.InteropServices;

namespace ReallyStopDebugger.Common
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
