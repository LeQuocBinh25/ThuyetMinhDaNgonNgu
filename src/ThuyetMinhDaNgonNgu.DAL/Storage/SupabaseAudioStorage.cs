using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

namespace ThuyetMinhDaNgonNgu.DAL.Storage;

public sealed class SupabaseAudioStorage : IAudioStorage
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _serviceRoleKey;
    private readonly string _bucket;

    public SupabaseAudioStorage(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = (configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Chưa cấu hình Supabase:Url.")).TrimEnd('/');
        _serviceRoleKey = configuration["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException("Chưa cấu hình Supabase:ServiceRoleKey.");
        _bucket = configuration["Supabase:AudioBucket"] ?? "narration-audio";

        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Host != "localhost"))
        {
            throw new InvalidOperationException("Supabase:Url phải là địa chỉ HTTPS hợp lệ.");
        }
    }

    public async Task UploadAsync(string objectPath, GeneratedAudio audio)
    {
        using var request = CreateRequest(HttpMethod.Post, ObjectUrl(objectPath));
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new ByteArrayContent(audio.Content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(audio.ContentType);
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Supabase Storage upload failed with HTTP {(int)response.StatusCode}.");
        }

    }

    public async Task<bool> ExistsAsync(string objectPath)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, PublicObjectUrl(objectPath));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
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

    public async Task DeleteAsync(string objectPath)
    {
        using var request = CreateRequest(
            HttpMethod.Delete,
            $"{_baseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}");
        request.Content = JsonContent.Create(new { prefixes = new[] { objectPath } });
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new HttpRequestException(
                $"Supabase Storage delete failed with HTTP {(int)response.StatusCode}.");
        }
    }

    public string GetPublicUrl(string objectPath) => PublicObjectUrl(objectPath);

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        return request;
    }

    private string ObjectUrl(string objectPath) =>
        $"{_baseUrl}/storage/v1/object/{Uri.EscapeDataString(_bucket)}/{EncodeObjectPath(objectPath)}";

    private string PublicObjectUrl(string objectPath) =>
        $"{_baseUrl}/storage/v1/object/public/{Uri.EscapeDataString(_bucket)}/{EncodeObjectPath(objectPath)}";

    private static string EncodeObjectPath(string objectPath) =>
        string.Join('/', objectPath.Split('/').Select(Uri.EscapeDataString));
}
