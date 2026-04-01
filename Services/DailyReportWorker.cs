using CMS.Models;
using CMS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CMS.Services
{
    public class DailyReportWorker : BackgroundService
    {
        private readonly ILogger<DailyReportWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailSettings _emailSettings;

        public DailyReportWorker(
            ILogger<DailyReportWorker> logger,
            IServiceProvider serviceProvider,
            IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _emailSettings = emailSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Daily Report Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRunTime = now.Date.AddHours(9); // 9 AM today

                if (now >= nextRunTime)
                {
                    nextRunTime = nextRunTime.AddDays(1); // 9 AM tomorrow
                }

                var delay = nextRunTime - now;
                _logger.LogInformation("Next report scheduled for {nextRunTime}. Delaying for {delay}.", nextRunTime, delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);

                    // Generate and send the report
                    await SendDailyReportAsync();
                }
                catch (TaskCanceledException)
                {
                    // Shutdown requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while sending the daily report.");
                    // Wait a bit before retrying if there was an error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task SendDailyReportAsync()
        {
            _logger.LogInformation("Generating daily report at {time}.", DateTime.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();

                try 
                {
                    var (body, csvData, fileName) = await reportingService.GenerateDailyReportAsync();
                    await emailService.SendEmailWithAttachmentAsync(_emailSettings.AdminEmail, $"CCMS Daily Status Report - {DateTime.Now:dd/MM/yyyy}", body, csvData, fileName);
                    _logger.LogInformation("Daily report with attachment sent successfully to {email}.", _emailSettings.AdminEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate or send daily report.");
                }
            }
        }
    }
}


