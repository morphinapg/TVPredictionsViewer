using System;
using System.Linq;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FixShow : ContentView
    {
        String show;
        public bool IsInitialized;
        public PredictionContainer prediction;

        public FixShow(PredictionContainer p)
        {
            var DisplayYear = p.DisplayYear;

            p.DisplayYear = false;
            show = p.Name;
            p.DisplayYear = DisplayYear;

            IsInitialized = true;
            prediction = p;

            InitializeComponent();

            ShowName.Text = show;

            LoadSearch();
        }

        public bool IsConfirmationDisplayed
        {
            get
            {
                return Confirmation.IsVisible;
            }
        }

        public FixShow()
        {
        }

        async void LoadSearch()
        {
            var result = await NetworkDatabase.GetSearchResults(show);
            var ShowList = new ObservableCollection<TMDBContainer>();

            TMDBResults.ItemsSource = ShowList;
            TMDBResults.IsVisible = true;

            foreach (TVSearchResult s in result)
            {
                var Series = new TMDBContainer(s.Show, s.Show.BackdropPath, ShowList.Count + 1);
                ShowList.Add(Series);
            }


            Loading.IsVisible = false;
        }

        private void Image_SizeChanged(object sender, EventArgs e)
        {
            var img = sender as Xamarin.Forms.Image;
            var grid = img.Parent as Grid;

            if (grid.Width > 5)
                grid.RowDefinitions[0].Height = grid.Width * 9 / 16;
        }

        private async void TMDBResults_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var item = TMDBResults.SelectedItem as TMDBContainer;

            Confirmation.BindingContext = item;

            Confirmation.IsVisible = true;
            await Confirmation.FadeTo(1);
        }

        public async void No_Clicked(object sender, EventArgs e)
        {
            await Confirmation.FadeTo(0);
            Confirmation.IsVisible = false;
        }

        private async void Yes_Clicked(object sender, EventArgs e)
        {
            var context = Confirmation.BindingContext as TMDBContainer;
            NetworkDatabase.ShowIDs[show] = context.ID;
            NetworkDatabase.ShowDescriptions[context.ID] = context.Description;
            //NetworkDatabase.ShowSlugs[context.ID] = context.Slug;
            NetworkDatabase.IMDBList[context.ID] = "";
            NetworkDatabase.ShowImages[context.ID] = context.BaseImage;
            Application.Current.Properties["SHOWID " + show] = context.ID;
            NetworkDatabase.backup = true;


            var NewestShow = NetworkDatabase.NetworkList.AsParallel().SelectMany(x => x.shows).Where(x => x.Name == show).OrderByDescending(x => x.year).First();
            var Network = NetworkDatabase.NetworkList.Where(x => x.shows.Contains(NewestShow)).First();
            var Adjustments = Network.model.GetAdjustments(true);
            var Average = Network.model.GetAverageThreshold(true);
            var Prediction = new PredictionContainer(NewestShow, Network, Adjustments[NewestShow.year], Average);

            await (Parent.Parent as ViewPage).Navigation.PushModalAsync(new ShowDetailPage(Prediction, true));
            await (Parent.Parent as ViewPage).Navigation.PopAsync();
        }
    }

    class TMDBContainer
    {
        //SeriesSearchResult show;
        TMDbLib.Objects.TvShows.TvShow show;

        public string Name { get { return show.Name; } }
        public string Description { get { return show.Overview; } }
        public int ID { get { return show.Id; } }
        //public string Slug { get { return show.Slug; } }

        string img;

        public UriImageSource ImageUri
        {
            get
            {
                return new UriImageSource
                {
                    Uri = new Uri("https://www.themoviedb.org/t/p/original/" + img),
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(90, 0, 0, 0)
                };
            }
        }

        public string BaseImage
        {
            get
            {
                //if (imgs.Data is null) return null;

                //return imgs.Data.First().FileName;

                return img;
            }
        }

        public String ResultNumber { get; set; }

        public TMDBContainer(TMDbLib.Objects.TvShows.TvShow s, string Image, int num)
        {
            show = s;
            img = Image;
            ResultNumber = "#" + num + ". ";
        }
    }
}