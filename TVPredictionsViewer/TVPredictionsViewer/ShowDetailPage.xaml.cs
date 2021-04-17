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
    public partial class ShowDetailPage : ContentPage
    {
        PredictionContainer show;
        //ObservableCollection<ListOfPredictions> Results = new ObservableCollection<ListOfPredictions>();
        public TitleTemplate TitleBar
        {
            get
            {
                return Bar;
            }
        }

        public ShowDetailPage(PredictionContainer p, bool RemoveStackAfterLoad = false)
        {
            var network = p.network;

            if (p.UseNetwork)
                foreach (ToolbarItem t in new Toolbar(this, network, p).ToolBarItems)
                    ToolbarItems.Add(t);
            else
                foreach (ToolbarItem t in new Toolbar(this, p).ToolBarItems)
                    ToolbarItems.Add(t);

            BindingContext = p;
            show = p;
            InitializeComponent();

            Bar.Title = p.Name;

            SearchResults.NavigationParent = this;


            ShowImage.Source = null;
            SideColumn.SizeChanged += SideColumn_SizeChanged;

            LoadImage(p);

            if (RemoveStackAfterLoad)
                Appearing += ShowDetailPage_Appearing;
        }

        private void ShowDetailPage_Appearing(object sender, EventArgs e)
        {
            var stack = Navigation.NavigationStack;
            var count = stack.Count;
            for (int i = count - 2; i > 0; i--)
                Navigation.RemovePage(stack[i]);
        }

        public T FindTemplateElementByName<T>(Page page, string name) where T : Element
        {
            if (!(page is IPageController pc))
            {
                return null;
            }

            foreach (var child in pc.InternalChildren)
            {
                var result = child.FindByName<T>(name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void SideColumn_SizeChanged(object sender, EventArgs e)
        {
            if (SideColumn.Width > 5)
                ImageRow.Height = SideColumn.Width * 9 / 16;
        }

        async void LoadImage(PredictionContainer p)
        {
            //var ID = await NetworkDatabase.GetShowID(p.Name, p.network.name);
            //if (ID > 0)
            //{
            //    ShowImage.Source = new UriImageSource
            //    {
            //        Uri = await NetworkDatabase.GetImageURI(ID),
            //        CachingEnabled = true,
            //        CacheValidity = new TimeSpan(90, 0, 0, 0)
            //    };                    

            //    p.Overview = NetworkDatabase.ShowDescriptions[ID];

            //    if (ShowImage.Source != null)
            //    {
            //        ShowImage.IsVisible = true;
            //        p.IsLoaded = true;
            //        ImageLoading.IsVisible = false;
            //    }

            //}

            bool reload = false;

            if (!NetworkDatabase.ShowIDs.ContainsKey(p.show.Name) && Application.Current.Properties.ContainsKey("SHOWID " + p.show.Name))
                reload = true;

            var ID = await NetworkDatabase.GetShowID(p.show.Name, p.network.name);

            if (ID > 0)
            {
                ShowImage.Source = new UriImageSource
                {
                    Uri = await NetworkDatabase.GetImageURI(ID),
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(90, 0, 0, 0)
                };
                p.Overview = NetworkDatabase.ShowDescriptions[ID];

                if (ShowImage.Source != null)
                {
                    p.IsLoaded = true;
                    ShowImage.IsVisible = true;
                    ImageLoading.IsVisible = false;
                }
            }

            if (reload)
            {
                ID = await NetworkDatabase.GetShowID(p.show.Name, p.network.name, true);

                if (ID > 0)
                {
                    ShowImage.Source = new UriImageSource
                    {
                        Uri = await NetworkDatabase.GetImageURI(ID),
                        CachingEnabled = true,
                        CacheValidity = new TimeSpan(90, 0, 0, 0)
                    };

                    p.Overview = NetworkDatabase.ShowDescriptions[ID];
                }
            }

            var TVDBText = "TV information and images are provided by TheTVDB.com, but we are not endorsed or certified by TheTVDB.com or its affiliates.";
            var Formatted = new FormattedString();
            Formatted.Spans.Add(new Span { Text = TVDBText });

            if (NetworkDatabase.TMDBerror)
                Formatted.Spans.Add(new Span()
                {
                    Text = " Error connecting to TVDB! Some show details and/or images may temporarily be unavailable.",
                    TextColor = Color.DarkRed
                });

            TMDBNotice.FormattedText = Formatted;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            show.IsShowPage = false;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (show != null)
                show.IsShowPage = true;
        }

        protected override bool OnBackButtonPressed()
        {
            if (SearchResults.IsFocused)
            {
                SearchResults.Unfocus();
                return true;
            }
            else if (SearchResults.IsVisible)
            {
                SearchResults.MakeInvisible();
                return true;
            }
            else
                return base.OnBackButtonPressed();
                
        }
    }
}