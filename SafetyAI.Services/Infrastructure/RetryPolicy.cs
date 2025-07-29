using System;
using System.Threading.Tasks;

namespace SafetyAI.Services.Infrastructure
{
    public class RetryPolicy
    {
        private readonly int _retryCount;
        private readonly TimeSpan _delay;
        private readonly double _backoffMultiplier;

        public RetryPolicy(int retryCount, TimeSpan delay, double backoffMultiplier = 2.0)
        {
            _retryCount = retryCount;
            _delay = delay;
            _backoffMultiplier = backoffMultiplier;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= _retryCount; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt))
                {
                    lastException = ex;
                    
                    if (attempt < _retryCount)
                    {
                        var delayTime = TimeSpan.FromMilliseconds(_delay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));
                        await Task.Delay(delayTime);
                    }
                }
            }
            
            throw lastException ?? new InvalidOperationException("Retry policy failed without exception");
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            });
        }

        private bool ShouldRetry(Exception exception, int attempt)
        {
            if (attempt >= _retryCount)
                return false;

            // Retry on specific exceptions
            return exception is System.Net.Http.HttpRequestException ||
                   exception is TaskCanceledException ||
                   exception is TimeoutException ||
                   (exception.Message?.Contains("timeout") == true) ||
                   (exception.Message?.Contains("503") == true) ||
                   (exception.Message?.Contains("502") == true) ||
                   (exception.Message?.Contains("429") == true);
        }
    }

    public static class RetryPolicyExtensions
    {
        public static async Task<T> WithRetryAsync<T>(this Task<T> task, RetryPolicy retryPolicy)
        {
            return await retryPolicy.ExecuteAsync(() => task);
        }

        public static async Task WithRetryAsync(this Task task, RetryPolicy retryPolicy)
        {
            await retryPolicy.ExecuteAsync(() => task);
        }
    }
}