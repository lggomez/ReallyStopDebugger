// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace ReallyStopDebugger.Common
{
    public static class WindowsInterop
    {
        #region Marshalling structs & consts
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        };

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        private enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F,
            NoH
        }

        //Token security access
        public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const uint STANDARD_RIGHTS_READ = 0x00020000;
        public const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const uint TOKEN_DUPLICATE = 0x0002;
        public const uint TOKEN_IMPERSONATE = 0x0004;
        public const uint TOKEN_QUERY = 0x0008;
        public const uint TOKEN_QUERY_SOURCE = 0x0010;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_ADJUST_GROUPS = 0x0040;
        public const uint TOKEN_ADJUST_DEFAULT = 0x0080;
        public const uint TOKEN_ADJUST_SESSIONID = 0x0100;
        public const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
        TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
        TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
        TOKEN_ADJUST_SESSIONID);

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId
        }

        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }
        #endregion

        #region Winapi dll imports
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             int processId);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr CreateToolhelp32Snapshot([In]UInt32 dwFlags, [In]UInt32 th32ProcessId);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern bool Process32First([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern bool Process32Next([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess, 
            out IntPtr tokenHandle);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetTokenInformation(
            IntPtr hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr tokenInformation,
            int tokeInfoLength,
            ref int reqLength);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool ConvertSidToStringSid(
            IntPtr pSid,
            [In][Out][MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid);

        #endregion

        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.Id);
        }

        public static Process GetCurrentProcess()
        {
            return Process.GetCurrentProcess();
        }

        public static List<Process> GetChildProcesses(int processId)
        {
            var childProcesses = new List<Process>();
            var snapshotHandle = IntPtr.Zero;

            try
            {
                var targetSize = (UInt32)Marshal.SizeOf(typeof(PROCESSENTRY32));
                var processEntry = new PROCESSENTRY32 { dwSize = targetSize };
                snapshotHandle = CreateToolhelp32Snapshot((uint)SnapshotFlags.Process, 0);

                if (Process32First(snapshotHandle, ref processEntry))
                {
                    do
                    {
                        if (processId == processEntry.th32ParentProcessID)
                        {
                            childProcesses.Add(Process.GetProcessById((int)processEntry.th32ProcessID));
                        }
                    } while (Process32Next(snapshotHandle, ref processEntry));
                }
            }
            catch (Exception)
            {
                return new List<Process>();
            }
            finally
            {
                CloseHandle(snapshotHandle);
            }

            return childProcesses;
        }

        public static bool GetProcessUser(IntPtr processHandle, out string userSID)
        {
            userSID = null;

            try
            {
                IntPtr processToken;

                if (OpenProcessToken(processHandle, TOKEN_QUERY, out processToken))
                {
                    GetUserSidFromProcessToken(processToken, out userSID);
                    CloseHandle(processToken);
                }

                return Marshal.GetLastWin32Error() == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool GetUserSidFromProcessToken(IntPtr processToken, out string userSID)
        {
            const int BufLength = 256;
            var tokenInformation = Marshal.AllocHGlobal(BufLength);
            userSID = null;

            try
            {
                var tokenInformationLength = BufLength;
                var ret = GetTokenInformation(processToken, TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, tokenInformationLength, ref tokenInformationLength);

                if (ret)
                {
                    var tokenUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_USER));
                    ConvertSidToStringSid(tokenUser.User.Sid, ref userSID);
                }

                return ret;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }
        }

        public static List<KeyValuePair<Process, string>> GetCurrentProcessesWIthUserSIDs()
        {
            var runningProcesses = Process.GetProcesses();
            var result = new List<KeyValuePair<Process, string>>();

            foreach (Process process in runningProcesses)
            {
                string userSID = null;

                try
                {
                    GetProcessUser(process.Handle, out userSID);
                }
                catch (Exception ex)
                {
                    // Failure to obtain user SID indicates that the process is either in an
                    // invalid state or we have access denied, so we ignore it
                    if ((ex is InvalidOperationException) || (ex is Win32Exception))
                        continue;
                }

                if (userSID != null)
                {
                    result.Add(new KeyValuePair<Process, string>(process, userSID));
                }
            }

            return result;
        }

        public static string GetCurrentUserSid()
        {
            try
            {
                var windowsIdentity = WindowsIdentity.GetCurrent();

                if ((windowsIdentity != null) && (windowsIdentity.User != null))
                {
                    return windowsIdentity.User.Value;
                }

                return null;
            }
            catch (SecurityException)
            {
                return null;
            }
        }

        public static List<Process> GetCurrentUserProcesses()
        {
            var runningProcesses = GetCurrentProcessesWIthUserSIDs();
            var userSID = GetCurrentUserSid();

            return runningProcesses
                .Where(kv => kv.Value.Equals(userSID, StringComparison.InvariantCultureIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();
        }
    }
}
