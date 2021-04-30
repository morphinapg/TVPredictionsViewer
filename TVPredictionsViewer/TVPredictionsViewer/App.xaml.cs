using TV_Ratings_Predictions;
using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

[assembly: ExportFont("seguisym.ttf", Alias = "Segoe UI")]
namespace TVPredictionsViewer
{    
    public partial class App : Application
    {
        public const string NotificationReceivedKey = "NotificationReceived";

        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            NetworkDatabase.InBackground = false;
            // Handle when your app starts
            AppCenter.Start("android=5bff64d6-b3ee-45b1-9c79-b560516f6659;"+
                "uwp=5cbe47d7-e007-4ef2-ba64-285b17cf5233;"
                , typeof(Analytics), typeof(Crashes));

            Analytics.TrackEvent("App Load");
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            NetworkDatabase.InBackground = true;
        }

        protected override void OnResume()
        {
            NetworkDatabase.InBackground = false;

            // Handle when your app resumes
            if (!(NetworkDatabase.mainpage is null))
                NetworkDatabase.mainpage.CheckForUpdate(true);

        }
    }
}
