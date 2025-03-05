using System;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using OlymPOS.Services.Interfaces;
using Speech;
using UIKit;

namespace OlymPOS.Platforms.iOS
{
    public class SpeechToTextImplementation : ISpeechToTextService
    {
        private readonly WeakEventManager _eventManager = new WeakEventManager();
        private SFSpeechRecognizer _speechRecognizer;
        private SFSpeechAudioBufferRecognitionRequest _recognitionRequest;
        private SFSpeechRecognitionTask _recognitionTask;
        private AVAudioEngine _audioEngine;
        private bool _isRecording;

        public event EventHandler<string> SpeechRecognized
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public bool IsSupported => true;

        public SpeechToTextImplementation()
        {
            _speechRecognizer = new SFSpeechRecognizer(NSLocale.CurrentLocale);
            _audioEngine = new AVAudioEngine();
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                // Check authorization status
                var status = await RequestAuthorizationAsync();
                if (status != SFSpeechRecognizerAuthorizationStatus.Authorized)
                {
                    OnSpeechRecognized($"Speech recognition not authorized: {status}");
                    return null;
                }

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
                    // Timed out, stop recording and remove the handler
                    StopRecording();
                    SpeechRecognized -= handler;
                    return null;
                }

                // Return the result
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speech recognition error: {ex.Message}");
                StopRecording();
                return null;
            }
        }

        private async Task<SFSpeechRecognizerAuthorizationStatus> RequestAuthorizationAsync()
        {
            var tcs = new TaskCompletionSource<SFSpeechRecognizerAuthorizationStatus>();

            SFSpeechRecognizer.RequestAuthorization(status =>
            {
                tcs.SetResult(status);
            });

            return await tcs.Task;
        }

        public void StartSpeechToText()
        {
            if (_isRecording)
            {
                StopRecording();
                return;
            }

            try
            {
                // Cancel any existing recognition task
                _recognitionTask?.Cancel();
                _recognitionTask = null;

                // Configure the audio session
                var audioSession = AVAudioSession.SharedInstance();
                NSError error;
                audioSession.SetCategory(AVAudioSessionCategory.Record, out error);
                if (error != null)
                {
                    OnSpeechRecognized($"Audio session error: {error.LocalizedDescription}");
                    return;
                }

                audioSession.SetMode(AVAudioSessionMode.Measurement, out error);
                if (error != null)
                {
                    OnSpeechRecognized($"Audio session error: {error.LocalizedDescription}");
                    return;
                }

                audioSession.SetActive(true, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out error);
                if (error != null)
                {
                    OnSpeechRecognized($"Audio session error: {error.LocalizedDescription}");
                    return;
                }

                // Create a new recognition request
                _recognitionRequest = new SFSpeechAudioBufferRecognitionRequest();
                if (_recognitionRequest == null)
                {
                    OnSpeechRecognized("Unable to create recognition request");
                    return;
                }

                // Set up the audio input
                var inputNode = _audioEngine.InputNode;
                if (inputNode == null)
                {
                    OnSpeechRecognized("Audio engine has no input node");
                    return;
                }

                // Configure the request for partial results
                _recognitionRequest.ShouldReportPartialResults = true;

                // Start the recognition task
                _recognitionTask = _speechRecognizer.GetRecognitionTask(_recognitionRequest, (result, taskError) =>
                {
                    bool isFinal = false;

                    if (result != null)
                    {
                        // Get the recognized text
                        string recognizedText = result.BestTranscription.FormattedString;
                        isFinal = result.Final;

                        // Send partial results
                        if (!string.IsNullOrEmpty(recognizedText))
                        {
                            OnSpeechRecognized(recognizedText);
                        }
                    }

                    // If there was an error or it's the final result, stop recording
                    if (taskError != null || isFinal)
                    {
                        StopRecording();
                    }
                });

                // Configure the audio buffer
                var recordingFormat = inputNode.GetBusOutputFormat(0);
                inputNode.InstallTapOnBus(0, 1024, recordingFormat, (buffer, when) =>
                {
                    _recognitionRequest?.Append(buffer);
                });

                // Start recording
                _audioEngine.Prepare();
                _audioEngine.StartAndReturnError(out error);
                if (error != null)
                {
                    OnSpeechRecognized($"Audio engine error: {error.LocalizedDescription}");
                    return;
                }

                _isRecording = true;
            }
            catch (Exception ex)
            {
                StopRecording();
                OnSpeechRecognized($"Error: {ex.Message}");
            }
        }

        private void StopRecording()
        {
            if (!_isRecording)
                return;

            // Stop the audio engine
            _audioEngine.Stop();
            _audioEngine.InputNode?.RemoveTapOnBus(0);

            // End the recognition request
            _recognitionRequest?.EndAudio();
            _recognitionRequest = null;

            // Cancel the recognition task
            _recognitionTask?.Cancel();
            _recognitionTask = null;

            _isRecording = false;
        }

        public void OnSpeechRecognized(string result)
        {
            _eventManager.HandleEvent(this, result, nameof(SpeechRecognized));
        }
    }
}