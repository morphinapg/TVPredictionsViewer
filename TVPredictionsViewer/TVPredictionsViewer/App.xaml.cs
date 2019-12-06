using TV_Ratings_Predictions;
using Xamarin.Forms;

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
