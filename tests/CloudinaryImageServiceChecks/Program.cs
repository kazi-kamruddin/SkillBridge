using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using SkillBridge.Helpers;
using SkillBridge.Services;

Environment.SetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_CLOUD_NAME", null);
Environment.SetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_API_KEY", null);
Environment.SetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_API_SECRET", null);
using var unconfiguredHttp = new HttpClient(new MockCloudinaryHandler());
var unconfiguredService = new CloudinaryImageService(unconfiguredHttp);
Checks.That(!unconfiguredService.IsConfigured, "Cloudinary service should report unconfigured when credentials are missing");

Environment.SetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_CLOUD_NAME", "test-cloud");
Environment.SetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_API_KEY", "test-key");
Environment.SetEnvironmentVariable("SKILLBRIDGE_CLOUDINARY_API_SECRET", "test-secret");
Environment.SetEnvironmentVariable("SKILLBRIDGE_MESSAGE_KEY", Convert.ToBase64String(new byte[32]));

var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 };
using var stream = new MemoryStream(png);
var file = new FormFile(stream, 0, png.Length, "file", "picture.png");
Checks.That(CloudinaryImageService.Validate(file) == null, "PNG header should be accepted");

const long maxAvatarBytes = 3 * 1024 * 1024;
var exact3Mb = new byte[maxAvatarBytes];
Array.Copy(png, exact3Mb, png.Length);
using var exact3MbStream = new MemoryStream(exact3Mb);
Checks.That(CloudinaryImageService.Validate(new FormFile(exact3MbStream, 0, exact3Mb.Length, "file", "exact-3mb.png"), maxAvatarBytes) == null,
    "Exact 3 MB avatar should pass 3 MB limit");
var over3Mb = new byte[maxAvatarBytes + 1];
Array.Copy(png, over3Mb, png.Length);
using var over3MbStream = new MemoryStream(over3Mb);
Checks.That(CloudinaryImageService.Validate(new FormFile(over3MbStream, 0, over3Mb.Length, "file", "over-3mb.png"), maxAvatarBytes) == "Choose an image smaller than 3 MB.",
    "Avatar over 3 MB must be rejected with 3 MB message");

using var badStream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));
Checks.That(CloudinaryImageService.Validate(new FormFile(badStream, 0, badStream.Length, "file", "bad.png")) != null,
    "File extension alone must not pass validation");

using var http = new HttpClient(new MockCloudinaryHandler());
var service = new CloudinaryImageService(http);
Checks.That(service.IsConfigured, "Cloudinary service should report configured when credentials are set");
var upload = await service.UploadAsync(file, "skillbridge/chat", true);
Checks.That(upload.PublicId == "skillbridge/chat/demo" && upload.Format == "png", "Authenticated upload result");
var downloaded = await service.DownloadAuthenticatedAsync(upload.PublicId, upload.Format);
Checks.That(downloaded.SequenceEqual(png), "Authenticated download result");
Checks.That(ProfileImageHelper.GetProfileImage(null, "Amina") == ProfileImageHelper.GetProfileImage(null, "Amina"),
    "Initial avatar must be stable across pages");
Checks.That(ProfileImageHelper.GetProfileImage(null, "Amina").StartsWith("data:image/svg+xml,"),
    "Missing photo must use an initial avatar");
var emptyCaption = MessageEncryptionService.Encrypt("");
Checks.That(MessageEncryptionService.Decrypt(emptyCaption.Ciphertext, emptyCaption.IV, emptyCaption.Hmac) == "",
    "Image-only messages must have an encrypted empty caption");
Console.WriteLine("Cloudinary image service checks passed.");

static class Checks
{
    public static void That(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
    }
}

sealed class MockCloudinaryHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri.AbsolutePath.EndsWith("/image/upload"))
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!body.Contains("authenticated") || !body.Contains("skillbridge/chat") || !body.Contains("test-key"))
                throw new Exception("Chat upload must be authenticated and signed by the server");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"public_id\":\"skillbridge/chat/demo\",\"format\":\"png\",\"secure_url\":\"https://res.cloudinary.com/test-cloud/image/authenticated/demo.png\"}")
            };
        }
        if (request.RequestUri.AbsolutePath.EndsWith("/image/download"))
        {
            var query = request.RequestUri.Query.TrimStart('?').Split('&')
                .Select(p => p.Split('=', 2)).ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p[1]));
            Checks.That(query["type"] == "authenticated", "Private download type");
            Checks.That(query["public_id"] == "skillbridge/chat/demo", "Private download asset");
            var toSign = string.Join("&", query.Where(p => p.Key != "api_key" && p.Key != "signature")
                .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}")) + "test-secret";
            var expected = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(toSign))).ToLowerInvariant();
            Checks.That(query["signature"] == expected, "Private download signature");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 })
            };
        }
        throw new Exception("Unexpected Cloudinary request");
    }
}
