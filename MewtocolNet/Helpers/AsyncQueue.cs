using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MewtocolNet.Queue {

    internal class AsyncQueue {

        readonly object _locker = new object();
        readonly WeakReference<Task> _lastTask = new WeakReference<Task>(null);

        internal Task<T> Enqueue<T>(Func<Task<T>> asyncFunction) {
            lock (_locker) {

                Task lastTask;
                Task<T> resultTask;

                if (_lastTask.TryGetTarget(out lastTask)) {
                    resultTask = lastTask.ContinueWith(_ => asyncFunction(), TaskContinuationOptions.ExecuteSynchronously).Unwrap();
                } else {
                    resultTask = Task.Run(asyncFunction);
                }

                _lastTask.SetTarget(resultTask);

                return resultTask;

            }
        }

    }

}
