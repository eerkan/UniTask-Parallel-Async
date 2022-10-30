using System.Threading;
using EmreErkanGames.UniTaskExtensions.Model.Abstract;

namespace EmreErkanGames.UniTaskExtensions.Model.Concrete
{
    public class ParallelAsyncLoopState : IParallelAsyncLoopState
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ParallelAsyncLoopState(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void Break()
        {
            if(_cancellationTokenSource.Token.CanBeCanceled)
                _cancellationTokenSource.Cancel();
        }
    }
}
