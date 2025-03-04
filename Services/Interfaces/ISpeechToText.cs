using System;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface ISpeechToTextService
    {
        event EventHandler<string> SpeechRecognized;
        Task<string> RecognizeSpeechAsync();
        bool IsSupported { get; }
        void OnSpeechRecognized(string result);
    }
}
