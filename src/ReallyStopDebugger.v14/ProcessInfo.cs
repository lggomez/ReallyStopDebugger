using System.Diagnostics;

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

        public ProcessInfo(Process process)
        {
            this.originProcess = process;
        }
    }
}