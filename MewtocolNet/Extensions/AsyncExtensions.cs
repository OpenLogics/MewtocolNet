﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace MewtocolNet {

    internal static class AsyncExtensions {

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {

            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) {
                if (task != await Task.WhenAny(task, tcs.Task)) {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return task.Result;

        }

    }

}
