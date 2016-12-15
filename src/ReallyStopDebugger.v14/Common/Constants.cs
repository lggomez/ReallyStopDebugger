// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;

namespace ReallyStopDebugger.Common
{
    public static class Constants
    {
        public static readonly string[] DeafultFilter = { "MSBuild" };
    }

    [Flags]
    public enum ProcessOperationResults : uint
    {
        None = 0,

        Success = 1,

        Error = 1 << 1,

        NotFound = 1 << 2,
    }
}