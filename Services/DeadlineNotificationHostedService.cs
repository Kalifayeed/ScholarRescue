using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Background service that checks order deadlines every hour and sends reminder notifications.
    /// Generates reminders at 24, 12, 6, and 2 hours remaining.
    /// Prevents duplicate reminders using DeadlineReminder history table.
    /// </summary>
    public class DeadlineNotificationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeadlineNotificationHostedService> _logger;

        // The hour thresholds at which reminders are sent
        private static readonly int[] ReminderThresholds = { 24, 12, 6, 2 };

        public DeadlineNotificationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<DeadlineNotificationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DeadlineNotificationHostedService started.");

            // Initial delay to let the application warm up
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DeadlineNotificationHostedService cancelled during startup.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDeadlineRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing deadline reminders.");
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Run every hour
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("DeadlineNotificationHostedService stopped.");
        }

        /// <summary>
        /// Checks all active orders and sends deadline reminders where applicable.
        /// </summary>
        private async Task ProcessDeadlineRemindersAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ScholarRescueDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

            var now = DateTime.UtcNow;

            // Get all orders that are in progress and have a future deadline
            var activeOrders = await context.Orders
                .Where(o => o.Status == OrderStatus.InProgress
                         || o.Status == OrderStatus.DraftSubmitted
                         || o.Status == OrderStatus.RevisionRequested
                         || o.Status == OrderStatus.RevisionSubmitted)
                .Where(o => o.Deadline > now)
                .Select(o => new { o.Id, o.OrderNumber, o.Deadline, o.AssignedWriterId, o.ClientId })
                .ToListAsync(cancellationToken);

            if (activeOrders.Count == 0)
            {
                _logger.LogDebug("No active orders with future deadlines found.");
                return;
            }

            // Get admin users
            var admins = await userManager.GetUsersInRoleAsync(RoleNames.Administrator);

            foreach (var order in activeOrders)
            {
                var hoursRemaining = (order.Deadline - now).TotalHours;

                // Check each threshold
                foreach (var threshold in ReminderThresholds)
                {
                    // Send reminder when hours remaining crosses the threshold
                    // Eg: at 24h mark when hoursRemaining <= 24 and > 12
                    if (hoursRemaining <= threshold && hoursRemaining > (threshold == 2 ? 0 : threshold - 1))
                    {
                        // Check if reminder already sent for this threshold
                        var alreadySent = await context.DeadlineReminders
                            .AnyAsync(d => d.OrderId == order.Id
                                        && d.HoursRemaining == threshold, cancellationToken);

                        if (alreadySent)
                            continue;

                        // Notify assigned writer
                        if (!string.IsNullOrEmpty(order.AssignedWriterId))
                        {
                            await notificationService.NotifyDeadlineReminderAsync(
                                order.Id, order.AssignedWriterId, order.OrderNumber, threshold);

                            await RecordReminderAsync(context, order.Id, order.AssignedWriterId, threshold);
                        }

                        // Notify all admins
                        foreach (var admin in admins)
                        {
                            await notificationService.NotifyDeadlineReminderAsync(
                                order.Id, admin.Id, order.OrderNumber, threshold);

                            await RecordReminderAsync(context, order.Id, admin.Id, threshold);
                        }

                        _logger.LogInformation(
                            "Deadline reminder sent for Order {OrderNumber} ({Hours}h remaining)",
                            order.OrderNumber, threshold);
                    }
                }
            }
        }

        /// <summary>
        /// Records a reminder in the DeadlineReminders table to prevent duplicates.
        /// </summary>
        private async Task RecordReminderAsync(
            ScholarRescueDbContext context,
            int orderId,
            string userId,
            int hoursRemaining)
        {
            context.DeadlineReminders.Add(new DeadlineReminder
            {
                OrderId = orderId,
                UserId = userId,
                HoursRemaining = hoursRemaining,
                SentAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}
