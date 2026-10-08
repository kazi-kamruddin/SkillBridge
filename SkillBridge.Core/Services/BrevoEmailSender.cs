using System.Net.Http.Json;

namespace SkillBridge.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string destination, string subject, string body);
}

public sealed class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient client;
    private readonly string apiKey;
    private readonly string fromEmail;

    public BrevoEmailSender(HttpClient client)
    {
        this.client = client;
        apiKey = Environment.GetEnvironmentVariable("SKILLBRIDGE_BREVO_API_KEY");
        fromEmail = Environment.GetEnvironmentVariable("SKILLBRIDGE_BREVO_FROM");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(apiKey) &&
        !string.IsNullOrWhiteSpace(fromEmail);

    public async Task SendAsync(string destination, string subject, string body)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Password-reset email is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { email = fromEmail, name = "SkillBridge" },
            to = new[] { new { email = destination } },
            subject,
            textContent = body
        });
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
