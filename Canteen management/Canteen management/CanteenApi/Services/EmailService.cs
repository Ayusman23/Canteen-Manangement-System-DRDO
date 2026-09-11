using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace CanteenApi.Services;

public interface IEmailService
{
    Task SendBookingConfirmationAsync(string recipientEmail, string recipientName, string tokenRef, string mealName, string mealType, DateOnly mealDate, decimal price, string qrHash);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendBookingConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string tokenRef,
        string mealName,
        string mealType,
        DateOnly mealDate,
        decimal price,
        string qrHash)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var subject = $"[DRDO Canteen] Meal Token Confirmed: {tokenRef}";
        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0b0f19; color: #e2e8f0; margin: 0; padding: 20px; }}
    .card {{ max-width: 560px; margin: 0 auto; background: #131b2e; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }}
    .header {{ background: linear-gradient(135deg, #1e3a8a, #0f172a); padding: 24px; text-align: center; border-bottom: 2px solid #3b82f6; }}
    .header h1 {{ margin: 0; font-size: 20px; color: #f8fafc; letter-spacing: 1px; }}
    .badge {{ display: inline-block; background: #2563eb; color: #ffffff; padding: 4px 12px; border-radius: 9999px; font-size: 11px; font-weight: 700; margin-top: 8px; text-transform: uppercase; }}
    .content {{ padding: 28px; }}
    .token-box {{ background: #0f172a; border: 2px dashed #3b82f6; border-radius: 8px; padding: 18px; text-align: center; margin: 20px 0; }}
    .token-title {{ font-size: 12px; text-transform: uppercase; color: #94a3b8; letter-spacing: 1.5px; }}
    .token-code {{ font-size: 28px; font-weight: 800; color: #60a5fa; letter-spacing: 2px; margin: 6px 0; font-family: monospace; }}
    .info-row {{ display: flex; justify-content: space-between; border-bottom: 1px solid #1e293b; padding: 10px 0; font-size: 14px; }}
    .label {{ color: #94a3b8; }}
    .value {{ font-weight: 600; color: #f1f5f9; }}
    .footer {{ padding: 20px; text-align: center; font-size: 11px; color: #64748b; background: #0a0f1d; border-top: 1px solid #1e293b; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <h1>DEFENCE RESEARCH &amp; DEVELOPMENT ORGANISATION</h1>
      <span class='badge'>Canteen Management Secure Token</span>
    </div>
    <div class='content'>
      <p>Dear <strong>{WebUtility.HtmlEncode(recipientName)}</strong>,</p>
      <p>Your meal reservation has been successfully confirmed and registered in the DRDO Central Logistics Dining Registry.</p>
      
      <div class='token-box'>
        <div class='token-title'>Official Meal Redemption Token</div>
        <div class='token-code'>{tokenRef}</div>
        <div style='font-size: 12px; color: #94a3b8;'>Present this token or your QR pass at the kitchen counter.</div>
      </div>

      <div class='info-row'><span class='label'>Scheduled Date:</span><span class='value'>{mealDate:dddd, MMMM dd, yyyy}</span></div>
      <div class='info-row'><span class='label'>Meal Category:</span><span class='value'>{mealType}</span></div>
      <div class='info-row'><span class='label'>Prepared Dish:</span><span class='value'>{WebUtility.HtmlEncode(mealName)}</span></div>
      <div class='info-row'><span class='label'>Price / Mess Debit:</span><span class='value'>₹{price:F2}</span></div>
      <div class='info-row'><span class='label'>Security Verification Hash:</span><span class='value' style='font-family: monospace; font-size: 11px;'>{qrHash[..Math.Min(16, qrHash.Length)]}...</span></div>

      <p style='margin-top: 24px; font-size: 13px; color: #94a3b8;'>
        <strong>Kitchen Guidelines:</strong> Lunch window is strictly between 12:30 PM and 02:15 PM. Please adhere to security enclave regulations.
      </p>
    </div>
    <div class='footer'>
      DRDO HQ, Rajaji Marg, New Delhi • Official Catering Command • Autonomous Mess Dispatcher
    </div>
  </div>
</body>
</html>";

        // 1. Try Brevo HTTP API (if BREVO_API_KEY is available)
        var brevoApiKey = _configuration["BREVO_API_KEY"];
        var brevoSender = _configuration["BREVO_SENDER_EMAIL"] ?? "adixx2384@gmail.com";

        if (!string.IsNullOrWhiteSpace(brevoApiKey))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", brevoApiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new
                {
                    sender = new { name = "DRDO Canteen Catering", email = brevoSender },
                    to = new[] { new { email = recipientEmail, name = recipientName } },
                    subject = subject,
                    htmlContent = htmlContent
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Booking email successfully delivered via Brevo API to {Email} for token {Token}", recipientEmail, tokenRef);
                    return;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Brevo API delivery failed ({StatusCode}): {Error}. Attempting Gmail SMTP fallback...", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception sending email via Brevo. Falling back to Gmail SMTP...");
            }
        }

        // 2. Fallback to Gmail SMTP (if GMAIL_USER and GMAIL_APP_PASSWORD are available)
        var gmailUser = _configuration["GMAIL_USER"];
        var gmailPassword = _configuration["GMAIL_APP_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(gmailUser) && !string.IsNullOrWhiteSpace(gmailPassword))
        {
            try
            {
                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(gmailUser, gmailPassword)
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(gmailUser, "DRDO Canteen Catering"),
                    Subject = subject,
                    Body = htmlContent,
                    IsBodyHtml = true
                };

                mail.To.Add(new MailAddress(recipientEmail, recipientName));

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("Booking email successfully sent via Gmail SMTP to {Email} for token {Token}", recipientEmail, tokenRef);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gmail SMTP dispatch encountered an error.");
            }
        }

        _logger.LogInformation("Email notification logged: Token {Token} reserved for {Recipient} ({Email}). Set BREVO_API_KEY or GMAIL credentials to enable live sending.", tokenRef, recipientName, recipientEmail);
    }
}
