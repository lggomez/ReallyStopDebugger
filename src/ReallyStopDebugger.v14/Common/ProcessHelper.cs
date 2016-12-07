// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using Microsoft.VisualStudio.Shell;

using ReallyStopDebugger.Native;

namespace ReallyStopDebugger.Common
{
    internal static class ProcessHelper
    {
        public static int KillProcesses(
            Package package,
            IList processNames,
            bool restrictUser,
            bool restrictChildren,
            bool consoleMode = false)
        {
            var currentProcessName = string.Empty;

            try
            {
                var runningProcesses = restrictUser
                                           ? WindowsNative.GetCurrentUserProcesses()
                                           : Process.GetProcesses().ToList();

                var filteredProcesses =
                    runningProcesses.Join(
                        processNames.Cast<string>(),
                        p => p.SafeGetProcessName().ToLower(),
                        n => (n ?? string.Empty).ToLower(),
                        (p, n) => p).ToList();

                if (restrictChildren)
                {
                    var childProcesses = WindowsNative.GetChildProcesses(WindowsNative.GetCurrentProcess().SafeGetProcessId());

                    filteredProcesses =
                        (from p in filteredProcesses join c in childProcesses on p.SafeGetProcessId() equals c.SafeGetProcessId() select p).ToList();
                }

                if (!filteredProcesses.Any()) return Constants.Processesnotfound;

                foreach (var p in filteredProcesses)
                {
                    currentProcessName = p.SafeGetProcessName();
                    p.Kill();
                }
            }
            catch (Win32Exception)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        "The associated process could not be terminated, is terminating or is an invalid Win32 process.",
                        "Error killing process: " + currentProcessName);
                }
                return Constants.Processeskillerror;
            }
            catch (NotSupportedException)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        "Cannot kill a process running on a remote computer. Aborting.",
                        "Error killing process: " + currentProcessName);
                }
                return Constants.Processeskillerror;
            }
            catch (InvalidOperationException)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        "The process has already exited or was not found.",
                        "Error killing process: " + currentProcessName);
                }
                return Constants.Processeskillerror;
            }
            catch (Exception ex)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        string.Format("An exception has occurred: " + ex.Message),
                        "Error killing process: " + currentProcessName);
                }
                return Constants.Processeskillerror;
            }

            return Constants.Processeskillsuccess;
        }

        public static string GetProcessPath(Process process)
        {
            return WindowsNative.GetProcessFilePath(process.SafeGetProcessId());
        }

        public static Icon GetProcessIcon(Process process)
        {
            try
            {
                return Icon.ExtractAssociatedIcon(GetProcessPath(process));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}