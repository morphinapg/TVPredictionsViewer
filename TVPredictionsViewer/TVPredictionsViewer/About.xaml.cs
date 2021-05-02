using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TV_Ratings_Predictions;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class About : ContentView
    {
        public bool UsePrediction;
        public PredictionContainer prediction;
        public About()
        {
            InitializeComponent();
        }

        public About(PredictionContainer p)
        {
            UsePrediction = true;
            prediction = p;
            InitializeComponent();
        }

        private async void ImageButton_Clicked(object sender, EventArgs e)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                try
                {
                    await Launcher.OpenAsync("fb://page/609673106087592/");
                }
                catch (Exception)
                {
                    await Launcher.OpenAsync("https://www.Facebook.com/TVPredictions");
                }
            }
            else
                await Launcher.OpenAsync("https://www.Facebook.com/TVPredictions");
        }

        private async void PayPalButton_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://www.paypal.com/signin?returnUri=https%3A%2F%2Fwww.paypal.com%2Fmyaccount%2Ftransfer%2Fhomepage%2Fexternal%2Fprofile%3FflowContextData%3DCQuMuiaY23NnEChz0FosV9Rc4YqntwruJzAT20eYMhGwoAAYMFbzBasPGNF7YMZC6ljkQHejKE8QI_RErKM-rnGqwQ-SPaW00uBiiXT1uB2aZjzGUAnwu1Pynsn288-DHcFLt64Iy63kOr92KIbeYsm4MTndJTEAWhOISqiPlnsVhwWNCdS9Tv-D_pxvv-Olf-mQwZm1tqALRKQBvJQTW1Fy-cu9e5Ol2ckqMYOup1oACbNLxe7rPi6AaR0J3kp0gpzhbx_u68FcCeymnalEPQxxPsTnIXBLADQvdzNcZlybX6KMpGu98wsf3F-iN5VTY4Igl5hDdTnwpNkgpo8rmeX-HXTyMsoMzKWcH5VwHnqYMogd3Xss7Yja0GIoYePGNh-F5DpLYfURmwnifFZCEYmxK8i&onboardData=%7B%22country.x%22%3A%22US%22%2C%22locale.x%22%3A%22en_US%22%2C%22intent%22%3A%22paypalme%22%2C%22redirect_url%22%3A%22https%253A%252F%252Fwww.paypal.com%252Fmyaccount%252Ftransfer%252Fhomepage%252Fexternal%252Fprofile%253FflowContextData%253DCQuMuiaY23NnEChz0FosV9Rc4YqntwruJzAT20eYMhGwoAAYMFbzBasPGNF7YMZC6ljkQHejKE8QI_RErKM-rnGqwQ-SPaW00uBiiXT1uB2aZjzGUAnwu1Pynsn288-DHcFLt64Iy63kOr92KIbeYsm4MTndJTEAWhOISqiPlnsVhwWNCdS9Tv-D_pxvv-Olf-mQwZm1tqALRKQBvJQTW1Fy-cu9e5Ol2ckqMYOup1oACbNLxe7rPi6AaR0J3kp0gpzhbx_u68FcCeymnalEPQxxPsTnIXBLADQvdzNcZlybX6KMpGu98wsf3F-iN5VTY4Igl5hDdTnwpNkgpo8rmeX-HXTyMsoMzKWcH5VwHnqYMogd3Xss7Yja0GIoYePGNh-F5DpLYfURmwnifFZCEYmxK8i%22%2C%22sendMoneyText%22%3A%22You%2520are%2520sending%2520Andy%2520Gilleand%22%7D");
        }
    }
}