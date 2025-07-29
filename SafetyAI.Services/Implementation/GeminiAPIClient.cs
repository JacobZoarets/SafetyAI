using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Configuration;
using SafetyAI.Services.Interfaces;
using SafetyAI.Services.Models;

namespace SafetyAI.Services.Implementation
{
    public class GeminiAPIClient : IGeminiAPIClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private bool _disposed = false;

        public GeminiAPIClient()
        {
            _apiKey = ServiceConfiguration.GeminiAPIKey;
            _baseUrl = ServiceConfiguration.GeminiAPIEndpoint;
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured. Please set the GeminiAPIKey in app settings.");
            }

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(ServiceConfiguration.ProcessingTimeoutSeconds)
            };
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SafetyAI/1.0");
        }

        public async Task<DocumentAnalysisResult> ProcessDocumentAsync(byte[] fileData, string contentType)
        {
            try
            {
                var request = new GeminiRequest
                {
                    Contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart
                                {
                                    Text = "Extract all text content from this safety incident document. Provide the extracted text with high accuracy, maintaining the original structure and formatting where possible. Focus on safety-related information, incident details, dates, locations, personnel involved, and any recommendations or actions taken."
                                },
                                new GeminiPart
                                {
                                    InlineData = new GeminiInlineData
                                    {
                                        MimeType = contentType,
                                        Data = Convert.ToBase64String(fileData)
                                    }
                                }
                            }
                        }
                    }
                };

                var response = await CallGeminiAPIAsync("generateContent", request);
                
                return new DocumentAnalysisResult
                {
                    ExtractedText = response.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "",
                    ConfidenceScore = CalculateConfidenceScore(response),
                    DetectedLanguages = DetectLanguages(response.Candidates?[0]?.Content?.Parts?[0]?.Text ?? ""),
                    ProcessingTime = TimeSpan.FromMilliseconds(response.ProcessingTimeMs),
                    RequiresHumanReview = response.Candidates?[0]?.FinishReason == "SAFETY" || CalculateConfidenceScore(response) < 0.8,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new DocumentAnalysisResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Document processing failed: {ex.Message}",
                    RequiresHumanReview = true
                };
            }
        }

        public async Task<SafetyAnalysisResult> AnalyzeSafetyContentAsync(string text)
        {
            try
            {
                var prompt = @"Analyze this safety incident report and provide a structured analysis. 

Text to analyze: " + text + @"

Please provide your analysis in the following JSON format:
{
  ""incidentType"": ""Fall|Slip|EquipmentFailure|ChemicalExposure|NearMiss|Fire|Electrical|Other"",
  ""severity"": ""Low|Medium|High|Critical"",
  ""riskScore"": 1-10,
  ""summary"": ""Brief summary of the incident"",
  ""keyFactors"": [""factor1"", ""factor2"", ""factor3""],
  ""recommendations"": [
    {
      ""type"": ""Preventive|Corrective|Administrative"",
      ""description"": ""Specific recommendation"",
      ""priority"": ""Low|Medium|High|Critical"",
      ""estimatedCost"": 0.00,
      ""estimatedTimeHours"": 0,
      ""responsibleRole"": ""Role responsible for implementation""
    }
  ],
  ""complianceMapping"": {
    ""oshaStandards"": [""standard1"", ""standard2""],
    ""iso45001Requirements"": [""requirement1"", ""requirement2""],
    ""localRegulations"": [""regulation1"", ""regulation2""]
  }
}

Focus on:
1. Accurate incident classification
2. Realistic risk assessment (1-10 scale)
3. Actionable safety recommendations
4. Relevant compliance standards
5. Cost-effective solutions";

                var request = new GeminiRequest
                {
                    Contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = prompt }
                            }
                        }
                    }
                };

                var response = await CallGeminiAPIAsync("generateContent", request);
                var analysisText = response.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                
                return ParseSafetyAnalysis(analysisText, CalculateConfidenceScore(response));
            }
            catch (Exception ex)
            {
                return new SafetyAnalysisResult
                {
                    Summary = $"Analysis failed: {ex.Message}",
                    AnalysisConfidence = 0.0
                };
            }
        }

        public async Task<ChatResponse> ProcessChatQueryAsync(string query, string sessionId)
        {
            try
            {
                var prompt = $@"You are a safety expert AI assistant. Answer the following safety-related question with accurate, helpful information.

Question: {query}

Please provide:
1. A clear, actionable answer
2. Relevant safety procedures or guidelines
3. Any applicable regulations or standards
4. Suggested follow-up actions if appropriate

Keep your response professional, accurate, and focused on safety best practices.";

                var request = new GeminiRequest
                {
                    Contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart { Text = prompt }
                            }
                        }
                    }
                };

                var response = await CallGeminiAPIAsync("generateContent", request);
                var responseText = response.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                
                return new ChatResponse
                {
                    Response = responseText,
                    ConfidenceScore = CalculateConfidenceScore(response),
                    SessionId = sessionId,
                    RequiresHumanReview = responseText.ToLower().Contains("contact") || responseText.ToLower().Contains("consult"),
                    SuggestedActions = ExtractSuggestedActions(responseText)
                };
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    Response = $"I apologize, but I'm unable to process your query at the moment. Please try again or contact a safety professional for assistance. Error: {ex.Message}",
                    ConfidenceScore = 0.0,
                    SessionId = sessionId,
                    RequiresHumanReview = true
                };
            }
        }

        public async Task<AudioProcessingResult> ProcessAudioAsync(byte[] audioData, string contentType)
        {
            try
            {
                var request = new GeminiRequest
                {
                    Contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            Parts = new List<GeminiPart>
                            {
                                new GeminiPart
                                {
                                    Text = "Transcribe this audio recording of a safety incident report. Focus on extracting safety-related information, incident details, and any recommendations mentioned. Identify safety terminology and key phrases."
                                },
                                new GeminiPart
                                {
                                    InlineData = new GeminiInlineData
                                    {
                                        MimeType = contentType,
                                        Data = Convert.ToBase64String(audioData)
                                    }
                                }
                            }
                        }
                    }
                };

                var response = await CallGeminiAPIAsync("generateContent", request);
                var transcribedText = response.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                
                return new AudioProcessingResult
                {
                    TranscribedText = transcribedText,
                    DetectedLanguage = DetectPrimaryLanguage(transcribedText),
                    TranscriptionConfidence = CalculateConfidenceScore(response),
                    SafetyTermsIdentified = ExtractSafetyTerms(transcribedText),
                    RequiresReprocessing = CalculateConfidenceScore(response) < 0.7,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new AudioProcessingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Audio processing failed: {ex.Message}",
                    RequiresReprocessing = true
                };
            }
        }

        public async Task<T> CallWithRetryAsync<T>(Func<Task<T>> apiCall)
        {
            var retryCount = ServiceConfiguration.RetryCount;
            var retryDelay = TimeSpan.FromSeconds(ServiceConfiguration.RetryDelaySeconds);
            
            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    return await apiCall();
                }
                catch (HttpRequestException ex) when (IsTransientError(ex) && attempt < retryCount)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * attempt)); // Exponential backoff
                    continue;
                }
                catch (TaskCanceledException ex) when (attempt < retryCount)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * attempt));
                    continue;
                }
            }
            
            // Final attempt without retry
            return await apiCall();
        }

        private async Task<GeminiResponse> CallGeminiAPIAsync(string endpoint, GeminiRequest request)
        {
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/models/gemini-2.5-flash:{endpoint}?key={_apiKey}";

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync(url, content);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API call failed: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseJson);
            geminiResponse.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
            
            return geminiResponse;
        }

        private bool IsTransientError(HttpRequestException ex)
        {
            return ex.Message.Contains("timeout") || 
                   ex.Message.Contains("503") || 
                   ex.Message.Contains("502") || 
                   ex.Message.Contains("429");
        }

        private double CalculateConfidenceScore(GeminiResponse response)
        {
            // Simple confidence calculation based on response quality
            if (response?.Candidates == null || response.Candidates.Count == 0)
                return 0.0;

            var candidate = response.Candidates[0];
            if (candidate.FinishReason == "STOP")
                return 0.95;
            else if (candidate.FinishReason == "MAX_TOKENS")
                return 0.85;
            else if (candidate.FinishReason == "SAFETY")
                return 0.60;
            else
                return 0.70;
        }

        private List<string> DetectLanguages(string text)
        {
            var languages = new List<string>();
            
            if (string.IsNullOrEmpty(text)) return languages;
            
            // Simple language detection based on character patterns
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0590-\u05FF]"))
                languages.Add("he"); // Hebrew
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0600-\u06FF]"))
                languages.Add("ar"); // Arabic
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0400-\u04FF]"))
                languages.Add("ru"); // Russian
            
            // Default to English if no other language detected
            if (languages.Count == 0)
                languages.Add("en");
                
            return languages;
        }

        private string DetectPrimaryLanguage(string text)
        {
            var languages = DetectLanguages(text);
            return languages.Count > 0 ? languages[0] : "en";
        }

        private List<string> ExtractSafetyTerms(string text)
        {
            var safetyTerms = new List<string>();
            var commonSafetyTerms = new[]
            {
                "accident", "incident", "injury", "hazard", "risk", "safety", "emergency",
                "fall", "slip", "trip", "fire", "explosion", "chemical", "exposure",
                "equipment", "failure", "malfunction", "PPE", "helmet", "gloves",
                "evacuation", "first aid", "medical", "hospital", "ambulance"
            };

            var lowerText = text.ToLower();
            foreach (var term in commonSafetyTerms)
            {
                if (lowerText.Contains(term))
                {
                    safetyTerms.Add(term);
                }
            }

            return safetyTerms;
        }

        private List<string> ExtractSuggestedActions(string text)
        {
            var actions = new List<string>();
            var lines = text.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("-") || 
                    line.Trim().StartsWith("•") || 
                    line.ToLower().Contains("recommend") ||
                    line.ToLower().Contains("should") ||
                    line.ToLower().Contains("action"))
                {
                    actions.Add(line.Trim());
                }
            }
            
            return actions;
        }

        private SafetyAnalysisResult ParseSafetyAnalysis(string analysisText, double confidence)
        {
            try
            {
                // Try to extract JSON from the response
                var jsonStart = analysisText.IndexOf('{');
                var jsonEnd = analysisText.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonText = analysisText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var parsed = JsonConvert.DeserializeObject<dynamic>(jsonText);
                    
                    var result = new SafetyAnalysisResult
                    {
                        IncidentType = ParseIncidentType(parsed.incidentType?.ToString()),
                        Severity = ParseSeverityLevel(parsed.severity?.ToString()),
                        RiskScore = parsed.riskScore ?? 5,
                        Summary = parsed.summary?.ToString() ?? "Analysis completed",
                        AnalysisConfidence = confidence
                    };
                    
                    // Parse key factors
                    if (parsed.keyFactors != null)
                    {
                        foreach (var factor in parsed.keyFactors)
                        {
                            result.KeyFactors.Add(factor.ToString());
                        }
                    }
                    
                    // Parse recommendations
                    if (parsed.recommendations != null)
                    {
                        foreach (var rec in parsed.recommendations)
                        {
                            result.Recommendations.Add(new SafetyRecommendation
                            {
                                Type = rec.type?.ToString() ?? "General",
                                Description = rec.description?.ToString() ?? "",
                                Priority = rec.priority?.ToString() ?? "Medium",
                                EstimatedCost = rec.estimatedCost ?? 0,
                                EstimatedTimeHours = rec.estimatedTimeHours ?? 0,
                                ResponsibleRole = rec.responsibleRole?.ToString() ?? "Safety Manager"
                            });
                        }
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse safety analysis JSON: {ex.Message}");
            }
            
            // Fallback to basic analysis
            return new SafetyAnalysisResult
            {
                Summary = "Basic analysis completed - detailed parsing failed",
                AnalysisConfidence = confidence * 0.7 // Reduce confidence for fallback
            };
        }

        private SafetyAI.Models.Enums.IncidentType ParseIncidentType(string type)
        {
            if (string.IsNullOrEmpty(type)) return SafetyAI.Models.Enums.IncidentType.Other;
            
            switch (type.ToLower())
            {
                case "fall": return SafetyAI.Models.Enums.IncidentType.Fall;
                case "slip": return SafetyAI.Models.Enums.IncidentType.Slip;
                case "equipmentfailure": return SafetyAI.Models.Enums.IncidentType.EquipmentFailure;
                case "chemicalexposure": return SafetyAI.Models.Enums.IncidentType.ChemicalExposure;
                case "nearmiss": return SafetyAI.Models.Enums.IncidentType.NearMiss;
                case "fire": return SafetyAI.Models.Enums.IncidentType.Fire;
                case "electrical": return SafetyAI.Models.Enums.IncidentType.Electrical;
                default: return SafetyAI.Models.Enums.IncidentType.Other;
            }
        }

        private SafetyAI.Models.Enums.SeverityLevel ParseSeverityLevel(string severity)
        {
            if (string.IsNullOrEmpty(severity)) return SafetyAI.Models.Enums.SeverityLevel.Medium;
            
            switch (severity.ToLower())
            {
                case "low": return SafetyAI.Models.Enums.SeverityLevel.Low;
                case "medium": return SafetyAI.Models.Enums.SeverityLevel.Medium;
                case "high": return SafetyAI.Models.Enums.SeverityLevel.High;
                case "critical": return SafetyAI.Models.Enums.SeverityLevel.Critical;
                default: return SafetyAI.Models.Enums.SeverityLevel.Medium;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}