using MassTransit;
using FibaPlus_Bank.Events;
using FibaPlus_Bank.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FibaPlus_Bank.Consumers
{
    public class SystemLogConsumer : IConsumer<SystemLogEvent>
    {
       
        private readonly IServiceScopeFactory _scopeFactory;

        public SystemLogConsumer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task Consume(ConsumeContext<SystemLogEvent> context)
        {
            var logData = context.Message;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<FibraPlusBankDbContext>();

                var newLog = new SystemLog
                {
                    UserId = logData.UserId,
                    UserName = logData.UserName,
                    ActionType = logData.ActionType,
                    Description = logData.Description,
                    IpAddress = logData.IpAddress,
                    LogLevel = logData.LogLevel,
                    CreatedAt = logData.CreatedAt
                };

                dbContext.SystemLogs.Add(newLog);
                await dbContext.SaveChangesAsync();

                Console.WriteLine($"[LOG - RABBITMQ] {logData.ActionType}: {logData.Description}");
            }
        }
    }
}