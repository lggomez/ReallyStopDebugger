using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace ReallyStopDebugger
{
    public class ProcessInfo
    {
        private readonly Process originProcess;

        public int Id
        {
            get
            {
                return this.originProcess.Id;
            }
        }

        public string Domain { get; }

        public string ProcessName
        {
            get
            {
                return this.originProcess.ProcessName;
            }
        }

        public string FileName { get; }

        public string UserName { get; }

        public string WorkingDirectory { get; }

        public int ProcessCount { get; }

        public bool IsSelected { get; set; }

        public ProcessInfo(IEnumerable<Process> processes)
        {
            this.originProcess = processes.First();
            this.ProcessCount = processes.Count();
        }
    }
}