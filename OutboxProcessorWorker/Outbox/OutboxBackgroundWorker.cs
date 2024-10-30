using System.Diagnostics;

namespace OutboxProcessorWorker.Outbox;

public sealed class OutboxBackgroundWorker(
    IServiceScopeFactory _serviceScopeFactory,
    ILogger<OutboxBackgroundWorker> _logger,
    IHostApplicationLifetime _hostApplicationLifetime) : BackgroundService
{
    private int _totalIterations, _totalProcessedMessage;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxLoggers.LogStarting(_logger);

        using var cts       = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

        long totalStartingTimestamp = Stopwatch.GetTimestamp();

        try
        {
            while (!linkedCts.IsCancellationRequested)
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();

                var outboxProcessor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

                int iterationCount = Interlocked.Increment(ref _totalIterations);

                OutboxLoggers.LogStartingIteration(_logger, iterationCount);

                int processedMessages = await outboxProcessor.Execute(linkedCts.Token);

                int totalProcessedMessages = Interlocked.Add(ref _totalProcessedMessage, processedMessages);

                OutboxLoggers.LogIterationCompleted(_logger, iterationCount, processedMessages, totalProcessedMessages);

                if (processedMessages == 0)
                {
                    _hostApplicationLifetime.StopApplication();
                }
            }
        }
        catch (OperationCanceledException)
        {
            OutboxLoggers.LogOperationCancelled(_logger);
        }
        catch (Exception ex)
        {
            OutboxLoggers.LogError(_logger, ex);
        }
        finally
        {
            double totalTime = Stopwatch.GetElapsedTime(totalStartingTimestamp).TotalMilliseconds;

            OutboxLoggers.LogFinished(_logger, totalTime, _totalIterations, _totalProcessedMessage);
        }
    }
}
