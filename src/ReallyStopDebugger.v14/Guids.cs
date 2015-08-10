// Guids.cs
// MUST match guids.h
using System;

namespace lggomez.ReallyStopDebugger
{
    static class GuidList
    {
        public const string guidReallyStopDebuggerPkgString = "6880C4A5-DB6B-4C60-AA84-9EB8973906EA";
        public const string guidReallyStopDebuggerCmdSetString = "D438C789-246B-4EFF-850A-DEA4407DA61B";
        public const string guidReallyStopDebuggerLiteCmdSetString = "DDC6E655-FC04-4D2B-BF00-E4BDC09002DA";
        public const string guidToolWindowPersistanceString = "B29934E0-C066-42EF-A419-48468F4C0DE6";

        public static readonly Guid guidReallyStopDebuggerCmdSet = new Guid(guidReallyStopDebuggerCmdSetString);
    };
}