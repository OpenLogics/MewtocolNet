using System;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet {

    internal static class AsyncExtensions {

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {

            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) {
                if (task != await Task.WhenAny(task, tcs.Task)) {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            if (task.IsCanceled) return default(T);

            return task.Result;

        }

    }

}
