using Android.Content;
using Android.Speech;
using Microsoft.Maui.Controls;
using OlymPOS.Services.Interfaces;
using System;

namespace OlymPOS.Platforms.Android
{
    public class SpeechToTextImplementation : ISpeechToTextService
    {
        private readonly WeakEventManager _eventManager = new WeakEventManager();
        private const int VOICE_REQUEST_CODE = 10;

        public event EventHandler<string> SpeechRecognized
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public bool IsSupported => true;

        public async Task<string> RecognizeSpeechAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                // Set up a handler for the speech recognition result
                EventHandler<string> handler = null;
                handler = (sender, result) =>
                {
                    // Remove the handler to avoid memory leaks
                    SpeechRecognized -= handler;

                    // Complete the task with the result
                    tcs.TrySetResult(result);
                };

                // Subscribe to the speech recognized event
                SpeechRecognized += handler;

                // Start the speech recognition
                StartSpeechToText();

                // Set a timeout for the speech recognition (10 seconds)
                var timeoutTask = Task.Delay(10000);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timed out, remove the handler and return null
                    SpeechRecognized -= handler;
                    return null;
                }

                // Return the result
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speech recognition error: {ex.Message}");
                return null;
            }
        }

        public void StartSpeechToText()
        {
            var activity = MainActivity.Instance;
            if (activity == null)
            {
                OnSpeechRecognized("Error: MainActivity not found");
                return;
            }

            try
            {
                // Create the speech intent
                var voiceIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                voiceIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                voiceIntent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak now...");
                voiceIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                voiceIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);

                // Check if there's a recognition service available
                var pm = activity.PackageManager;
                var activities = pm.QueryIntentActivities(voiceIntent, Android.Content.PM.PackageInfoFlags.MatchDefaultOnly);

                if (activities.Count > 0)
                {
                    // Start the activity for the speech intent
                    activity.StartActivityForResult(voiceIntent, VOICE_REQUEST_CODE);
                }
                else
                {
                    OnSpeechRecognized("Error: No speech recognition service available");
                }
            }
            catch (Exception ex)
            {
                OnSpeechRecognized($"Error: {ex.Message}");
            }
        }

        // This method is called by MainActivity when the speech recognition result is available
        public void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == VOICE_REQUEST_CODE && resultCode == Result.Ok)
            {
                var matches = data?.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                if (matches != null && matches.Count > 0)
                {
                    string recognizedText = matches[0];
                    OnSpeechRecognized(recognizedText);
                }
                else
                {
                    OnSpeechRecognized(string.Empty);
                }
            }
            else
            {
                OnSpeechRecognized(string.Empty);
            }
        }

        public void OnSpeechRecognized(string result)
        {
            _eventManager.HandleEvent(this, result, nameof(SpeechRecognized));
        }
    }
}

