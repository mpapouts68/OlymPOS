using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.Services
{
    public class SpeechToTextService : ISpeechToTextService
    {
        private readonly WeakEventManager _eventManager = new WeakEventManager();

        public event EventHandler<string> SpeechRecognized
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public bool IsSupported
        {
            get
            {
                // Android and iOS both support speech recognition
                // Windows requires extra packages
#if ANDROID || IOS
                return true;
#else
                return false;
#endif
            }
        }

        public async Task<string> RecognizeSpeechAsync()
        {
            try
            {
                // Check for microphone permission
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted)
                    {
                        throw new Exception("Microphone permission denied");
                    }
                }

                // Use platform specific implementation
#if ANDROID
                return await RecognizeSpeechAndroidAsync();
#elif IOS
                return await RecognizeSpeechIOSAsync();
#else
                throw new PlatformNotSupportedException("Speech recognition is not supported on this platform");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speech recognition error: {ex.Message}");
                return null;
            }
        }

#if ANDROID
        private async Task<string> RecognizeSpeechAndroidAsync()
        {
            // Get the MainActivity
            var activity = Platform.CurrentActivity as Android.App.Activity;
            if (activity == null)
                throw new InvalidOperationException("MainActivity not found");
                
            var tcs = new TaskCompletionSource<string>();
            
            // Setup the listener for results
            EventHandler<string> handler = null;
            handler = (sender, result) =>
            {
                // Remove the handler to avoid memory leaks
                SpeechRecognized -= handler;
                
                // Complete the task
                tcs.TrySetResult(result);
            };
            
            // Register for results
            SpeechRecognized += handler;
            
            // Start speech recognition using the platform implementation
            if (activity is MainActivity mainActivity)
            {
                mainActivity.StartSpeechToText();
            }
            else
            {
                // Use DependencyService as fallback
                DependencyService.Get<ISpeechToText>()?.StartSpeechToText();
            }
            
            // Wait for result with timeout
            var timeoutTask = Task.Delay(10000); // 10 second timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                SpeechRecognized -= handler;
                throw new TimeoutException("Speech recognition timed out");
            }
            
            return await tcs.Task;
        }
#endif

#if IOS
        private async Task<string> RecognizeSpeechIOSAsync()
        {
            // iOS implementation would go here
            // For now, using DependencyService
            var tcs = new TaskCompletionSource<string>();

            // Setup the listener for results
            EventHandler<string> handler = null;
            handler = (sender, result) =>
            {
                // Remove the handler to avoid memory leaks
                SpeechRecognized -= handler;

                // Complete the task
                tcs.TrySetResult(result);
            };

            // Register for results
            SpeechRecognized += handler;

            // Start speech recognition using the DependencyService
            DependencyService.Get<ISpeechToText>()?.StartSpeechToText();

            // Wait for result with timeout
            var timeoutTask = Task.Delay(10000); // 10 second timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                SpeechRecognized -= handler;
                throw new TimeoutException("Speech recognition timed out");
            }

            return await tcs.Task;
        }
#endif

        // Method used by platform implementations to report results
        public void OnSpeechRecognized(string result)
        {
            _eventManager.HandleEvent(this, result, nameof(SpeechRecognized));
        }
    }

    // Simple implementation of weak event manager to avoid memory leaks
    internal class WeakEventManager
    {
        private readonly Dictionary<string, List<WeakReference>> _eventHandlers =
            new Dictionary<string, List<WeakReference>>();

        public void AddEventHandler(Delegate handler, [CallerMemberName] string eventName = null)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers = new List<WeakReference>();
                _eventHandlers[eventName] = handlers;
            }

            handlers.Add(new WeakReference(handler));
        }

        public void RemoveEventHandler(Delegate handler, [CallerMemberName] string eventName = null)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var reference = handlers[i];
                var target = reference.Target as Delegate;

                if (target == null || target.Equals(handler))
                {
                    handlers.RemoveAt(i);
                }
            }
        }

        public void HandleEvent(object sender, object args, string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));

            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var reference = handlers[i];
                var target = reference.Target as Delegate;

                if (target == null)
                {
                    handlers.RemoveAt(i);
                }
                else
                {
                    var parameters = new[] { sender, args };
                    target.DynamicInvoke(parameters);
                }
            }
        }
    }
}