using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;
        private readonly string? _twilioAccountSid;
        private readonly string? _twilioAuthToken;
        private readonly string? _twilioFromNumber;

        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // In a real implementation, these would be configured in appsettings
            _twilioAccountSid = _configuration.GetValue<string>("Twilio:AccountSid");
            _twilioAuthToken = _configuration.GetValue<string>("Twilio:AuthToken");
            _twilioFromNumber = _configuration.GetValue<string>("Twilio:FromNumber");
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                _logger.LogInformation("Sending SMS to {PhoneNumber}: {Message}",
                    MaskPhoneNumber(phoneNumber), message);

                // In a real implementation, this would use Twilio API:
                /*
                var client = new TwilioRestClient(_twilioAccountSid, _twilioAuthToken);
                var messageOptions = new CreateMessageOptions(new PhoneNumber(phoneNumber))
                {
                    From = new PhoneNumber(_twilioFromNumber),
                    Body = message
                };

                var result = await MessageResource.CreateAsync(messageOptions, client);
                _logger.LogInformation("SMS sent successfully. SID: {MessageSid}", result.Sid);
                */

                // For demo purposes, simulate SMS sending
                await Task.Delay(500); // Simulate API call delay
                _logger.LogInformation("SMS sent successfully (simulated) to {PhoneNumber}",
                    MaskPhoneNumber(phoneNumber));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
                throw;
            }
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                _logger.LogInformation("Sending email to {Email}: {Subject}", MaskEmail(email), subject);

                // In a real implementation, this would use SendGrid, Azure Communication Services, etc.
                // For demo purposes, simulate email sending
                await Task.Delay(1000);
                _logger.LogInformation("Email sent successfully (simulated) to {Email}", MaskEmail(email));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", MaskEmail(email));
                throw;
            }
        }

        public async Task<bool> SendETAUpdateAsync(string customerPhone, string technicianName, double etaMinutes)
        {
            try
            {
                if (string.IsNullOrEmpty(customerPhone))
                {
                    _logger.LogWarning("Cannot send ETA update - customer phone number is empty");
                    return false;
                }

                var message = FormatETAMessage(technicianName, etaMinutes);
                await SendSmsAsync(customerPhone, message);

                _logger.LogInformation("ETA update sent to customer {Phone}: Technician {Technician}, ETA {ETA} minutes",
                    MaskPhoneNumber(customerPhone), technicianName, etaMinutes);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send ETA update to {Phone}", MaskPhoneNumber(customerPhone));
                return false;
            }
        }

        public async Task<bool> SendEmergencyDiversionNotificationAsync(NotificationAction notification)
        {
            try
            {
                switch (notification.Type.ToUpper())
                {
                    case "SMS":
                        await SendSmsAsync(notification.Recipient, notification.Message);
                        break;
                    case "EMAIL":
                        await SendEmailAsync(notification.Recipient, "Emergency Service Update", notification.Message);
                        break;
                    case "CALL":
                        await MakePhoneCallAsync(notification.Recipient, notification.Message);
                        break;
                    default:
                        _logger.LogWarning("Unknown notification type: {Type}", notification.Type);
                        return false;
                }

                _logger.LogInformation("Emergency notification sent successfully: {Type} to {Recipient}",
                    notification.Type, MaskContact(notification.Recipient));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send emergency notification: {Type} to {Recipient}",
                    notification.Type, MaskContact(notification.Recipient));
                return false;
            }
        }

        public async Task<bool> SendTechnicianAssignmentAsync(string technicianPhone, JobRequest job, double etaMinutes)
        {
            try
            {
                var urgencyText = job.IsEmergency ? "🚨 EMERGENCY" : job.Priority.ToUpper();
                var message = $"{urgencyText} JOB ASSIGNMENT\n" +
                             $"Job: {job.JobId}\n" +
                             $"Service: {job.ServiceType}\n" +
                             $"Location: {job.Location}\n" +
                             $"ETA: {etaMinutes:F0} minutes\n" +
                             $"Priority: {job.Priority}\n" +
                             $"Customer: {job.CustomerName}";

                await SendSmsAsync(technicianPhone, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send technician assignment");
                return false;
            }
        }

        public async Task<bool> SendCustomerConfirmationAsync(string customerPhone, string technicianName,
            JobRequest job, double etaMinutes)
        {
            try
            {
                var urgencyText = job.IsEmergency ? "🚨 EMERGENCY SERVICE" : "Service";
                var message = $"{urgencyText} CONFIRMED\n" +
                             $"Technician: {technicianName}\n" +
                             $"Job: {job.JobId}\n" +
                             $"ETA: {etaMinutes:F0} minutes\n" +
                             $"We'll keep you updated with arrival time.";

                await SendSmsAsync(customerPhone, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send customer confirmation");
                return false;
            }
        }

        public async Task<bool> SendStatusUpdateAsync(string phoneNumber, string technicianName,
            string status, double? etaMinutes = null)
        {
            try
            {
                var message = status.ToLower() switch
                {
                    "enroute" => $"📍 UPDATE: {technicianName} is on the way!" +
                                (etaMinutes.HasValue ? $" ETA: {etaMinutes:F0} minutes" : ""),
                    "arrived" => $"✅ ARRIVED: {technicianName} has arrived at your location.",
                    "working" => $"🔧 WORKING: {technicianName} has started work on your service request.",
                    "completed" => $"✅ COMPLETED: Service has been completed by {technicianName}.",
                    "delayed" => $"⏰ DELAY: {technicianName} is running late." +
                                (etaMinutes.HasValue ? $" New ETA: {etaMinutes:F0} minutes" : ""),
                    _ => $"📋 UPDATE: {status} - {technicianName}"
                };

                await SendSmsAsync(phoneNumber, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status update");
                return false;
            }
        }

        private async Task MakePhoneCallAsync(string phoneNumber, string message)
        {
            // In a real implementation, this would use Twilio Voice API
            _logger.LogInformation("Making phone call (simulated) to {Phone}: {Message}",
                MaskPhoneNumber(phoneNumber), message);
            await Task.Delay(2000); // Simulate call duration
        }

        private string FormatETAMessage(string technicianName, double etaMinutes)
        {
            if (etaMinutes <= 5)
                return $"📍 {technicianName} is almost there! ETA: {etaMinutes:F0} minutes";
            else if (etaMinutes <= 15)
                return $"📍 {technicianName} is on the way. ETA: {etaMinutes:F0} minutes";
            else if (etaMinutes <= 30)
                return $"📍 UPDATE: {technicianName} ETA: {etaMinutes:F0} minutes";
            else
                return $"⏰ {technicianName} is experiencing delays. New ETA: {etaMinutes:F0} minutes. Thank you for your patience.";
        }

        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
                return "***";

            return phoneNumber.Substring(0, 3) + "***" + phoneNumber.Substring(phoneNumber.Length - 2);
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return "***";

            var parts = email.Split('@');
            if (parts[0].Length <= 2)
                return "***@" + parts[1];

            return parts[0].Substring(0, 2) + "***@" + parts[1];
        }

        private string MaskContact(string contact)
        {
            return contact.Contains('@') ? MaskEmail(contact) : MaskPhoneNumber(contact);
        }
    }
}