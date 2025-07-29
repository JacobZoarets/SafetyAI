using System.Collections.Generic;
using Newtonsoft.Json;

namespace SafetyAI.Services.Models
{
    public class GeminiRequest
    {
        [JsonProperty("contents")]
        public List<GeminiContent> Contents { get; set; } = new List<GeminiContent>();

        [JsonProperty("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; }

        [JsonProperty("safetySettings")]
        public List<GeminiSafetySetting> SafetySettings { get; set; }
    }

    public class GeminiContent
    {
        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();

        [JsonProperty("role")]
        public string Role { get; set; } = "user";
    }

    public class GeminiPart
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("inlineData")]
        public GeminiInlineData InlineData { get; set; }

        [JsonProperty("fileData")]
        public GeminiFileData FileData { get; set; }
    }

    public class GeminiInlineData
    {
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }

    public class GeminiFileData
    {
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("fileUri")]
        public string FileUri { get; set; }
    }

    public class GeminiGenerationConfig
    {
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        [JsonProperty("topK")]
        public int? TopK { get; set; }

        [JsonProperty("topP")]
        public double? TopP { get; set; }

        [JsonProperty("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }

        [JsonProperty("stopSequences")]
        public List<string> StopSequences { get; set; }
    }

    public class GeminiSafetySetting
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("threshold")]
        public string Threshold { get; set; }
    }

    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = new List<GeminiCandidate>();

        [JsonProperty("promptFeedback")]
        public GeminiPromptFeedback PromptFeedback { get; set; }

        [JsonProperty("usageMetadata")]
        public GeminiUsageMetadata UsageMetadata { get; set; }

        // Custom property for tracking processing time
        public int ProcessingTimeMs { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }

        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("safetyRatings")]
        public List<GeminiSafetyRating> SafetyRatings { get; set; }
    }

    public class GeminiPromptFeedback
    {
        [JsonProperty("safetyRatings")]
        public List<GeminiSafetyRating> SafetyRatings { get; set; }

        [JsonProperty("blockReason")]
        public string BlockReason { get; set; }
    }

    public class GeminiSafetyRating
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("probability")]
        public string Probability { get; set; }

        [JsonProperty("blocked")]
        public bool Blocked { get; set; }
    }

    public class GeminiUsageMetadata
    {
        [JsonProperty("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonProperty("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonProperty("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }

    public class GeminiError
    {
        [JsonProperty("error")]
        public GeminiErrorDetails Error { get; set; }
    }

    public class GeminiErrorDetails
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("details")]
        public List<object> Details { get; set; }
    }
}