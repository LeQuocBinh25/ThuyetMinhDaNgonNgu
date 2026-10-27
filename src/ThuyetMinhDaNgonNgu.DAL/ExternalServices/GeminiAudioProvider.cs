
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using ThuyetMinhDaNgonNgu.BLL.Interfaces;
using ThuyetMinhDaNgonNgu.BLL.Models;

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

    public async Task<GeneratedAudio> GenerateAudioAsync(
        string text,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        string apiKey = _configuration["Gemini:ApiKey"]
            ?? throw new ProviderConfigurationException("Chưa cấu hình Gemini:ApiKey.");

        string model = _configuration["Gemini:TtsModel"] ?? "gemini-2.5-flash-preview-tts";
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");

        request.Headers.Add("x-goog-api-key", apiKey);

        string ttsLanguageCode = languageCode.ToLowerInvariant() switch
        {
            "vi" => "vi-VN",
            "en" => "en-US",
            "ja" => "ja-JP",
            "ko" => "ko-KR",
            "fr" => "fr-FR",
            "zh" => "cmn-CN",
            _ => languageCode
        };

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
                            text = $"Speak clearly and naturally in the language with code {languageCode}: {text}"
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
                        prebuiltVoiceConfig = new
                        {
                            voiceName = "Kore"
                        }
                    },
                    languageCode = ttsLanguageCode
                }
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini TTS lỗi ({(int)response.StatusCode}): {responseBody}");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array)
            {
                throw new ProviderResponseException(
                    "Gemini TTS không trả về dữ liệu audio. Hãy nhập nội dung thuyết minh dài hơn rồi thử lại.");
            }

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (!part.TryGetProperty("inlineData", out var inlineData) ||
                        !inlineData.TryGetProperty("data", out var dataElement) ||
                        dataElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    string? audioBase64 = dataElement.GetString();
                    if (string.IsNullOrWhiteSpace(audioBase64))
                    {
                        continue;
                    }

                    string mimeType = inlineData.TryGetProperty("mimeType", out var mimeTypeValue)
                        ? mimeTypeValue.GetString() ?? string.Empty
                        : string.Empty;
                    byte[] bytes = Convert.FromBase64String(audioBase64);
                    return CreateBrowserAudio(bytes, mimeType);
                }
            }

            throw new ProviderResponseException(
                "Gemini TTS không tạo được audio cho nội dung này. Hãy nhập nội dung thuyết minh dài hơn rồi thử lại.");
        }
        catch (JsonException exception)
        {
            throw new ProviderResponseException(
                "Gemini TTS trả về phản hồi không hợp lệ, không thể tạo audio.",
                exception);
        }
        catch (FormatException exception)
        {
            throw new ProviderResponseException(
                "Gemini TTS trả về dữ liệu audio không hợp lệ.",
                exception);
        }
    }

    private static GeneratedAudio CreateBrowserAudio(byte[] bytes, string mimeType)
    {
        if (mimeType.StartsWith("audio/wav", StringComparison.OrdinalIgnoreCase) ||
            mimeType.StartsWith("audio/x-wav", StringComparison.OrdinalIgnoreCase))
        {
            return new GeneratedAudio(bytes, "audio/wav", ".wav");
        }

        if (!mimeType.StartsWith("audio/L16", StringComparison.OrdinalIgnoreCase) &&
            !mimeType.StartsWith("audio/pcm", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Gemini trả về định dạng audio không được hỗ trợ: '{mimeType}'.");
        }

        var rateMatch = Regex.Match(mimeType, @"(?:^|;)\s*rate=(\d+)", RegexOptions.IgnoreCase);
        int sampleRate = rateMatch.Success
            ? int.Parse(rateMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)
            : 24000;

        if (sampleRate is < 8000 or > 96000 || bytes.Length == 0 || bytes.Length % 2 != 0)
        {
            throw new InvalidOperationException("Gemini trả về dữ liệu PCM không hợp lệ.");
        }

        const short channels = 1;
        const short bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        using var output = new MemoryStream(bytes.Length + 44);
        using var writer = new BinaryWriter(output);
        writer.Write("RIFF"u8);
        writer.Write(bytes.Length + 36);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(bytes.Length);
        writer.Write(bytes);
        return new GeneratedAudio(output.ToArray(), "audio/wav", ".wav");
    }
}
