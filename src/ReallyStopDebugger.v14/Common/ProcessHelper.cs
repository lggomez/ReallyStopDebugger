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
                        p => p.SafeGetProcessName().ToLowerInvariant(),
                        n => (n ?? string.Empty).ToLowerInvariant(),
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
                        Resources.ProcessKillError_Win32,
                        Resources.ProcessKillError_Prompt + currentProcessName);
                }
                return Constants.Processeskillerror;
            }
            catch (NotSupportedException)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        Resources.ProcessKillError_NotSupported,
                        Resources.ProcessKillError_Prompt + currentProcessName);
                }
                return Constants.Processeskillerror;
            }
            catch (InvalidOperationException)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        Resources.ProcessKillError_InvalidOperation,
                        Resources.ProcessKillError_Prompt + currentProcessName);
                }
                return Constants.Processeskillerror;
            }
            catch (Exception ex)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(
                        string.Format(Resources.ProcessKillError_General + ex.Message),
                        Resources.ProcessKillError_Prompt + currentProcessName);
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