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
            await Launcher.OpenAsync("https://www.Facebook.com/TVPredictions");
        }

        private async void PayPalButton_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://www.paypal.me/andygilleand");
        }
    }
}