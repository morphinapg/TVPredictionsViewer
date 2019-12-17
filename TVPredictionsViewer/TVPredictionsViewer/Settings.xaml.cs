using Plugin.Connectivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

        MiniNetwork network;
        bool UseNetwork;

        PredictionContainer prediction;
        bool UsePrediction;

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
            if (CrossConnectivity.Current.IsConnected)
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
            if (CrossConnectivity.Current.IsConnected)
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
    }
}