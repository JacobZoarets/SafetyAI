using System;
using System.IO;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;
using SafetyAI.Services.Configuration;
using SafetyAI.Services.Exceptions;
using SafetyAI.Services.Infrastructure;
using SafetyAI.Services.Interfaces;

namespace SafetyAI.Services.Implementation
{
    public class AudioProcessor : IAudioProcessor, IDisposable
    {
        private readonly IGeminiAPIClient _geminiClient;
        private readonly IFileValidator _fileValidator;
        private bool _disposed = false;

        public AudioProcessor(IGeminiAPIClient geminiClient, IFileValidator fileValidator)
        {
            _geminiClient = geminiClient ?? throw new ArgumentNullException(nameof(geminiClient));
            _fileValidator = fileValidator ?? throw new ArgumentNullException(nameof(fileValidator));
        }

        public async Task<AudioProcessingResult> ProcessAudioAsync(Stream audioStream, string contentType)
        {
            using (var context = new LogContext($"ProcessAudio_{contentType}"))
            {
                try
                {
                    context.LogProgress("Starting audio processing");

                    // Validate input parameters
                    if (audioStream == null)
                        throw new ArgumentNullException(nameof(audioStream));
                    if (string.IsNullOrWhiteSpace(contentType))
                        throw new ArgumentException("Content type cannot be empty", nameof(contentType));

                    // Validate audio quality first
                    context.LogProgress("Validating audio quality");
                    if (!await ValidateAudioQualityAsync(audioStream))
                    {
                        return new AudioProcessingResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Audio quality validation failed. Please ensure the audio is clear and in a supported format.",
                            RequiresReprocessing = true
                        };
                    }

                    // Convert stream to byte array for Gemini processing
                    context.LogProgress("Converting audio stream to byte array");
                    byte[] audioData;
                    using (var memoryStream = new MemoryStream())
                    {
                        audioStream.Position = 0; // Reset stream position
                        await audioStream.CopyToAsync(memoryStream);
                        audioData = memoryStream.ToArray();
                    }

                    if (audioData.Length == 0)
                    {
                        return new AudioProcessingResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Audio file appears to be empty or corrupted.",
                            RequiresReprocessing = true
                        };
                    }

                    // Process audio with Gemini API
                    context.LogProgress("Processing audio with Gemini API");
                    var geminiResult = await _geminiClient.CallWithRetryAsync(async () =>
                    {
                        return await _geminiClient.ProcessAudioAsync(audioData, contentType);
                    });

                    if (!geminiResult.IsSuccess)
                    {
                        return new AudioProcessingResult
                        {
                            IsSuccess = false,
                            ErrorMessage = geminiResult.ErrorMessage ?? "Audio processing failed",
                            RequiresReprocessing = true
                        };
                    }

                    // Enhance the result with additional processing
                    var enhancedResult = EnhanceAudioResult(geminiResult);

                    context.LogProgress($"Audio processing completed. Confidence: {enhancedResult.TranscriptionConfidence:P2}");

                    // Log processing metrics
                    Logger.LogProcessingMetrics(
                        "AudioProcessing", 
                        TimeSpan.FromMilliseconds(context.ElapsedMilliseconds), 
                        enhancedResult.IsSuccess, 
                        audioData.Length);

                    return enhancedResult;
                }
                catch (Exception ex)
                {
                    context.LogError($"Audio processing failed: {ex.Message}");
                    Logger.LogError(ex, "AudioProcessor");

                    return new AudioProcessingResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "An unexpected error occurred during audio processing. Please try again.",
                        RequiresReprocessing = true
                    };
                }
            }
        }

        public async Task<bool> ValidateAudioQualityAsync(Stream audioStream)
        {
            try
            {
                if (audioStream == null || !audioStream.CanRead)
                    return false;

                // Check file size
                if (audioStream.Length == 0)
                    return false;

                if (audioStream.Length > ServiceConfiguration.MaxFileSize)
                    return false;

                // Reset stream position for header validation
                var originalPosition = audioStream.Position;
                audioStream.Seek(0, SeekOrigin.Begin);

                // Read header to validate audio format
                var headerBytes = new byte[Math.Min(512, audioStream.Length)];
                var bytesRead = await audioStream.ReadAsync(headerBytes, 0, headerBytes.Length);

                // Reset stream position
                audioStream.Seek(originalPosition, SeekOrigin.Begin);

                if (bytesRead < 4)
                    return false;

                // Validate audio format based on header
                return ValidateAudioHeader(headerBytes);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Audio quality validation failed: {ex.Message}", "AudioProcessor");
                return false;
            }
        }

        public async Task<VoiceCommandResult> ProcessVoiceCommandAsync(string audioText)
        {
            using (var context = new LogContext("ProcessVoiceCommand"))
            {
                try
                {
                    context.LogProgress($"Processing voice command: {audioText?.Substring(0, Math.Min(30, audioText?.Length ?? 0))}...");

                    if (string.IsNullOrWhiteSpace(audioText))
                    {
                        return new VoiceCommandResult
                        {
                            IsValid = false,
                            Command = "unknown",
                            Action = "No voice command detected"
                        };
                    }

                    var lowerText = audioText.ToLowerInvariant().Trim();
                    var result = new VoiceCommandResult();

                    // Parse navigation commands
                    if (lowerText.Contains("go to") || lowerText.Contains("navigate to") || lowerText.Contains("open"))
                    {
                        result = ParseNavigationCommand(lowerText);
                    }
                    // Parse upload commands
                    else if (lowerText.Contains("upload") || lowerText.Contains("submit") || lowerText.Contains("analyze"))
                    {
                        result = ParseUploadCommand(lowerText);
                    }
                    // Parse search commands
                    else if (lowerText.Contains("search") || lowerText.Contains("find") || lowerText.Contains("look for"))
                    {
                        result = ParseSearchCommand(lowerText);
                    }
                    // Parse help commands
                    else if (lowerText.Contains("help") || lowerText.Contains("assist") || lowerText.Contains("how to"))
                    {
                        result = ParseHelpCommand(lowerText);
                    }
                    // Parse safety-specific commands
                    else if (ContainsSafetyKeywords(lowerText))
                    {
                        result = ParseSafetyCommand(lowerText);
                    }
                    else
                    {
                        result = new VoiceCommandResult
                        {
                            IsValid = false,
                            Command = "unknown",
                            Action = "Voice command not recognized. Try saying 'help' for available commands."
                        };
                    }

                    context.LogProgress($"Voice command processed. Command: {result.Command}, Valid: {result.IsValid}");
                    return result;
                }
                catch (Exception ex)
                {
                    context.LogError($"Voice command processing failed: {ex.Message}");
                    Logger.LogError(ex, "AudioProcessor");

                    return new VoiceCommandResult
                    {
                        IsValid = false,
                        Command = "error",
                        Action = "Error processing voice command"
                    };
                }
            }
        }

        private AudioProcessingResult EnhanceAudioResult(AudioProcessingResult baseResult)
        {
            try
            {
                // Enhance transcribed text with safety context
                if (!string.IsNullOrWhiteSpace(baseResult.TranscribedText))
                {
                    // Identify safety-specific terminology
                    baseResult.SafetyTermsIdentified = IdentifySafetyTerms(baseResult.TranscribedText);

                    // Adjust confidence based on safety term recognition
                    if (baseResult.SafetyTermsIdentified.Count > 0)
                    {
                        baseResult.TranscriptionConfidence = Math.Min(1.0, baseResult.TranscriptionConfidence * 1.1);
                    }

                    // Check if reprocessing is needed based on confidence and content
                    baseResult.RequiresReprocessing = baseResult.TranscriptionConfidence < 0.7 || 
                                                     string.IsNullOrWhiteSpace(baseResult.TranscribedText) ||
                                                     baseResult.TranscribedText.Length < 10;
                }

                return baseResult;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to enhance audio result: {ex.Message}", "AudioProcessor");
                return baseResult; // Return original result if enhancement fails
            }
        }

        private bool ValidateAudioHeader(byte[] headerBytes)
        {
            if (headerBytes.Length < 4)
                return false;

            // WAV format validation
            if (headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46)
            {
                return headerBytes.Length >= 12 && 
                       headerBytes[8] == 0x57 && headerBytes[9] == 0x41 && 
                       headerBytes[10] == 0x56 && headerBytes[11] == 0x45;
            }

            // MP3 format validation
            if (headerBytes[0] == 0xFF && (headerBytes[1] & 0xE0) == 0xE0)
                return true;

            // MP3 with ID3 tag
            if (headerBytes[0] == 0x49 && headerBytes[1] == 0x44 && headerBytes[2] == 0x33)
                return true;

            // M4A format validation
            if (headerBytes.Length >= 8 && 
                headerBytes[4] == 0x66 && headerBytes[5] == 0x74 && 
                headerBytes[6] == 0x79 && headerBytes[7] == 0x70)
                return true;

            // OGG format validation
            if (headerBytes[0] == 0x4F && headerBytes[1] == 0x67 && 
                headerBytes[2] == 0x67 && headerBytes[3] == 0x53)
                return true;

            return false;
        }

        private System.Collections.Generic.List<string> IdentifySafetyTerms(string text)
        {
            var safetyTerms = new System.Collections.Generic.List<string>();
            var lowerText = text.ToLowerInvariant();

            var commonSafetyTerms = new[]
            {
                "accident", "incident", "injury", "hazard", "risk", "safety", "emergency",
                "fall", "slip", "trip", "fire", "explosion", "chemical", "exposure",
                "equipment", "failure", "malfunction", "ppe", "helmet", "gloves", "goggles",
                "evacuation", "first aid", "medical", "hospital", "ambulance", "lockout",
                "tagout", "confined space", "ventilation", "noise", "ergonomic", "lifting"
            };

            foreach (var term in commonSafetyTerms)
            {
                if (lowerText.Contains(term))
                {
                    safetyTerms.Add(term);
                }
            }

            return safetyTerms;
        }

        private VoiceCommandResult ParseNavigationCommand(string command)
        {
            var result = new VoiceCommandResult { Command = "navigate" };

            if (command.Contains("dashboard") || command.Contains("home"))
            {
                result.IsValid = true;
                result.Action = "navigate_dashboard";
                result.Parameters["url"] = "Default.aspx";
            }
            else if (command.Contains("results") || command.Contains("analysis"))
            {
                result.IsValid = true;
                result.Action = "navigate_results";
                result.Parameters["url"] = "Results.aspx";
            }
            else if (command.Contains("history") || command.Contains("reports"))
            {
                result.IsValid = true;
                result.Action = "navigate_history";
                result.Parameters["url"] = "History.aspx";
            }
            else if (command.Contains("chat") || command.Contains("assistant"))
            {
                result.IsValid = true;
                result.Action = "navigate_chat";
                result.Parameters["url"] = "Chat.aspx";
            }
            else
            {
                result.IsValid = false;
                result.Action = "Unknown navigation target. Try 'go to dashboard', 'go to results', 'go to history', or 'go to chat'.";
            }

            return result;
        }

        private VoiceCommandResult ParseUploadCommand(string command)
        {
            return new VoiceCommandResult
            {
                IsValid = true,
                Command = "upload",
                Action = "show_upload_dialog",
                Parameters = { ["message"] = "Please select a file to upload for safety analysis." }
            };
        }

        private VoiceCommandResult ParseSearchCommand(string command)
        {
            var result = new VoiceCommandResult { Command = "search", IsValid = true };

            // Extract search terms (simplified approach)
            var searchTerms = ExtractSearchTerms(command);
            
            result.Action = "perform_search";
            result.Parameters["query"] = searchTerms;
            result.Parameters["message"] = $"Searching for: {searchTerms}";

            return result;
        }

        private VoiceCommandResult ParseHelpCommand(string command)
        {
            return new VoiceCommandResult
            {
                IsValid = true,
                Command = "help",
                Action = "show_help",
                Parameters = { 
                    ["message"] = "Available voice commands: 'go to [page]', 'upload file', 'search for [term]', 'help with [topic]'" 
                }
            };
        }

        private VoiceCommandResult ParseSafetyCommand(string command)
        {
            var result = new VoiceCommandResult { Command = "safety_query", IsValid = true };

            result.Action = "process_safety_question";
            result.Parameters["question"] = command;
            result.Parameters["message"] = "Processing your safety question...";

            return result;
        }

        private bool ContainsSafetyKeywords(string text)
        {
            var safetyKeywords = new[]
            {
                "safety", "hazard", "risk", "accident", "incident", "injury", "emergency",
                "ppe", "osha", "regulation", "procedure", "training", "fire", "chemical"
            };

            return System.Array.Exists(safetyKeywords, keyword => text.Contains(keyword));
        }

        private string ExtractSearchTerms(string command)
        {
            // Simple extraction - remove command words and return the rest
            var commandWords = new[] { "search", "find", "look", "for" };
            var words = command.Split(' ');
            var searchWords = new System.Collections.Generic.List<string>();

            foreach (var word in words)
            {
                if (!System.Array.Exists(commandWords, cmd => word.Contains(cmd)))
                {
                    searchWords.Add(word);
                }
            }

            return string.Join(" ", searchWords).Trim();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _geminiClient?.Dispose();
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