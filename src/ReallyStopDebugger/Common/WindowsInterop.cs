using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace lggomez.ReallyStopDebugger.Common
{
    public static class WindowsInterop
    {
        #region Marshalling structs & consts
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

        //Win32Error codes
        const int NO_ERROR = 0x00000000;
        const int ERROR_ACCESS_DENIED = 0x00000005;
        const int ERROR_INVALID_HANDLE = 0x00000006;

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
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle,
            uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern bool GetTokenInformation(
            IntPtr hToken,
            TOKEN_INFORMATION_CLASS tokenInfoClass,
            IntPtr TokenInformation,
            int tokeInfoLength,
            ref int reqLength
        );

        [DllImport("kernel32")]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32", CharSet = CharSet.Auto)]
        static extern bool ConvertSidToStringSid(
            IntPtr pSID,
            [In, Out, MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid
        );
        #endregion

        public static bool GetProcessUser(IntPtr processHandle, out string userSID)
        {
            IntPtr processToken = IntPtr.Zero;
            bool ret = false;
            userSID = null;

            try
            {
                if (OpenProcessToken(processHandle, TOKEN_QUERY, out processToken))
                {
                    GetUserSidFromProcessToken(processToken, out userSID);
                    CloseHandle(processToken);
                }
                //else
                //{
                //    var err = Marshal.GetLastWin32Error();
                //}

                return ret;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool GetUserSidFromProcessToken(IntPtr processToken, out string userSID)
        {
            const int bufLength = 256;
            IntPtr tokenInformation = Marshal.AllocHGlobal(bufLength);
            bool ret = false;
            userSID = null;

            try
            {
                int tokenInformationLength = bufLength;
                ret = GetTokenInformation(processToken, TOKEN_INFORMATION_CLASS.TokenUser, tokenInformation, tokenInformationLength, ref tokenInformationLength);

                if (ret)
                {
                    TOKEN_USER tokenUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInformation , typeof(TOKEN_USER));
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
                    //Failure to obtain user SID indicates that the process is either in an
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
                return WindowsIdentity.GetCurrent().User.Value;
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
