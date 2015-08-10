using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Process = System.Diagnostics.Process;

namespace lggomez.ReallyStopDebugger.Common
{
    internal static class ProcessHelper
    {
        public static int KillProcesses(Package package, string[] processNames, bool restrictUser, bool consoleMode = false)
        {
            string currentProcessName = string.Empty;

            try
            {
                var runningProcesses = new List<Process>();

                if (restrictUser)
                {
                    runningProcesses = WindowsInterop.GetCurrentUserProcesses();
                }
                else
                {
                    runningProcesses = Process.GetProcesses().ToList();
                }

                List<Process> filteredProcesses = runningProcesses.Join(processNames,
                    p => (p.ProcessName ?? string.Empty).ToLower(),
                    n => (n ?? string.Empty).ToLower(),
                    (p, n) => p)
                    .ToList();

                if (!filteredProcesses.Any())
                    return Constants.PROCESSESNOTFOUND;

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
                    package.ShowErrorMessage(string.Format("The associated process could not be terminated, is terminating or is an invalid Win32 process."), "Error killing process: " + currentProcessName);
                }
                return Constants.PROCESSESKILLERROR;
            }
            catch (NotSupportedException)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(string.Format("Cannot kill a process running on a remote computer. Aborting."), "Error killing process: " + currentProcessName);
                }
                return Constants.PROCESSESKILLERROR;
            }
            catch (InvalidOperationException)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(string.Format("The process has already exited or was not found."), "Error killing process: " + currentProcessName);
                }
                return Constants.PROCESSESKILLERROR;
            }
            catch (Exception ex)
            {
                if (!consoleMode)
                {
                    package.ShowErrorMessage(string.Format("An exception has occurred: " + ex.Message), "Error killing process: " + currentProcessName);
                }
                return Constants.PROCESSESKILLERROR;
            }

            return Constants.PROCESSESKILLSUCCESS;
        }
    }
}
