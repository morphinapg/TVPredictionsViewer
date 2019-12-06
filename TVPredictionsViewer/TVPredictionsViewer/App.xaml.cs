using TV_Ratings_Predictions;
using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Push;

namespace TVPredictionsViewer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            AppCenter.Start("android=5bff64d6-b3ee-45b1-9c79-b560516f6659;"+
                "uwp=5cbe47d7-e007-4ef2-ba64-285b17cf5233;"
                , typeof(Analytics), typeof(Crashes), typeof(Push));
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            if (!(NetworkDatabase.mainpage is null))
                NetworkDatabase.mainpage.CheckForUpdate(true);
        }
    }
}
