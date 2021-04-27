using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RatingsBreakdown : ContentView
    {
        ObservableCollection<RatingsDetails> Ratings;
        MiniNetwork network;

        public RatingsBreakdown(Show s, MiniNetwork n)
        {
            network = n;
            InitializeComponent();

            ShowInfo.Elapsed += ShowInfo_Elapsed;ShowInfo.Start();

            Load_Ratings(s);
        }

        

        void Load_Ratings(Show s)
        {
            Ratings = new ObservableCollection<RatingsDetails>();
            RatingsList.ItemsSource = Ratings;
            int count = s.ratings.Count;

            if (s.Episodes > count)
                FinalRating.Text = "Projected Final Rating: ";
            else
            {
                FinalRating.Text = "Final Rating: ";
                IsProjected.IsVisible = false;
            }

            FinalRating.Text += s.AverageRating.ToString("N3");

            //await Task.Run(() =>
            //{
            double average = 0;
            long weight = 0;

            for (int i = 0; i < count; i++) //First, add existing ratings
            {
                average = s.ratingsAverages[i];
                weight += (long)Math.Pow(i + 1, 2);
                Ratings.Add(new RatingsDetails(i + 1, s.ratings[i], average, false));
            }

            double FinalAverage = average,
            CurrentDrop = FinalAverage / s.ratings[0];

            if (count < s.Episodes)
                for (int i = count; i < s.Episodes; i++) //Then project future ratings
                {
                    var NewWeight = (long)Math.Pow(i + 1, 2);
                    var NewAverage = FinalAverage * network.AdjustAverage(count, i + 1, CurrentDrop);
                    var NewRating = (NewAverage * (weight + NewWeight) - average * weight) / NewWeight;

                    Ratings.Add(new RatingsDetails(i + 1, NewRating, NewAverage, true));

                    average = NewAverage;
                    weight += NewWeight;
                }
            //});

        }

        Timer ShowInfo = new Timer(1000) { AutoReset = false };

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            ShowInfo.Stop();
            ShowInfo.Start();
        }

        private async void ShowInfo_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                if (RatingsList.Height < 500)
                {
                    Disclaimer.IsVisible = false;
                    Info.IsVisible = true;
                }
                else
                {
                    Disclaimer.IsVisible = true;
                    Info.IsVisible = false;
                }
            });
            
        }

        private async void Info_Clicked(object sender, EventArgs e)
        {
            var stack = NetworkDatabase.mainpage.Navigation.NavigationStack;

            if (stack.Count > 0)
                await stack.Last().DisplayAlert("Ratings Info", "Average values are calculated using a weighted average that weighs newer episodes higher. Each Episode's weight is the square of the episode number. This is done because the ratings a show has at the end of the season are far more important for renewal odds. Future episode ratings and averages are projected using historical data about how ratings tend to drop off for this network. This is done because different shows premiere at different parts of the year. Projecting their final ratings averages allows us to compare their performances despite being at different parts of their respective seasons. This allows for a much more reliable comparison for the purpose of our predictions.", "OK");
            else
                await NetworkDatabase.mainpage.DisplayAlert("Ratings Info", "Average values are calculated using a weighted average that weighs newer episodes higher. Each Episode's weight is the square of the episode number. This is done because the ratings a show has at the end of the season are far more important for renewal odds. Future episode ratings and averages are projected using historical data about how ratings tend to drop off for this network. This is done because different shows premiere at different parts of the year. Projecting their final ratings averages allows us to compare their performances despite being at different parts of their respective seasons. This allows for a much more reliable comparison for the purpose of our predictions.", "OK");

        }
    }

    class RatingsDetails
    {
        public int Episode { get; set; }

        double _rating, _average;
        public string Rating
        {
            get
            {
                return _rating.ToString("N2");
            }
        }
        public string Average
        {
            get
            {
                return _average.ToString("N3");
            }
        }

        Color _color;
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
            }
        }

        public RatingsDetails(int episode, double rating, double average, bool isProjection)
        {
            Episode = episode;
            _rating = rating;
            _average = average;
            Color = isProjection ? Color.IndianRed : (Color)Application.Current.Resources["PageText"];
        }
    }
}