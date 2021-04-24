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
            await Launcher.OpenAsync("https://www.paypal.me/andygilleand");
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
    }
}