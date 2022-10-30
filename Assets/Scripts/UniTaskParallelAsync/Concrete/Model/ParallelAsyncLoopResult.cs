using EmreErkanGames.UniTaskExtensions.Model.Abstract;

namespace EmreErkanGames.UniTaskExtensions.Model.Concrete
{
    public class ParallelAsyncLoopResult<T> : IParallelAsyncLoopResult<T>
    {
        public bool IsCompleted { get; set; }
        public T BreakItem { get; set; }
        public long? BreakIndex { get; set; }
        public bool IsBreakItem { get; set; }
        public bool HasBreak => IsBreakItem ? BreakItem != null : BreakIndex.HasValue;
    }
}
