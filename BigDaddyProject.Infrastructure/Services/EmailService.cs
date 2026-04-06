using BigDaddyProject.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BigDaddyProject.Infrastructure.Services;

// ── Email Service (dev: logs to console; prod: swap in MailKit) ───────
public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _log;
    public EmailService(IConfiguration cfg, ILogger<EmailService> log) { _cfg = cfg; _log = log; }

    public async Task SendPasswordResetAsync(string toEmail, string otp, string link)
    {
        _log.LogInformation("[EMAIL] Password reset to {Email} | OTP={OTP} | Link={Link}",
            toEmail, otp, link);
        await Task.CompletedTask;
        // PRODUCTION: replace with MailKit send
    }

    public async Task SendWelcomeAsync(string toEmail, string name, string tempPassword)
    {
        _log.LogInformation("[EMAIL] Welcome {Name} <{Email}> | TempPwd={Pwd}",
            name, toEmail, tempPassword);
        await Task.CompletedTask;
    }
}