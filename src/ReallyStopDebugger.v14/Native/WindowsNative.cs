// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

using Microsoft.Win32.SafeHandles;

using ReallyStopDebugger.Common;

namespace ReallyStopDebugger.Native
{
    using System.Drawing;
    using System.Runtime.CompilerServices;
    using System.Windows.Forms;

    internal static class WindowsNative
    {
        #region Marshalling structs & consts

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct Processentry32
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
        internal enum ProcessAccessFlags : uint
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
        internal enum SnapshotFlags : uint
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

        #region Token security access
        internal const uint StandardRightsRequired = 0x000F0000;

        internal const uint StandardRightsRead = 0x00020000;

        internal const uint TokenAssignPrimary = 0x0001;

        internal const uint TokenDuplicate = 0x0002;

        internal const uint TokenImpersonate = 0x0004;

        internal const uint TokenQuery = 0x0008;

        internal const uint TokenQuerySource = 0x0010;

        internal const uint TokenAdjustPrivileges = 0x0020;

        internal const uint TokenAdjustGroups = 0x0040;

        internal const uint TokenAdjustDefault = 0x0080;

        internal const uint TokenAdjustSessionid = 0x0100;

        internal const uint TokenRead = StandardRightsRead | TokenQuery;

        internal const uint TokenAllAccess =
            StandardRightsRequired | TokenAssignPrimary | TokenDuplicate | TokenImpersonate | TokenQuery
            | TokenQuerySource | TokenAdjustPrivileges | TokenAdjustGroups | TokenAdjustDefault | TokenAdjustSessionid; 
        #endregion

        internal enum TOKEN_INFORMATION_CLASS
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

        internal struct TokenUser
        {
            public SidAndAttributes User;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SidAndAttributes
        {
            public IntPtr Sid;

            public int Attributes;
        }

        internal enum SID_NAME_USE
        {
            SidTypeUser = 1,

            SidTypeGroup,

            SidTypeDomain,

            SidTypeAlias,

            SidTypeWellKnownGroup,

            SidTypeDeletedAccount,

            SidTypeInvalid,

            SidTypeUnknown,

            SidTypeComputer
        }

        [Flags]
        internal enum FileAccess : uint
        {
            AccessSystemSecurity = 0x1000000, // AccessSystemAcl access type

            MaximumAllowed = 0x2000000, // MaximumAllowed access type

            Delete = 0x10000,

            ReadControl = 0x20000,

            WriteDAC = 0x40000,

            WriteOwner = 0x80000,

            Synchronize = 0x100000,

            StandardRightsRequired = 0xF0000,

            StandardRightsRead = ReadControl,

            StandardRightsWrite = ReadControl,

            StandardRightsExecute = ReadControl,

            StandardRightsAll = 0x1F0000,

            SpecificRightsAll = 0xFFFF,

            FILE_READ_DATA = 0x0001, // file & pipe

            FILE_LIST_DIRECTORY = 0x0001, // directory

            FILE_WRITE_DATA = 0x0002, // file & pipe

            FILE_ADD_FILE = 0x0002, // directory

            FILE_APPEND_DATA = 0x0004, // file

            FILE_ADD_SUBDIRECTORY = 0x0004, // directory

            FILE_CREATE_PIPE_INSTANCE = 0x0004, // named pipe

            FILE_READ_EA = 0x0008, // file & directory

            FILE_WRITE_EA = 0x0010, // file & directory

            FILE_EXECUTE = 0x0020, // file

            FILE_TRAVERSE = 0x0020, // directory

            FILE_DELETE_CHILD = 0x0040, // directory

            FILE_READ_ATTRIBUTES = 0x0080, // all

            FILE_WRITE_ATTRIBUTES = 0x0100, // all

            GenericRead = 0x80000000,

            GenericWrite = 0x40000000,

            GenericExecute = 0x20000000,

            GenericAll = 0x10000000,

            SPECIFIC_RIGHTS_ALL = 0x00FFFF,

            FILE_ALL_ACCESS = StandardRightsRequired | Synchronize | 0x1FF,

            FILE_GENERIC_READ = StandardRightsRead | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | Synchronize,

            FILE_GENERIC_WRITE =
                StandardRightsWrite | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | FILE_APPEND_DATA
                | Synchronize,

            FILE_GENERIC_EXECUTE = StandardRightsExecute | FILE_READ_ATTRIBUTES | FILE_EXECUTE | Synchronize
        }

        #region File Flags and attributes
        public const short FILE_ATTRIBUTE_NORMAL = 0x80;

        public const uint GENERIC_READ = 0x80000000;

        public const uint GENERIC_WRITE = 0x40000000;

        public const uint CREATE_NEW = 1;

        public const uint CREATE_ALWAYS = 2;

        public const uint OPEN_EXISTING = 3;

        public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

        public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x4000000;

        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;

        public const uint FILE_FLAG_OPEN_NO_RECALL = 0x100000;

        public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x200000;

        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        public const uint FILE_FLAG_POSIX_SEMANTICS = 0x1000000;

        public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;

        public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000;

        public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        #endregion

        #region Window handling

        /// <summary>
        /// Window handles (HWND) used for hWndInsertAfter
        /// </summary>
        public static class HWND
        {
            public static IntPtr NoTopMost = new IntPtr(-2),
                                 TopMost = new IntPtr(-1),
                                 Top = new IntPtr(0),
                                 Bottom = new IntPtr(1);
        }

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public static class SWP
        {
            public static readonly int NOSIZE = 0x0001,
                                       NOMOVE = 0x0002,
                                       NOZORDER = 0x0004,
                                       NOREDRAW = 0x0008,
                                       NOACTIVATE = 0x0010,
                                       DRAWFRAME = 0x0020,
                                       FRAMECHANGED = 0x0020,
                                       SHOWWINDOW = 0x0040,
                                       HIDEWINDOW = 0x0080,
                                       NOCOPYBITS = 0x0100,
                                       NOOWNERZORDER = 0x0200,
                                       NOREPOSITION = 0x0200,
                                       NOSENDCHANGING = 0x0400,
                                       DEFERERASE = 0x2000,
                                       ASYNCWINDOWPOS = 0x4000;
        }

        /// <summary>
        /// Special window handles
        /// </summary>
        public enum SpecialWindowHandles
        {
            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Places the window at the top of the Z order.
            /// </summary>
            HWND_TOP = 0,
            /// <summary>
            ///     Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
            /// </summary>
            HWND_BOTTOM = 1,
            /// <summary>
            ///     Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
            /// </summary>
            HWND_TOPMOST = -1,
            /// <summary>
            ///     Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
            /// </summary>
            HWND_NOTOPMOST = -2
            // ReSharper restore InconsistentNaming
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            // ReSharper disable InconsistentNaming

            /// <summary>
            ///     If the calling thread and the thread that owns the window are attached to different input queues, 
            /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
            /// blocking its execution while other threads process the request.
            /// </summary>
            SWP_ASYNCWINDOWPOS = 0x4000,

            /// <summary>
            ///     Prevents generation of the WM_SYNCPAINT message.
            /// </summary>
            SWP_DEFERERASE = 0x2000,

            /// <summary>
            ///     Draws a frame (defined in the window's class description) around the window.
            /// </summary>
            SWP_DRAWFRAME = 0x0020,

            /// <summary>
            ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the 
            /// window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
            /// </summary>
            SWP_FRAMECHANGED = 0x0020,

            /// <summary>
            ///     Hides the window.
            /// </summary>
            SWP_HIDEWINDOW = 0x0080,

            /// <summary>
            ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost 
            /// or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOACTIVATE = 0x0010,

            /// <summary>
            ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area 
            /// are saved and copied back into the client area after the window is sized or repositioned.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            ///     Retains the current position (ignores X and Y parameters).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            ///     Does not change the owner window's position in the Z order.
            /// </summary>
            SWP_NOOWNERZORDER = 0x0200,

            /// <summary>
            ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. 
            /// This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered 
            /// as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window 
            /// and parent window that need redrawing.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            ///     Same as the SWP_NOOWNERZORDER flag.
            /// </summary>
            SWP_NOREPOSITION = 0x0200,

            /// <summary>
            ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
            /// </summary>
            SWP_NOSENDCHANGING = 0x0400,

            /// <summary>
            ///     Retains the current size (ignores the cx and cy parameters).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOZORDER = 0x0004,

            /// <summary>
            ///     Displays the window.
            /// </summary>
            SWP_SHOWWINDOW = 0x0040,

            // ReSharper restore InconsistentNaming
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        #endregion

        #endregion

        #region Winapi dll imports

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr CreateToolhelp32Snapshot([In] uint dwFlags, [In] uint th32ProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern bool Process32First([In] IntPtr hSnapshot, ref Processentry32 lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern bool Process32Next([In] IntPtr hSnapshot, ref Processentry32 lppe);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool GetTokenInformation(
            IntPtr hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr tokenInformation,
            int tokeInfoLength,
            ref int reqLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool ConvertSidToStringSid(
            IntPtr pSid,
            [In] [Out] [MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid);

        [DllImport("psapi.dll")]
        internal static extern uint GetModuleFileNameEx(
            IntPtr hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpBaseName,
            [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("psapi.dll")]
        internal static extern uint GetProcessImageFileName(
            IntPtr hProcess,
            [Out] StringBuilder lpImageFileName,
            [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern uint GetFinalPathNameByHandle(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string filename,
            [MarshalAs(UnmanagedType.U4)] uint access,
            [MarshalAs(UnmanagedType.U4)] FileShare share,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] uint creationDisposition,
            [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode,
             SetLastError = true)]
        internal static extern int GetFinalPathNameByHandleW(
            SafeFileHandle handle,
            [In] [Out] StringBuilder path,
            int bufLen,
            int flags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        #endregion

        #region Misc Constants

        public const short INVALID_HANDLE_VALUE = -1;

        private const int MAX_PATH_SIZE = 0x00000104 - 1;

        private const string DefaultInvalidPath = "N/A";

        #endregion

        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.SafeGetProcessId());
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
                var targetSize = (uint)Marshal.SizeOf(typeof(Processentry32));
                var processEntry = new Processentry32 { dwSize = targetSize };
                snapshotHandle = CreateToolhelp32Snapshot((uint)SnapshotFlags.Process, 0);

                if (Process32First(snapshotHandle, ref processEntry))
                {
                    do
                    {
                        if (processId == processEntry.th32ParentProcessID)
                        {
                            childProcesses.Add(Process.GetProcessById((int)processEntry.th32ProcessID));
                        }
                    }
                    while (Process32Next(snapshotHandle, ref processEntry));
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

        public static bool GetProcessUser(IntPtr processHandle, out string userSid)
        {
            userSid = null;
            IntPtr processToken = IntPtr.Zero;

            try
            {
                if (OpenProcessToken(processHandle, TokenQuery, out processToken))
                {
                    GetUserSidFromProcessToken(processToken, out userSid);
                }

                return Marshal.GetLastWin32Error() == 0;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                CloseHandle(processToken);
            }
        }

        private static bool GetUserSidFromProcessToken(IntPtr processToken, out string userSid)
        {
            const int BufLength = 256;
            var tokenInformation = Marshal.AllocHGlobal(BufLength);
            userSid = null;

            try
            {
                var tokenInformationLength = BufLength;
                var ret = GetTokenInformation(
                    processToken,
                    TOKEN_INFORMATION_CLASS.TokenUser,
                    tokenInformation,
                    tokenInformationLength,
                    ref tokenInformationLength);

                if (ret)
                {
                    var tokenUser = (TokenUser)Marshal.PtrToStructure(tokenInformation, typeof(TokenUser));
                    ConvertSidToStringSid(tokenUser.User.Sid, ref userSid);
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

        public static List<KeyValuePair<Process, string>> GetCurrentProcessesWIthUserSiDs()
        {
            var runningProcesses = Process.GetProcesses();
            var result = new List<KeyValuePair<Process, string>>();

            foreach (var process in runningProcesses)
            {
                string userSid = null;

                try
                {
                    GetProcessUser(process.Handle, out userSid);
                }
                catch (Exception ex)
                {
                    // Failure to obtain user SID indicates that the process is either in an
                    // invalid state or we have access denied, so we ignore it
                    if (ex is InvalidOperationException || ex is Win32Exception) continue;
                }

                if (userSid != null)
                {
                    result.Add(new KeyValuePair<Process, string>(process, userSid));
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
            var runningProcesses = GetCurrentProcessesWIthUserSiDs();
            var userSid = GetCurrentUserSid();

            return
                runningProcesses.Where(kv => kv.Value.Equals(userSid, StringComparison.InvariantCultureIgnoreCase))
                    .Select(kv => kv.Key)
                    .ToList();
        }

        public static string GetProcessFilePathEx(int processId)
        {
            var result = DefaultInvalidPath;
            var processHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(
                    ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.QueryInformation,
                    false,
                    processId);

                if (processHandle == IntPtr.Zero)
                {
                    return result;
                }

                var baseNameBuilder = new StringBuilder(MAX_PATH_SIZE);

                if (GetModuleFileNameEx(processHandle, IntPtr.Zero, baseNameBuilder, MAX_PATH_SIZE) > 0)
                {
                    result = baseNameBuilder.ToString();
                }
                else
                {
                    Debug.WriteLine(processHandle.ToString());
                }
            }
            finally
            {
                CloseHandle(processHandle);
            }

            return result;
        }

        public static string GetProcessFilePath(int processId)
        {
            StringBuilder imageFileName = new StringBuilder(MAX_PATH_SIZE);
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                processHandle = OpenProcess(ProcessAccessFlags.All, true, processId);

                GetProcessImageFileName(processHandle, imageFileName, MAX_PATH_SIZE);
            }
            finally
            {
                CloseHandle(processHandle);
            }

            return ConvertDevicePath(imageFileName.ToString());
        }

        private static string ConvertDevicePath(string filePath)
        {
            StringBuilder path = new StringBuilder(MAX_PATH_SIZE);
            if (string.IsNullOrWhiteSpace(filePath)) return DefaultInvalidPath;

            using (
                var safeFileHandle = CreateFileW(
                    filePath.Replace(@"Device\", @"\?\"),
                    (uint)FileAccess.FILE_GENERIC_READ,
                    FileShare.Read,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero))
            {
                if (safeFileHandle.IsInvalid) return DefaultInvalidPath;

                var result = GetFinalPathNameByHandleW(safeFileHandle, path, MAX_PATH_SIZE, 0x0);

                if ((result == 0) || (result > MAX_PATH_SIZE)) return DefaultInvalidPath;

                filePath = path.ToString().Replace(@"\\?\", string.Empty);
            }

            return filePath;
        }

        public static void ResizeWindowFromHandle(IntPtr windowHandle, int width, int heigth)
        {
            RECT lpRect;
            if (!GetWindowRect(windowHandle, out lpRect)) return;

            Rectangle windowRectangle = new Rectangle
                                   {
                                       X = lpRect.Left,
                                       Y = lpRect.Top,
                                       Width = lpRect.Right - lpRect.Left + 1,
                                       Height = lpRect.Bottom - lpRect.Top + 1
                                   };

            SetWindowPos(windowHandle, (IntPtr)SpecialWindowHandles.HWND_TOP, Screen.FromHandle(windowHandle).WorkingArea.Left,
                 Screen.FromHandle(windowHandle).WorkingArea.Top, CalculateResizeNumber(width, windowRectangle.Size.Width), CalculateResizeNumber(heigth, windowRectangle.Size.Height),
                 SetWindowPosFlags.SWP_NOACTIVATE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateResizeNumber(int width, int resizeWidth)
        {
            var widthExcess = width - resizeWidth;
            return resizeWidth + (widthExcess < 0 ? 0 : widthExcess);
        }
    }
}