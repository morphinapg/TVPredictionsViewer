using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TvDbSharper.Dto;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Plugin.Connectivity;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FixShow : ContentView
    {
        String show;
        public bool IsInitialized;

        public FixShow(PredictionContainer p)
        {
            var DisplayYear = p.DisplayYear;

            p.DisplayYear = false;
            show = p.Name;
            p.DisplayYear = DisplayYear;

            IsInitialized = true;

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
            var ShowList = new ObservableCollection<TVDBContainer>();

            TVDBResults.ItemsSource = ShowList;
            TVDBResults.IsVisible = true;

            foreach (SeriesSearchResult s in result.Data)
            {
                TvDbResponse<TvDbSharper.Dto.Image[]> Images;

                try
                {
                    Images = await GetImages(s.Id);
                }
                catch (Exception)
                {
                    Images = new TvDbResponse<TvDbSharper.Dto.Image[]>();
                }

                var Series = new TVDBContainer(s, Images, ShowList.Count + 1);
                ShowList.Add(Series);
            }


            Loading.IsVisible = false;
        }

        async Task<TvDbResponse<TvDbSharper.Dto.Image[]>> GetImages(int ID)
        {
            return await NetworkDatabase.client.Series.GetImagesAsync(ID, new ImagesQuery() { KeyType = KeyType.Series });
        }

        private void Image_SizeChanged(object sender, EventArgs e)
        {
            var img = sender as Xamarin.Forms.Image;
            var grid = img.Parent as Grid;

            if (grid.Width > 5)
                grid.RowDefinitions[0].Height = grid.Width * 140 / 758;
        }

        private async void TVDBResults_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var item = TVDBResults.SelectedItem as TVDBContainer;

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
            var context = Confirmation.BindingContext as TVDBContainer;
            NetworkDatabase.ShowIDs[show] = context.ID;
            NetworkDatabase.ShowDescriptions[context.ID] = context.Description;
            NetworkDatabase.ShowSlugs[context.ID] = context.Slug;
            NetworkDatabase.IMDBList[context.ID] = "";
            NetworkDatabase.ShowImages[context.ID] = context.BaseImage;
            Application.Current.Properties["SHOWID " + show] = context.ID;
            NetworkDatabase.backup = true;


            var NewestShow = NetworkDatabase.NetworkList.AsParallel().SelectMany(x => x.shows).Where(x => x.Name == show).OrderByDescending(x => x.year).First();
            var Network = NetworkDatabase.NetworkList.Where(x => x.shows.Contains(NewestShow)).First();
            var Adjustments = Network.model.GetAdjustments(true);
            var Average = Network.model.GetAverageThreshold(true);
            var Prediction = new PredictionContainer(NewestShow, Network, Adjustments[NewestShow.year], Average);

            await (Parent.Parent as ViewPage).Navigation.PushAsync(new ShowDetailPage(Prediction, true));           
            
        }
    }

    class TVDBContainer
    {
        SeriesSearchResult show;

        public string Name { get { return show.SeriesName; } }
        public string Description { get { return show.Overview; } }
        public int ID { get { return show.Id; } }
        public string Slug { get { return show.Slug; } }

        TvDbResponse<TvDbSharper.Dto.Image[]> imgs;

        public UriImageSource ImageUri
        {
            get
            {
                if (imgs.Data is null) return null;

                return new UriImageSource
                {
                    Uri = new Uri("https://artworks.thetvdb.com/banners/" + imgs.Data.First().FileName),
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(90, 0, 0, 0)
                };
            }
        }

        public string BaseImage
        {
            get
            {
                if (imgs.Data is null) return null;

                return imgs.Data.First().FileName;
            }
        }

        public String ResultNumber { get; set; }

        public TVDBContainer(TvDbSharper.Dto.SeriesSearchResult s, TvDbResponse<TvDbSharper.Dto.Image[]> Images, int num)
        {
            show = s;
            imgs = Images;
            ResultNumber = "#" + num + ". ";
        }
    }
}