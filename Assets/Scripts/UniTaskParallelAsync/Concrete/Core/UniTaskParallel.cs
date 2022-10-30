using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EmreErkanGames.UniTaskExtensions.Model.Abstract;
using EmreErkanGames.UniTaskExtensions.Model.Concrete;

namespace EmreErkanGames.UniTaskExtensions.Core.Concrete
{
    public static class UniTaskParallel
    {
        private static ParallelOptions CreateDefaultParallelOptions()
        {
            return new ParallelOptions
            {
                CancellationToken = new CancellationTokenSource().Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2
            };
        }

        public static async UniTask<IParallelAsyncLoopResult<T>> ForEachAsync<T>(IEnumerable<T> source, Func<T, IParallelAsyncLoopState, UniTask> body, ParallelOptions parallelOptions)
        {
            var parallelAsyncLoopResult = new ParallelAsyncLoopResult<T>
            {
                IsCompleted = false,
                IsBreakItem = true
            };
            var disposed = 0L;
            //var disposeLock = new object();
            var parallelCancellationTokenSource = new CancellationTokenSource();
            var completeSemphoreSlim = new SemaphoreSlim(1);
            var taskCountLimitsemaphoreSlim = new SemaphoreSlim(parallelOptions.MaxDegreeOfParallelism);
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parallelOptions.CancellationToken,
                    parallelCancellationTokenSource.Token);
            var parallelAsyncLoopState = new ParallelAsyncLoopState(parallelCancellationTokenSource);
            await completeSemphoreSlim.WaitAsync(cancellationTokenSource.Token);
            var runningTaskCount = source.Count();
            if (runningTaskCount == 0)
            {
                completeSemphoreSlim.Release();
                taskCountLimitsemaphoreSlim.Release();
            }
            foreach (var item in source)
            {
                try
                {
                    await taskCountLimitsemaphoreSlim.WaitAsync(cancellationTokenSource.Token);
                }
                catch (Exception)
                {
                    // ignored
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    break;
                var item2 = item;
                _ = UniTask.RunOnThreadPool(async () =>
                {
                    try
                    {
                        await body.Invoke(item2, parallelAsyncLoopState);
                        if (parallelCancellationTokenSource.Token.IsCancellationRequested)
                            parallelAsyncLoopResult.BreakItem = item2;
                    }
                    finally
                    {
                        //lock (disposeLock)
                        //{
                            if (Interlocked.Read(ref disposed) == 0)
                            {
                                taskCountLimitsemaphoreSlim.Release();
                                Interlocked.Decrement(ref runningTaskCount);
                                if (Interlocked.CompareExchange(ref runningTaskCount, -1, 0) == 0)
                                    completeSemphoreSlim.Release();
                            }
                        //}
                    }
                }, false, cancellationToken: cancellationTokenSource.Token);
            }
            try
            {
                await completeSemphoreSlim.WaitAsync(cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                // ignored
            }
            parallelAsyncLoopResult.IsCompleted = !parallelCancellationTokenSource.IsCancellationRequested;
            //lock (disposeLock)
            //{
                Interlocked.Increment(ref disposed);
                taskCountLimitsemaphoreSlim.Dispose();
                completeSemphoreSlim.Dispose();
                parallelCancellationTokenSource.Dispose();
                cancellationTokenSource.Dispose();
            //}
            return parallelAsyncLoopResult;
        }

        public static async UniTask<IParallelAsyncLoopResult<T>> ForEachAsync<T>(IEnumerable<T> source, Func<T, IParallelAsyncLoopState, UniTask> body)
        {
            var parallelOptions = CreateDefaultParallelOptions();
            return await ForEachAsync(source, body, parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<T>> ForEachAsync<T>(IEnumerable<T> source, Func<T, UniTask> body, ParallelOptions parallelOptions)
        {
            return await ForEachAsync(source, async (item, breakCancellationSource) => await body.Invoke(item), parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<T>> ForEachAsync<T>(IEnumerable<T> source, Func<T, UniTask> body)
        {
            var parallelOptions = CreateDefaultParallelOptions();
            return await ForEachAsync(source, body, parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<long>> ForAsync(long fromInclusive, long toExclude, Func<long, IParallelAsyncLoopState, UniTask> body, ParallelOptions parallelOptions)
        {
            var parallelAsyncLoopResult = new ParallelAsyncLoopResult<long>
            {
                IsCompleted = false,
                IsBreakItem = false
            };
            var disposed = 0L;
            //var disposeLock = new object();
            var parallelCancellationTokenSource = new CancellationTokenSource();
            var completeSemphoreSlim = new SemaphoreSlim(1);
            var taskCountLimitsemaphoreSlim = new SemaphoreSlim(parallelOptions.MaxDegreeOfParallelism);
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parallelOptions.CancellationToken,
                parallelCancellationTokenSource.Token);
            var parallelAsyncLoopState = new ParallelAsyncLoopState(parallelCancellationTokenSource);
            await completeSemphoreSlim.WaitAsync(cancellationTokenSource.Token);
            var runningTaskCount = toExclude - fromInclusive;
            if (runningTaskCount == 0)
            {
                completeSemphoreSlim.Release();
                taskCountLimitsemaphoreSlim.Release();
            }
            for (var index = fromInclusive; index < toExclude; index++)
            {
                try
                {
                    await taskCountLimitsemaphoreSlim.WaitAsync(cancellationTokenSource.Token);
                }
                catch (Exception)
                {
                    // ignored
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    break;
                var item2 = index;
                _ = UniTask.RunOnThreadPool(async () =>
                {
                    try
                    {
                        await body.Invoke(item2, parallelAsyncLoopState);
                        if (parallelCancellationTokenSource.IsCancellationRequested)
                            parallelAsyncLoopResult.BreakIndex = item2;
                    }
                    finally
                    {
                        //lock (disposeLock)
                        //{
                            if (Interlocked.Read(ref disposed) == 0)
                            {
                                taskCountLimitsemaphoreSlim.Release();
                                Interlocked.Decrement(ref runningTaskCount);
                                if (Interlocked.CompareExchange(ref runningTaskCount, -1, 0) == 0)
                                    completeSemphoreSlim?.Release();
                            }
                        //}
                    }
                }, false, cancellationToken: cancellationTokenSource.Token);
            }
            try
            {
                await completeSemphoreSlim.WaitAsync(cancellationTokenSource.Token);
            }
            catch (Exception)
            {
                // ignored
            }
            parallelAsyncLoopResult.IsCompleted = !parallelCancellationTokenSource.IsCancellationRequested;
            //lock (disposeLock)
            //{
                Interlocked.Increment(ref disposed);
                taskCountLimitsemaphoreSlim.Dispose();
                completeSemphoreSlim.Dispose();
                parallelCancellationTokenSource.Dispose();
                cancellationTokenSource.Dispose();
            //}
            return parallelAsyncLoopResult;
        }

        public static async UniTask<IParallelAsyncLoopResult<long>> ForAsync(long fromInclusive, long toExclude, Func<long, IParallelAsyncLoopState, UniTask> body)
        {
            var parallelOptions = CreateDefaultParallelOptions();
            return await ForAsync(fromInclusive, toExclude, body, parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<long>> ForAsync(long fromInclusive, long toExclude, Func<long, UniTask> body, ParallelOptions parallelOptions)
        {
            return await ForAsync(fromInclusive, toExclude, async (item, breakCancellationSource) => await body.Invoke(item), parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<long>> ForAsync(long fromInclusive, long toExclude, Func<long, UniTask> body)
        {
            var parallelOptions = CreateDefaultParallelOptions();
            return await ForAsync(fromInclusive, toExclude, body, parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<int>> ForAsync(int fromInclusive, int toExclude,
            Func<int, IParallelAsyncLoopState, UniTask> body, ParallelOptions parallelOptions)
        {
            var parallelAsyncLoopResult = new ParallelAsyncLoopResult<int>
            {
                IsCompleted = false,
                IsBreakItem = false
            };
            var parallelAsyncLoopResultLong = await ForAsync((long)fromInclusive, (long)toExclude, async (index, breakCancellationSource) => await body.Invoke((int)index, breakCancellationSource), parallelOptions);
            if (parallelAsyncLoopResultLong?.BreakItem != null)
                parallelAsyncLoopResult.BreakItem = (int)parallelAsyncLoopResultLong.BreakItem;
            if (parallelAsyncLoopResultLong != null)
                parallelAsyncLoopResult.IsCompleted = parallelAsyncLoopResultLong.IsCompleted;
            return parallelAsyncLoopResult;
        }

        public static async UniTask<IParallelAsyncLoopResult<int>> ForAsync(int fromInclusive, int toExclude,
            Func<int, IParallelAsyncLoopState, UniTask> body)
        {
            var parallelOptions = CreateDefaultParallelOptions();
            return await ForAsync(fromInclusive, toExclude, body, parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<int>> ForAsync(int fromInclusive, int toExclude,
            Func<int, UniTask> body, ParallelOptions parallelOptions)
        {
            return await ForAsync(fromInclusive, toExclude, async (item, breakCancellationSource) => await body.Invoke(item), parallelOptions);
        }

        public static async UniTask<IParallelAsyncLoopResult<int>> ForAsync(int fromInclusive, int toExclude,
            Func<int, UniTask> body)
        {
            var parallelOptions = CreateDefaultParallelOptions();
            return await ForAsync(fromInclusive, toExclude, body, parallelOptions);
        }
    }
}