using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TV_Ratings_Predictions;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Settings : ContentView, INotifyPropertyChanged
    {
        public bool UseOdds
        {
            get
            {
                return NetworkDatabase.UseOdds;
            }
            set
            {
                NetworkDatabase.UseOdds = value;
                Application.Current.Properties["UseOdds"] = value;

                if (UseNetwork)
                    network.Predictions.AsParallel().Where(x => x.Count > 0).SelectMany(x => x).ForAll(x => x.OnPropertyChanged("Prediction"));

                if (UsePredictionList)
                    PredictionList.AsParallel().Where(x => x.Count > 0).SelectMany(x => x).ForAll(x => x.OnPropertyChanged("Prediction"));

                if (UsePrediction)
                    prediction.OnPropertyChanged("Prediction");

                if (parent is Predictions)
                {
                    var item = (parent as Predictions).PreviousItem;

                    if (item != null)
                        item.OnPropertyChanged("Prediction");
                }
                else if (parent is ScoreBoard)
                {
                    var item = (parent as ScoreBoard).LastItem;

                    if (item != null)
                        item.OnPropertyChanged("Prediction");
                }
                    
            }
        }

        public int Sort
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("PredictionSort"))
                {
                    switch (Application.Current.Properties["PredictionSort"] as string)
                    {
                        case "Ratings":
                            return 1;
                        case "Name":
                            return 2;
                        default:
                            return 0;
                    }
                }
                else
                    return 0;
            }
            set
            {
                switch (value)
                {
                    case 0:
                        Application.Current.Properties["PredictionSort"] = "Odds";
                        break;
                    case 1:
                        Application.Current.Properties["PredictionSort"] = "Ratings";
                        break;
                    default:
                        Application.Current.Properties["PredictionSort"] = "Name";
                        break;
                }

                foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                    n.pendingFilter = true;

                if (UseNetwork)
                    network.Filter();

                if (UsePredictionList)
                    Predictions.UpdateFilter(ref PredictionList);
            }
        }

        public int Theme
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("Theme"))
                {
                    switch ((string)Application.Current.Properties["Theme"])
                    {
                        case "Dark":
                            return 1;
                        default:
                            return 0;
                    }
                }
                else
                    return 0;
            }
            set
            {
                var MergedDictionaries = Application.Current.Resources.MergedDictionaries;
                MergedDictionaries.Clear();

                switch (value)
                {
                    case 1:
                        Application.Current.Properties["Theme"] = "Dark";
                        MergedDictionaries.Add(new DarkTheme());
                        break;
                    default:
                        Application.Current.Properties["Theme"] = "Light";
                        MergedDictionaries.Add(new LightTheme());
                        break;
                }
            }
        }

        public bool UseHome
        {
            get
            {
                return NetworkDatabase.HomeButton;
            }
            set
            {
                NetworkDatabase.HomeButton = value;
            }
        }

        public double PredictionPrecision
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("PredictionPrecision"))
                    return (double)Application.Current.Properties["PredictionPrecision"];
                else
                    return 1;
            }
            set => Application.Current.Properties["PredictionPrecision"] = value;
        }

        public bool EnableHighlights
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("EnableHighlights"))
                    return (bool)Application.Current.Properties["EnableHighlights"];
                else
                    return true;
            }
            set
            {
                Application.Current.Properties["EnableHighlights"] = value;

                if (!value)
                    NetworkDatabase.mainpage.home.HideHighlights();
                else
                {
                    NetworkDatabase.mainpage.home.UnhideHighlights();
                }                   
            }
        }

        MiniNetwork network;
        bool UseNetwork;

        public PredictionContainer prediction;
        public bool UsePrediction;

        ObservableCollection<ListOfPredictions> PredictionList;
        bool UsePredictionList;

        Page parent;

        public Settings()
        {
            BindingContext = this;

            InitializeComponent();           
        }

        public Settings(Page parent, ObservableCollection<ListOfPredictions> predictions)
        {
            PredictionList = predictions;
            UsePredictionList = true;

            BindingContext = this;
            InitializeComponent();

            this.parent = parent;
        }

        public Settings(PredictionContainer p)
        {
            prediction = p;
            UsePrediction = true;

            BindingContext = this;
            InitializeComponent();
        }

        public Settings(Page page, MiniNetwork n)
        {
            network = n;
            UseNetwork = true;

            BindingContext = this;
            InitializeComponent();

            parent = page;
        }

        public Settings(MiniNetwork n, PredictionContainer p)
        {
            network = n;
            UseNetwork = true;

            prediction = p;
            UsePrediction = true;

            BindingContext = this;
            InitializeComponent();
        }

        private async void ImageButton_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://www.Facebook.com/TVPredictions");
        }

        private async void Refresh_Clicked(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                if (File.Exists(Path.Combine(NetworkDatabase.Folder, "Update.txt")))
                    File.Delete(Path.Combine(NetworkDatabase.Folder, "Update.txt"));

                if (File.Exists(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP")))
                    File.Delete(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP"));

                NetworkDatabase.mainpage.home.Completed = false;

                NetworkDatabase.IsLoaded = false;

                Refresh.Text = "Downloading...";
                await NetworkDatabase.ReadUpdateAsync();
                Refresh.Text = "Refresh Predictions";
            }            
            else
                await (Parent.Parent as Page).DisplayAlert("TV Predictions", "Not Connected to the Internet! Try again later.", "Close");


        }

        private async void Fix_Clicked(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                await (Parent.Parent as Page).Navigation.PushAsync(new ViewPage(new FixShow(), "Fix Show Details"));
            else
                await(Parent.Parent as Page).DisplayAlert("TV Predictions", "Not Connected to the Internet! Try again later.", "Close");
        }

        private void OddsCell_Tapped(object sender, EventArgs e)
        {
            UseOdds = !UseOdds;
            OnPropertyChanged("UseOdds");
        }

        private void HomeCell_Tapped(object sender, EventArgs e)
        {
            UseHome = !UseHome;
            OnPropertyChanged("UseHome");
        }

        private async void PayPalButton_Clicked(object sender, EventArgs e)
        {
            await Launcher.OpenAsync("https://www.paypal.com/signin?returnUri=https%3A%2F%2Fwww.paypal.com%2Fmyaccount%2Ftransfer%2Fhomepage%2Fexternal%2Fprofile%3FflowContextData%3DCQuMuiaY23NnEChz0FosV9Rc4YqntwruJzAT20eYMhGwoAAYMFbzBasPGNF7YMZC6ljkQHejKE8QI_RErKM-rnGqwQ-SPaW00uBiiXT1uB2aZjzGUAnwu1Pynsn288-DHcFLt64Iy63kOr92KIbeYsm4MTndJTEAWhOISqiPlnsVhwWNCdS9Tv-D_pxvv-Olf-mQwZm1tqALRKQBvJQTW1Fy-cu9e5Ol2ckqMYOup1oACbNLxe7rPi6AaR0J3kp0gpzhbx_u68FcCeymnalEPQxxPsTnIXBLADQvdzNcZlybX6KMpGu98wsf3F-iN5VTY4Igl5hDdTnwpNkgpo8rmeX-HXTyMsoMzKWcH5VwHnqYMogd3Xss7Yja0GIoYePGNh-F5DpLYfURmwnifFZCEYmxK8i&onboardData=%7B%22country.x%22%3A%22US%22%2C%22locale.x%22%3A%22en_US%22%2C%22intent%22%3A%22paypalme%22%2C%22redirect_url%22%3A%22https%253A%252F%252Fwww.paypal.com%252Fmyaccount%252Ftransfer%252Fhomepage%252Fexternal%252Fprofile%253FflowContextData%253DCQuMuiaY23NnEChz0FosV9Rc4YqntwruJzAT20eYMhGwoAAYMFbzBasPGNF7YMZC6ljkQHejKE8QI_RErKM-rnGqwQ-SPaW00uBiiXT1uB2aZjzGUAnwu1Pynsn288-DHcFLt64Iy63kOr92KIbeYsm4MTndJTEAWhOISqiPlnsVhwWNCdS9Tv-D_pxvv-Olf-mQwZm1tqALRKQBvJQTW1Fy-cu9e5Ol2ckqMYOup1oACbNLxe7rPi6AaR0J3kp0gpzhbx_u68FcCeymnalEPQxxPsTnIXBLADQvdzNcZlybX6KMpGu98wsf3F-iN5VTY4Igl5hDdTnwpNkgpo8rmeX-HXTyMsoMzKWcH5VwHnqYMogd3Xss7Yja0GIoYePGNh-F5DpLYfURmwnifFZCEYmxK8i%22%2C%22sendMoneyText%22%3A%22You%2520are%2520sending%2520Andy%2520Gilleand%22%7D");
        }

        private async void Log_Clicked(object sender, EventArgs e)
        {
            await (Parent.Parent as Page).Navigation.PushAsync(new ViewPage(new ChangeLog(), "Changelog"));
        }

        private void PrecisionInfo_Clicked(object sender, EventArgs e)
        {
            var message = "The predictions displayed by this app are the result of Neural Network AI. Each prediction considers several different factors set for that show, such as which season #, run time, timeslot, network ownership, syndication, etc. " +
                "However, as this is a neural network, all of these factors are considered at the same time, meaning we don't just add one factor at a time. " +
                "In order to estimate the amount each factor contributes to the total prediction, we need to start with a \"Base Odds\" based solely on the ratings, with the other factors represented by the average of those factors for the network. " +
                "From there, we add each factor one by one until we reach the final prediction. However, when we do this, each factor will give us a different result depending on the order we add those factors back in. " +
                "So in order to estimate the average contribution of each factor, we need to run a LOT of simulations, randomizing the order of the factors, and then averaging the results. " +
                "The default number of simulations is 20,000, which we think should typically provide enough precision to determine an average contribution between -100.00% and +100.00. " +
                "However, this number can potentially cause some devices to be slow, so we've provided the option to increase speed at the cost of precision, or even increase precision beyond the default for fast devices. " +
                "The scale is exponential, so maximum values can end up being millions of samples, with minimums being in the hundreds, depending on how many factors each network has.";

            if (parent is null) parent = NetworkDatabase.mainpage.Detail;

            parent.DisplayAlert("Prediction Breakdown Precision Info", message, "OK");
        }

        private void ResetPrecision_Clicked(object sender, EventArgs e)
        {
            PredictionPrecision = 1;
            OnPropertyChanged("PredictionPrecision");
        }

        private void HighlightsOption_Tapped(object sender, EventArgs e)
        {
            EnableHighlights = !EnableHighlights;

            OnPropertyChanged("EnableHighlights");
        }

        private void FixNotifications_Clicked(object sender, EventArgs e)
        {
            NetworkDatabase.RefreshToken();

            if (parent is null) parent = NetworkDatabase.mainpage.Detail;

            parent.DisplayAlert("Fix Notifications", "Notification Token Refreshed", "OK");
        }
    }
}