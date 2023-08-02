using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MewtocolNet.Helpers {

    internal class AsyncQueue {

        readonly object _locker = new object();
        readonly WeakReference<Task> _lastTask = new WeakReference<Task>(null);


        private List<Task> queuedTasks = new List<Task>();

        //internal Task<T> Enqueue<T>(Func<Task<T>> asyncFunction) {

        //    lock (_locker) {

        //        var token = tSource.Token;

        //        Task lastTask;
        //        Task<T> resultTask;

        //        if (_lastTask.TryGetTarget(out lastTask)) {
        //            resultTask = lastTask.ContinueWith(_ => asyncFunction(), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current).Unwrap();
        //        } else {
        //            resultTask = Task.Run(asyncFunction, token);
        //        }

        //        _lastTask.SetTarget(resultTask);

        //        return resultTask;

        //    }

        //}

    }

}
