// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

// Guids.cs
// MUST match guids.h
using System;

namespace lggomez.ReallyStopDebugger
{
    static class GuidList
    {
        public const string guidReallyStopDebuggerPkgString = "F4802ACB-BF36-4A39-98C5-C7C24CDB82FA";
        public const string guidReallyStopDebuggerCmdSetString = "D2C106BC-114A-48B7-947E-25D32F287F06";
        public const string guidReallyStopDebuggerLiteCmdSetString = "73F445D3-2695-4331-9458-3F0848A27746";
        public const string guidToolWindowPersistanceString = "608F8180-F49A-4FF1-8662-AAB47DFF6803";

        public static readonly Guid guidReallyStopDebuggerCmdSet = new Guid(guidReallyStopDebuggerCmdSetString);
    };
}