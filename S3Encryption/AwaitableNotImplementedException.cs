using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace S3Encryption
{
    class AwaitableNotImplementedException<TResult> : NotImplementedException
    {
        public AwaitableNotImplementedException() { }

        public AwaitableNotImplementedException(string message) : base(message) { }

        // This method makes the constructor awaitable.
        public TaskAwaiter<AwaitableNotImplementedException<TResult>> GetAwaiter()
        {
            throw this;
        }
    }
}
