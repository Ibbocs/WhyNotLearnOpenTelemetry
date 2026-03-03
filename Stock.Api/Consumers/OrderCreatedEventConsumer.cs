using System.Diagnostics;
using System.Text.Json;
using Common.Shared.Events;
using MassTransit;

namespace Stock.API.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        Thread.Sleep(2000);
        
        Activity.Current?.SetTag("message.body", JsonSerializer.Serialize(context.Message));
        //context.Headers.
        return Task.CompletedTask;
    }
}