namespace OutboxProcessorWorker.Outbox;

public static partial class OutboxLoggers
{
    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService starting...")]
    internal static partial void LogStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting iteration {IterationCount}")]
    internal static partial void LogStartingIteration(ILogger logger, int iterationCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Iteration {IterationCount} completed. Processed {ProcessedMessages:N0} messages. Total processed: {TotalProcessedMessages:N0}")]
    internal static partial void LogIterationCompleted(ILogger logger, int iterationCount, int processedMessages, int totalProcessedMessages);

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService operation cancelled.")]
    internal static partial void LogOperationCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred in OutboxBackgroundService")]
    internal static partial void LogError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxBackgroundService finished. Total time: {TotalTime:N0} ms. Total iterations: {IterationCount}, Total processed messages: {TotalProcessedMessages:N0}")]
    internal static partial void LogFinished(ILogger logger, double totalTime, int iterationCount, int totalProcessedMessages);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox processing completed. Total time: {TotalTime:N0} ms, Query time: {QueryTime:N0} ms, Publish time: {PublishTime:N0} ms, Update time: {UpdateTime:N0} ms, Messages processed: {MessageCount:N0}")]
    internal static partial void LogProcessingPerformance(ILogger logger, double totalTime, double queryTime, double publishTime, double updateTime, int messageCount);
}
