using System.Diagnostics;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using WebApi.Models;

namespace WebApi.Services;

public class VerificationService : IVerificationService
{
    private readonly IConfiguration _configuration;
    private readonly EmailClient _emailClient;
    private readonly IMemoryCache _cache;
    private static readonly Random _random = new Random();

    public VerificationService(IConfiguration configuration, EmailClient emailClient, IMemoryCache cache)
    {
        _configuration = configuration;
        _emailClient = emailClient;
        _cache = cache;
    }

    public async Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return new VerificationServiceResult { Success = false, Error = "Recipient email is required." };

            var verificationCode = _random.Next(100000, 999999).ToString();
            var subject = $"Your verification code for Ventixe";
            var plainTextContent = $@"
                Hello,

                To complete the verification, please enter the following code:
        
                {verificationCode}

                If you did not initiate this request, you can safely disregard this email.
            ";
            var htmlContent = $@"
                <!DOCTYPE html>
                <html lang='en'>
                  <head>
                    <meta charset='UTF-8' />
                    <title>Your verification code for Ventixe</title>
                  </head>
                  <body style='margin:0; padding:32px; font-family: Inter, sans-serif; background-color: #f7f7f7; color: #1e1e20;'>
                    <div style='max-width:600px; margin:32px auto; background: #ffffff; border-radius:16px; padding:32px;'>

                        <p style='font-size:16px; color: #1e1e20; margin-bottom:16px;'>Hello, </p>

                        <p style='font-size:16px; color: #1e1e20; margin-bottom:24px;'>To complete the verification, please enter the following code:</p>

                        <div style='display:flex; justify-content:center; align-items:center; padding:16px; background-color:#fcd3fe; color:#1c2346; font-size:20px; font-weight:600; border-radius:16px;'>
                        {verificationCode}
                        </div>

                        <p style='font-size:12px; color:#777779; text-align:center; margin-top:24px;'>If you did not initiate this request, you can safely disregard this email.</p>
                    </div>
                  </body>
                </html>
            ";

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAddress"],
                recipients: new EmailRecipients([new(request.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                }
            );

            var emailSendOperation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            SaveVerificationCode(new SaveVerificationCodeRequest { Email = request.Email, Code = verificationCode, ValidFor = TimeSpan.FromMinutes(10) });

            return new VerificationServiceResult { Success = true, Message = "Verification email sent successfully." };
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(ex);
            return new VerificationServiceResult { Success = false, Error = "Failed to send verification email." };
        }
    }

    public void SaveVerificationCode(SaveVerificationCodeRequest request)
    {
        _cache.Set(request.Email.ToLowerInvariant(), request.Code, request.ValidFor);
    }

    public VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        var key = request.Email.ToLowerInvariant();

        if (_cache.TryGetValue(key, out string? storedCode))
        {
            
            if (storedCode == request.Code)
            {
                _cache.Remove(key);
                return new VerificationServiceResult { Success = true, Message = "Verification successfull." };
            }
        }
        return new VerificationServiceResult { Success = false, Error = "Invalid or expired verification code." };
    }
}