// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

using ReallyStopDebugger.Native;

namespace ReallyStopDebugger.Common
{
    public class ProcessInfo
    {
        private readonly Process originProcess;

        private readonly BitmapSource defaultProcessIcon =
            ((Icon)
                typeof(Form).GetProperty(
                        "DefaultIcon",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .GetValue(null, null)).ToBitmapSource();

        public int Id => this.originProcess.SafeGetProcessId();

        private BitmapSource executableIcon;

        public BitmapSource ExecutableIcon
        {
            get
            {
                return this.executableIcon;
            }
            private set
            {
                this.executableIcon = value ?? this.defaultProcessIcon;
            }
        }

        public string ProcessName => this.originProcess.SafeGetProcessName();

        public string FilePath => ProcessHelper.GetProcessPath(this.originProcess);

        public int ProcessCount { get; }

        public bool IsSelected { get; set; }

        public ProcessInfo(IEnumerable<Process> processes)
        {
            this.originProcess = processes.First();
            this.ProcessCount = processes.Count();
            this.ExecutableIcon = ProcessHelper.GetProcessIcon(this.originProcess)?.ToBitmapSource();
        }
    }
}