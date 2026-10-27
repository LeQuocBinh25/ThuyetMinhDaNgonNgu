using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.DAL.Storage;

public sealed class SupabaseAudioStorage : IAudioStorage
{
    private const int SignedUrlExpiresInSeconds = 300;

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _serviceRoleKey;
    private readonly string _bucket;

    public SupabaseAudioStorage(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = (configuration["Supabase:Url"]
            ?? throw new ProviderConfigurationException("Chưa cấu hình Supabase:Url.")).TrimEnd('/');
        _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
            ?? throw new ProviderConfigurationException("Chưa cấu hình Supabase:ServiceRoleKey.");
        _bucket = configuration["Supabase:AudioBucket"] ?? "narration-audio";

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Host != "localhost"))
        {
            throw new ProviderConfigurationException("Supabase:Url phải là địa chỉ HTTPS hợp lệ.");
        }
    }

    public async Task UploadAsync(
        string objectPath,
        GeneratedAudio audio,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, ObjectUrl(objectPath));
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new ByteArrayContent(audio.Content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(audio.ContentType);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Supabase Storage upload failed with HTTP {(int)response.StatusCode}: " +
                GetErrorDetail(responseBody),
                inner: null,
                response.StatusCode);
        }

    }

    public async Task<bool> ExistsAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Head, ObjectUrl(objectPath));
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Supabase Storage check failed with HTTP {(int)response.StatusCode}.");
        }

        return true;
    }

    public async Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Delete,
            $"{_baseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}");
        request.Content = JsonContent.Create(new { prefixes = new[] { objectPath } });
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new HttpRequestException(
                $"Supabase Storage delete failed with HTTP {(int)response.StatusCode}.");
        }
    }

    public async Task<string> GetSignedUrlAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            $"{_baseUrl}/storage/v1/object/sign/{Uri.EscapeDataString(_bucket)}/{EncodeObjectPath(objectPath)}");
        request.Content = JsonContent.Create(new { expiresIn = SignedUrlExpiresInSeconds });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Supabase Storage signed URL request failed with HTTP {(int)response.StatusCode}: " +
                GetErrorDetail(responseBody),
                inner: null,
                response.StatusCode);
        }

        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("signedURL", out var signedUrlElement) ||
            signedUrlElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(signedUrlElement.GetString()))
        {
            throw new InvalidOperationException(
                "Supabase Storage returned a successful response without a signed URL.");
        }

        string signedUrl = signedUrlElement.GetString()!;
        if (signedUrl.StartsWith("/object/", StringComparison.Ordinal))
        {
            signedUrl = "/storage/v1" + signedUrl;
        }

        var baseUri = new Uri(_baseUrl, UriKind.Absolute);
        if (!Uri.TryCreate(new Uri(_baseUrl.TrimEnd('/') + "/", UriKind.Absolute), signedUrl, out var resultUri) ||
            resultUri.Scheme != baseUri.Scheme ||
            !string.Equals(resultUri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            resultUri.Port != baseUri.Port)
        {
            throw new InvalidOperationException(
                "Supabase Storage returned a signed URL with an unexpected origin.");
        }

        return resultUri.AbsoluteUri;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        return request;
    }

    private string ObjectUrl(string objectPath) =>
        $"{_baseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{EncodeObjectPath(objectPath)}";

    private static string EncodeObjectPath(string objectPath) =>
        string.Join('/', objectPath.Split('/').Select(Uri.EscapeDataString));

    private static string GetErrorDetail(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            foreach (string propertyName in new[] { "message", "error", "error_description" })
            {
                if (document.RootElement.TryGetProperty(propertyName, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    return Truncate(value.GetString() ?? string.Empty);
                }
            }
        }
        catch (JsonException)
        {
            return "Supabase Storage returned a non-JSON error response.";
        }

        return "Supabase Storage did not provide an error message.";
    }

    private static string Truncate(string value) =>
        value.Length <= 500 ? value : value[..500];
}
