// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace ReallyStopDebugger.Native.SafeHandles
{
    [System.Security.SecurityCritical]
    public class SafeHGlobalHandle : SafeBuffer
    {
        private SafeHGlobalHandle()
            : base(true)
        {
        }

        internal SafeHGlobalHandle(IntPtr preexistingHandle)
            : base(true)
        {
            this.SetHandle(preexistingHandle);
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            // Marshal.FreeHGlobal wraps LocalFree, so we use the direct call to validate
            return WindowsInterop.LocalFree(this.handle) == IntPtr.Zero;
        }
    }
}