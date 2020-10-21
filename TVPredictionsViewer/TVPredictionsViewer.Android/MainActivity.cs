using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using TV_Ratings_Predictions;
using Android.Graphics;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using CarouselView.FormsPlugin.Android;
using Android.Gms.Common;
using Android.Util;
using Firebase.Iid;
using Firebase.Messaging;
using Xamarin.Forms;

namespace TVPredictionsViewer.Droid
{
    [Activity(Label = "TV Predictions", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            CarouselViewRenderer.Init();
            NetworkDatabase.Folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            LoadApplication(new App());

            IsPlayServiceAvailable();

            //var token = FirebaseInstanceId.Instance.Token; //For testing notifications
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        bool IsPlayServiceAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    Log.Debug("XamarinNotify", GoogleApiAvailability.Instance.GetErrorString(resultCode));
                else
                {
                    Log.Debug("XamarinNotify", "This device is not supported");
                }
                return false;
            }
            return true;
        }
    }

    [Service]
    [IntentFilter(new[] {  "com.google.firebase.INSTANCE_ID_EVENT"})]
    public class MyFirebaseIDService : FirebaseMessagingService
    {
        public override void OnNewToken(string s)
        {
            var refreshedToken = s;
            Console.WriteLine($"Token received: {refreshedToken}");
            SendRegistrationToServer(refreshedToken);           
        }

        void SendRegistrationToServer(string token)
        {
            //We'll do this later
        }
    }


    //Messaging Service - If received while app is open, refresh predictions
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT"})]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            Console.WriteLine("Received: " + message);

            try
            {
                var msg = message.GetNotification().Body;

                MessagingCenter.Send<object, string>(this, App.NotificationReceivedKey, msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error extracting message: " + ex);
            }
        }
    }

}