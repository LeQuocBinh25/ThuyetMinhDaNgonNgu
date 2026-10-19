
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;

namespace ThuyetMinhDaNgonNgu.DAL.ExternalServices;

public class GeminiAudioProvider : IAudioNarrationProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiAudioProvider(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<byte[]> GenerateAudioAsync(string text)
    {
        string apiKey = _configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Chưa cấu hình Gemini:ApiKey.");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash-tts:generateContent");

        request.Headers.Add("x-goog-api-key", apiKey);

        request.Content = JsonContent.Create(new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = text,
                            speech_metadata = new
                            {
                                style = "Speak clearly and naturally as a tourist guide."
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                responseModalities = new[] { "AUDIO" },
                speechConfig = new
                {
                    voiceConfig = new
                    {
                        voice = "Kore"
                    }
                }
            }
        });

        using var response = await _httpClient.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini TTS lỗi ({(int)response.StatusCode}): {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);

        string audioBase64 = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("inlineData")
            .GetProperty("data")
            .GetString()
            ?? throw new InvalidOperationException("Gemini không trả về âm thanh.");

        return Convert.FromBase64String(audioBase64);
    }
}
