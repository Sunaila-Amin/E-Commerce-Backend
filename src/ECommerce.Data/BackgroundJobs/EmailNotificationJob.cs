using ECommerce.Business.Abstractions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Data.BackgroundJobs;

public class EmailNotificationJob
{
    private readonly ILogger<EmailNotificationJob> _logger;

    public EmailNotificationJob(ILogger<EmailNotificationJob> logger)
    {
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(int orderId, string userEmail, CancellationToken cancellationToken)
    {
        // In production this would call an SMTP / transactional email provider.
        _logger.LogInformation(
            "EmailNotification: sending order confirmation for order {OrderId} to {Email}.",
            orderId,
            userEmail);

        await Task.CompletedTask;
    }
}
