using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using TV_Ratings_Predictions;
using Android.Graphics;
using Android.Gms.Common;
using Android.Util;
using Firebase.Messaging;
using Xamarin.Forms;
using WindowsAzure.Messaging;
using System.Linq;
using Android.Content;
using AndroidX.Core.App;

namespace TVPredictionsViewer.Droid
{
    [Activity(Label = "TV Predictions", Icon = "@mipmap/icon", RoundIcon = "@mipmap/icon_round", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode= LaunchMode.SingleTop)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public const string TAG = "MainActivity";
        internal static readonly string CHANNEL_ID = "my_notification_channel";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            //CarouselViewRenderer.Init();
            NetworkDatabase.Folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            LoadApplication(new App());

            IsPlayServiceAvailable();

            CreateNotificationChannel();

//#if DEBUG
//            Task.Run(() =>
//            {
//                FirebaseInstanceId.Instance.DeleteInstanceId();
//                Console.WriteLine("Forced token: " + FirebaseInstanceId.Instance.Token);
//            });
//#endif

            //var token = FirebaseInstanceId.Instance.Token; //For testing notifications
        }

        protected override void OnNewIntent(Intent intent)
        {
            MessagingCenter.Send<object, string>(this, App.NotificationReceivedKey, "IntentTap");
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

        void CreateNotificationChannel()
        {
            // Notification channels are new as of "Oreo".
            // There is no need to create a notification channel on older versions of Android.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelName = Constants.NotificationChannelName;
                var channelDescription = String.Empty;
                var channel = new NotificationChannel(channelName, channelName, NotificationImportance.Default)
                {
                    Description = channelDescription
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
    }

    [Service]
    [IntentFilter(new[] {  "com.google.firebase.INSTANCE_ID_EVENT"})]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        //const string TAG = "MyFirebaseMsgService";

        public override void OnNewToken(string s)
        {
            var refreshedToken = s;
            Console.WriteLine($"Token received: {refreshedToken}");
            SendRegistrationToServer(refreshedToken);           
        }

        void SendRegistrationToServer(string token)
        {
            ////We'll do this later
            //hub = new NotificationHub(Constants.NotificationHubName, Constants.ListenConnectionString, this);

            //var tags = new List<string>() { };
            //var regID = hub.Register(token, tags.ToArray()).RegistrationId;

            //Log.Debug(TAG, $"Successful registration of ID {regID}");

            try
            {
                NotificationHub hub = new NotificationHub(Constants.NotificationHubName, Constants.ListenConnectionString, this);

                // register device with Azure Notification Hub using the token from FCM
                Registration registration = hub.Register(token, Constants.SubscriptionTags);

                // subscribe to the SubscriptionTags list with a simple template.
                string pnsHandle = registration.PNSHandle;
                TemplateRegistration templateReg = hub.RegisterTemplate(pnsHandle, "defaultTemplate", Constants.FCMTemplateBody, Constants.SubscriptionTags);
            }
            catch (Exception e)
            {
                Log.Error(Constants.DebugTag, $"Error registering device: {e.Message}");
            }
        }

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            Console.WriteLine("Received: " + message);
            string messageBody = (message.GetNotification() != null) ? 
                message.GetNotification().Body : 
                message.Data.Values.First(); // NOTE: test messages sent via the Azure portal will be received here

            if (NetworkDatabase.InBackground)
                SendLocalNotification(messageBody); 
            else
                MessagingCenter.Send<object, string>(this, App.NotificationReceivedKey, messageBody);
        }

        void SendLocalNotification(string body)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.PutExtra("message", body);

            //Unique request code to avoid PendingIntent collision.
            var requestCode = new Random().Next();
            var pendingIntent = PendingIntent.GetActivity(this, requestCode, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new NotificationCompat.Builder(this, Constants.NotificationChannelName)
                .SetContentTitle("New TV Predictions!")
                .SetSmallIcon(Resource.Mipmap.launcher_foreground)
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.icon))
                .SetColor(Resource.Color.colorAccent)
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetShowWhen(false)
                .SetContentIntent(pendingIntent);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                notificationBuilder.SetChannelId(Constants.NotificationChannelName);
            }

            var notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }

}