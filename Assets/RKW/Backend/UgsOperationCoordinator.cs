using System;
using System.Threading;
using System.Threading.Tasks;

namespace RKW.Backend
{
    internal static class UgsOperationCoordinator
    {
        internal static async Task WaitAsync(
            Task operation,
            TimeSpan timeout,
            CancellationToken cancellationToken,
            string operationName)
        {
            await WaitAsync(WaitForCompletionAsync(operation), timeout, cancellationToken, operationName);
        }

        internal static async Task<T> WaitAsync<T>(
            Task<T> operation,
            TimeSpan timeout,
            CancellationToken cancellationToken,
            string operationName)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            cancellationToken.ThrowIfCancellationRequested();

            using var timeoutCancellation = new CancellationTokenSource();
            timeoutCancellation.CancelAfter(timeout);
            using var completionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCancellation.Token);
            var cancellationSignal = Task.Delay(Timeout.Infinite, completionCancellation.Token);

            await Task.WhenAny(operation, cancellationSignal);

            if (operation.IsCompleted)
            {
                completionCancellation.Cancel();
                return await operation;
            }

            ObserveLateCompletion(operation);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            throw new TimeoutException($"{operationName} exceeded its configured timeout of {timeout}.");
        }

        private static async Task<bool> WaitForCompletionAsync(Task operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            await operation;
            return true;
        }

        private static void ObserveLateCompletion(Task operation)
        {
            _ = operation.ContinueWith(
                completed =>
                {
                    _ = completed.Exception;
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
