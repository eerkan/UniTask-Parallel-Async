using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EmreErkanGames.UniTaskExtensions.Core.Concrete;
using UnityEngine;

public class Test : MonoBehaviour
{
    internal class Item
    {
        public int Index { get; set; }
        public string Value { get; set; }
    }
    
    private CancellationTokenSource _cancellationTokenSource;

    private void Start()
    {
        _cancellationTokenSource = new();
        _ = TestUniTaskParallelAsync();
    }

    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
           _cancellationTokenSource.Cancel();
    }

    private async UniTask TestUniTaskParallelAsync()
    {
        await TestUniTaskParallelForEachAsync();
        Debug.Log(string.Empty);
        await TestUniTaskParallelForAsync();
    }

    private async UniTask TestUniTaskParallelForEachAsync()
    {
        var squareOfItems = new ConcurrentBag<Item>();
        var parallelAsyncLoopResult = await UniTaskParallel.ForEachAsync(
            Enumerable.Range(0, 100).Select(x => new Item
            {
                Index = x,
                Value = $"Item {x}"
            }),
            async (item, loopState) =>
            {
                if (item.Index > 50)
                {
                    loopState.Break();
                    return;
                }
                squareOfItems.Add(new Item
                {
                    Index = item.Index,
                    Value = $"Item {item.Index * item.Index}"
                });
                Debug.Log($"Hey foreach: {item.Value}! Thread: {Thread.CurrentThread.ManagedThreadId}");
            },
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = _cancellationTokenSource.Token
            }
        );
        foreach(var item in squareOfItems)
            Debug.Log($"Square of {item.Index} = {item.Value}");
        if (parallelAsyncLoopResult.BreakItem != null)
            Debug.Log($"Foreach break item: {parallelAsyncLoopResult.BreakItem.Value}");
        else if (_cancellationTokenSource.IsCancellationRequested)
            Debug.Log("Foreach operation cancelled");
        else
            Debug.Log("Foreach operation completed");
    }

    private async UniTask TestUniTaskParallelForAsync()
    {
        var parallelAsyncLoopResult = await UniTaskParallel.ForAsync(
            0,
            1000000,
            async (item, loopState) =>
            {
                if (item > 200000)
                {
                    loopState.Break();
                    return;
                }
                Debug.Log($"Hey for: {item}! Thread: {Thread.CurrentThread.ManagedThreadId}");
            },
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 16,
                CancellationToken = _cancellationTokenSource.Token
            }
        );
        if (parallelAsyncLoopResult.BreakIndex != null)
            Debug.Log($"For break item: {parallelAsyncLoopResult.BreakIndex.Value}");
        else if(_cancellationTokenSource.IsCancellationRequested)
            Debug.Log("For operation cancelled");
        else 
            Debug.Log("For operation completed");
    }
}
