// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;

namespace ReallyStopDebugger.Common
{
    public static class Constants
    {
        public static readonly string[] DeafultFilter = { "MSBuild" };

        #region Configuration keys

        public static string CollectionPath { get; } = "ReallyStopDebugger";

        public static string ForceCleanProperty { get; } = "ForceClean";

        public static string CustomProcessesProperty { get; } = "CustomProcessList";

        public static string UserProcessMatchProperty { get; } = "UserProcessMatch";

        public static string ChildProcessMatchProperty { get; } = "ChildProcessMatch";

        #endregion
    }

    [Flags]
    public enum ProcessOperationResults : uint
    {
        None = 0,

        Success = 1,

        Error = 1 << 1,

        NotFound = 1 << 2
    }
}