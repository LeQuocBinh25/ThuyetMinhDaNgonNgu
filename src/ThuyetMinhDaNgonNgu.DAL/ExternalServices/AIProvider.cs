using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;

namespace ThuyetMinhDaNgonNgu.DAL.ExternalServices;

public class AIProvider : IAiNarrationProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AIProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GenerateNarrationAsync(
        string locationName,
        string description,
        string languageCode)
    {
        string apiKey = _configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Gemini:ApiKey.");

        string model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        string language = languageCode.ToLower() switch
        {
            "vi" => "Vietnamese",
            "en" => "English",
            "ja" => "Japanese",
            "ko" => "Korean",
            "fr" => "French",
            "zh" => "Chinese",
            _ => languageCode
        };

        string prompt = $"""
            Write a clear and engaging tourist narration in {language} (language code: {languageCode}).
            Location: {locationName}
            Description: {description}
            Only return the narration text.
            """;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");

        request.Headers.Add("x-goog-api-key", apiKey);

        request.Content = JsonContent.Create(new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        });

        using var response = await _httpClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini API lỗi ({(int)response.StatusCode}): {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);

        return document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException(
                "Gemini không trả về nội dung.");
    }
}
