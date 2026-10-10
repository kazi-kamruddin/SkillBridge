using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace SkillBridge.Services;

public sealed record UploadedImage(string PublicId, string Format, string Url);

public sealed class CloudinaryImageService
{
    private readonly HttpClient client;
    private readonly string cloudName;
    private readonly string apiKey;
    private readonly string apiSecret;

    public CloudinaryImageService(HttpClient client)
    {
        this.client = client;
        cloudName = Environment.GetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_CLOUD_NAME");
        apiKey = Environment.GetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_API_KEY");
        apiSecret = Environment.GetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_API_SECRET");
    }

    public static string Validate(IFormFile file, long maximumBytes = 5 * 1024 * 1024)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > maximumBytes) return $"Choose an image smaller than {maximumBytes / 1024 / 1024} MB.";
        return DetectContentType(file) == null ? "Choose a JPEG, PNG, or WebP image." : null;
    }

    private static string DetectContentType(IFormFile file)
    {
        if (file.Length < 12) return null;
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[12];
        try { stream.ReadExactly(header); }
        catch (EndOfStreamException) { return null; }
        var jpeg = header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;
        var png = header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var webp = header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8);
        return jpeg ? "image/jpeg" : png ? "image/png" : webp ? "image/webp" : null;
    }

    public async Task<UploadedImage> UploadAsync(IFormFile file, string folder, bool authenticated)
    {
        var validation = Validate(file);
        if (validation != null) throw new ArgumentException(validation, nameof(file));
        EnsureConfigured();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["folder"] = folder,
            ["timestamp"] = timestamp,
            ["type"] = authenticated ? "authenticated" : "upload"
        };
        using var form = new MultipartFormDataContent();
        foreach (var pair in parameters) form.Add(new StringContent(pair.Value), pair.Key);
        form.Add(new StringContent(apiKey), "api_key");
        form.Add(new StringContent(Sign(parameters)), "signature");
        var contentType = DetectContentType(file);
        using var stream = file.OpenReadStream();
        var imageContent = new StreamContent(stream);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(imageContent, "file", contentType == "image/jpeg" ? "upload.jpg" :
            contentType == "image/png" ? "upload.png" : "upload.webp");
        using var response = await client.PostAsync($"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/image/upload", form);
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        return new UploadedImage(root.GetProperty("public_id").GetString(),
            root.GetProperty("format").GetString(), root.GetProperty("secure_url").GetString());
    }

    public async Task<byte[]> DownloadAuthenticatedAsync(string publicId, string format)
    {
        EnsureConfigured();
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["expires_at"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString(),
            ["format"] = format,
            ["public_id"] = publicId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["type"] = "authenticated"
        };
        var query = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var url = $"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/image/download?{query}&api_key={Uri.EscapeDataString(apiKey)}&signature={Sign(parameters)}";
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task DeletePublicAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        EnsureConfigured();
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["public_id"] = publicId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["type"] = "upload"
        };
        using var form = new FormUrlEncodedContent(parameters.Append(new KeyValuePair<string, string>("api_key", apiKey))
            .Append(new KeyValuePair<string, string>("signature", Sign(parameters))));
        using var response = await client.PostAsync($"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/image/destroy", form);
        response.EnsureSuccessStatusCode();
    }

    private string Sign(SortedDictionary<string, string> parameters)
    {
        var data = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}")) + apiSecret;
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new InvalidOperationException("Cloudinary server credentials are missing.");
    }
}
