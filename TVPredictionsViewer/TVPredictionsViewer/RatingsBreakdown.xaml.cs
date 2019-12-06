using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

                var FinalAverage = average;

                if (count < s.Episodes)
                    for (int i = count; i < s.Episodes; i++) //Then project future ratings
                    {
                        var NewWeight = (long)Math.Pow(i + 1, 2);
                        var NewAverage = FinalAverage * network.AdjustAverage(count, i + 1);
                        var NewRating = (NewAverage * (weight + NewWeight) - average * weight) / NewWeight;

                        Ratings.Add(new RatingsDetails(i + 1, NewRating, NewAverage, true));

                        average = NewAverage;
                        weight += NewWeight;
                    }
            //});
            
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