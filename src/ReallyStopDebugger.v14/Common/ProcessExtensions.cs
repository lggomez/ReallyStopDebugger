// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Diagnostics;

namespace ReallyStopDebugger.Common
{
    /// <summary>
    /// These wrappers catch exceptions when accessing closed processes information
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// If the process was closed, return an empty process name
        /// </summary>
        public static string SafeGetProcessName(this Process p)
        {
            try
            {
                return !p.HasExited ? p.ProcessName : string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// If the process was closed, return an invalid process id
        /// </summary>
        public static int SafeGetProcessId(this Process p)
        {
            try
            {
                return !p.HasExited ? p.Id : 0;
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }
    }
}
