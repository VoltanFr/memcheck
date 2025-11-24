using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemCheck.WebUI;

public interface ITurnstileValidator
{
    record TurnstileValidationResult(bool IsValid, string? ErrorMessage);
    string SiteKey { get; }
    Task<TurnstileValidationResult> ValidateAsync(string token, string expectedAction, string expectedCData, string remoteIp);
}

public sealed class TurnstileValidator : ITurnstileValidator
{
    #region private sealed class TurnstileResponse
    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        // Le nom JSON contient un tiret : il faut absolument JsonPropertyName
        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("cdata")]
        public string? CData { get; set; }

        [JsonPropertyName("metadata")]
        public TurnstileMetadata? Metadata { get; set; }
    }
    private sealed class TurnstileMetadata
    {
        [JsonPropertyName("ephemeral_id")]
        public string? EphemeralId { get; set; }
    }
    #endregion
    #region Private fields
    private readonly HttpClient httpClient;
    private readonly string secretKey;
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Uri turnstileUri = new("https://challenges.cloudflare.com/turnstile/v0/siteverify");
    #endregion

    public TurnstileValidator(HttpClient httpClient, string siteKey, string secretKey)
    {
        this.httpClient = httpClient;
        this.secretKey = secretKey;
        SiteKey = siteKey;
    }
    public string SiteKey { get; }
    public async Task<ITurnstileValidator.TurnstileValidationResult> ValidateAsync(string token, string expectedAction, string expectedCData, string remoteIp)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "secret", secretKey },
            { "response", token },
            { "remoteip", remoteIp }
        });

        using var response = await httpClient.PostAsync(turnstileUri, content);
        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<TurnstileResponse>(json, jsonSerializerOptions);
        if (result == null)
            return new ITurnstileValidator.TurnstileValidationResult(false, "Failed to get Cloudflare validation");

        if (!result.Success)
            if (result.ErrorCodes != null && result.ErrorCodes.Length > 0)
                return new ITurnstileValidator.TurnstileValidationResult(false, $"Cloudflare rejected your input (error codes: {string.Join(',', result.ErrorCodes)})");
            else
                return new ITurnstileValidator.TurnstileValidationResult(false, "Cloudflare rejected your input (no error code)");

        if (result.ErrorCodes != null && result.ErrorCodes.Length > 0)
            return new ITurnstileValidator.TurnstileValidationResult(false, $"Cloudflare validated your input, but got error codes: {string.Join(',', result.ErrorCodes)}");

        if (result.Action != expectedAction)
            return new ITurnstileValidator.TurnstileValidationResult(false, "Cloudflare action mismatch");

        if (result.CData != expectedCData)
            return new ITurnstileValidator.TurnstileValidationResult(false, "Cloudflare CData mismatch");

        return new ITurnstileValidator.TurnstileValidationResult(true, null);
    }
}
