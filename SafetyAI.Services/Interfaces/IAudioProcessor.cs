using System.IO;
using System.Threading.Tasks;
using SafetyAI.Models.DTOs;

namespace SafetyAI.Services.Interfaces
{
    public interface IAudioProcessor
    {
        Task<AudioProcessingResult> ProcessAudioAsync(Stream audioStream, string contentType);
        Task<bool> ValidateAudioQualityAsync(Stream audioStream);
        Task<VoiceCommandResult> ProcessVoiceCommandAsync(string audioText);
    }
}