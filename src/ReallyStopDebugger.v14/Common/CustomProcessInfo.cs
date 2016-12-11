// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

namespace ReallyStopDebugger.Common
{
    public class CustomProcessInfo
    {
        public string ProcessName { get; set; }

        public CustomProcessInfo()
        {
        }

        public CustomProcessInfo(string processName)
        {
            this.ProcessName = processName;
        }
    }
}