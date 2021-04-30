using Android.App;
using Android.Gms.Tasks;
using Android.Util;
using Firebase.Installations;
using System;
using WindowsAzure.Messaging;
using Xamarin.Forms;

namespace TVPredictionsViewer.Droid
{   

    public class AppFirebaseMessagingRequestToken : Activity, IOnCompleteListener
    {
        public void OnComplete(Task task)
        {
            if (task?.Result != null)
            {
                if (!(task.Result is InstallationTokenResult tokenResult))
                    return;

                var token = tokenResult.Token;
                App.Current.Properties["Token"] = token;

                Log.Debug(nameof(AppFirebaseMessagingRequestToken), "Token: " + token);

                SendRegistrationToServer(token);
            }
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
    }
}