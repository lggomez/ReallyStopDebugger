using System;
using System.Runtime.InteropServices;

namespace ReallyStopDebugger.Native.SafeHandles
{
    [System.Security.SecurityCritical]
    internal sealed class SafeGDIObjectHandle : SafeBuffer
    {
        private SafeGDIObjectHandle() 
            : base(true)
        {
        }
        
        internal SafeGDIObjectHandle(IntPtr preexistingHandle)
            : base(true)
        {
            this.SetHandle(preexistingHandle);
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return WindowsInterop.DeleteObject(this.handle);
        }
    }
}