using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using Android.Speech;
using System;
using System.Linq;

namespace OlymPOS;

    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity Instance { get; private set; }
        public const int VOICE = 10; // Request code for speech
        public override void OnBackPressed()
        {
            // Do nothing or add your custom logic here
            base.OnBackPressed(); // Comment this out to disable the default back press behavior
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;
        }

        public void StartSpeechToText()
        {
            var voiceIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            voiceIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            voiceIntent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak now...");
            voiceIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
            voiceIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);

            // Make sure the activity will not throw if no recognition service is found
            var pm = PackageManager;
            var activities = pm.QueryIntentActivities(voiceIntent, PackageInfoFlags.MatchDefaultOnly);
            if (activities.Count > 0)
            {
                StartActivityForResult(voiceIntent, VOICE);
            }
            else
            {
                Console.WriteLine("etetdttdftdkajguisrcg");
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == VOICE && resultCode == Result.Ok)
            {
                var matches = data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
                if (matches != null && matches.Count > 0)
                {
                    string textInput = matches[0];
                    //Microsoft.Maui.Controls.MessagingCenter.Send(Microsoft.Maui.Controls.Application.Current, "SpeechToText", textInput);
                }
            }
        }
    }

