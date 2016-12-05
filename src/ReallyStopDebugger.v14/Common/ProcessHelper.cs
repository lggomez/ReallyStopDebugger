// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using Microsoft.VisualStudio.Shell;

using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Drawing;

using Process = System.Diagnostics.Process;

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
                                           ? WindowsInterop.GetCurrentUserProcesses()
                                           : Process.GetProcesses().ToList();

                var filteredProcesses =
                    runningProcesses.Join(
                        processNames.Cast<string>(),
                        p => (p.ProcessName).ToLower(),
                        n => (n ?? string.Empty).ToLower(),
                        (p, n) => p).ToList();

                if (restrictChildren)
                {
                    var childProcesses = WindowsInterop.GetChildProcesses(WindowsInterop.GetCurrentProcess().Id);

                    filteredProcesses =
                        (from p in filteredProcesses join c in childProcesses on p.Id equals c.Id select p).ToList();
                }

                if (!filteredProcesses.Any()) return Constants.Processesnotfound;

                foreach (var p in filteredProcesses)
                {
                    currentProcessName = p.ProcessName;
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
            return WindowsInterop.GetProcessPath(process.Id);
        }

        public static string GetProcessFileName(Process process)
        {
            return WindowsInterop.GetProcessFileName(process.Id);
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