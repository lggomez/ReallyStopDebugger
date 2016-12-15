// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

using System;

namespace ReallyStopDebugger.Common
{
    public class ProcessOperationException : Exception
    {
        public ProcessOperationResults ResultCode { get; private set; }

        public Exception InnerProcessException { get; }

        public string Message { get; }

        public bool IsFaulted => this.InnerProcessException != null;

        public ProcessOperationException(ProcessOperationResults resultCode, Exception innerException, string message = "")
        {
            this.ResultCode = resultCode;
            this.InnerProcessException = innerException;
        }

        public override string ToString()
        {
            return this.InnerProcessException.ToString();
        }
    }
}