using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Events.External
{
    //See more: https://www.stevejgordon.co.uk/asp-net-core-2-ihostedservice
    public class ExternalRabbitMqEventConsumerBackgroundWorker : IHostedService
    {
        private Task executingTask;
        private CancellationTokenSource cts;
        private readonly IExternalRabbitMqEventConsumer externalEventConsumer;
        private readonly ILogger<ExternalRabbitMqEventConsumerBackgroundWorker> logger;

        public ExternalRabbitMqEventConsumerBackgroundWorker(
            IExternalRabbitMqEventConsumer externalEventConsumer,
            ILogger<ExternalRabbitMqEventConsumerBackgroundWorker> logger
        )
        {
            this.externalEventConsumer = externalEventConsumer ?? throw new ArgumentNullException(nameof(externalEventConsumer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("External Event Consumer started");

            // Create a linked token so we can trigger cancellation outside of this token's cancellation
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            executingTask = externalEventConsumer.StartAsync(cancellationToken);

            // If the task is completed then return it, otherwise it's running
            return executingTask.IsCompleted ? executingTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (executingTask == null)
            {
                return;
            }

            // Signal cancellation to the executing method
            cts.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(executingTask, Task.Delay(-1, cancellationToken));

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("External Event Consumer stopped");
        }
    }
}
