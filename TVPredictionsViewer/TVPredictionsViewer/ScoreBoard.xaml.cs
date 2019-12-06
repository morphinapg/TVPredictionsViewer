using CarouselView.FormsPlugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class ScoreBoard : ContentPage, INotifyPropertyChanged
    {
        CarouselViewControl YearList = new CarouselViewControl();
        ActivityIndicator Activity = new ActivityIndicator();
        ResultsList SearchResults = new ResultsList();
        public TitleTemplate TitleBar => Bar;
        Timer timer;
        PredictionContainer LastItem;
        bool isDesktop = false;
        double height;

        List<Year> FilteredYearList;

        ObservableCollection<ListOfPredictions> Predictions = new ObservableCollection<ListOfPredictions>();

        bool _filtered;
        public bool Filtered
        {
            get
            {
                return _filtered;
            }
            set
            {
                _filtered = value;
                OnPropertyChanged("Filtered");
                if (Predictions.Count > 0 && SelectedNetwork > -1) LoadPredictions();
            }
        }

        MiniNetwork network;
        public int SelectedNetwork
        {
            get
            {
                if (network is null)
                    return -1;

                return NetworkDatabase.NetworkList.IndexOf(network);
            }
            set
            {
                Filtered = true;
                network = NetworkDatabase.NetworkList[value];

                if (Predictions.Count > 0 && SelectedNetwork > -1) LoadPredictions();
            }
        }

        bool _years;
        public bool AllYears
        {
            get
            {
                return _years;
            }
            set
            {
                _years = value;
                OnPropertyChanged("AllYears");

                if (Predictions.Count > 0) LoadPredictions();
            }
        }

        bool _model;
        public bool CurrentModel
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                OnPropertyChanged("CurrentModel");
                if (Predictions.Count > 0) LoadPredictions();
            }
        }

        public List<string> NetworkList { get { return NetworkDatabase.NetworkList.Select(x => x.Name).ToList(); } }

        public ScoreBoard(MiniNetwork n = null)
        {
            foreach (ToolbarItem t in new Toolbar(this).ToolBarItems)
                ToolbarItems.Add(t);

            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;

            FilteredYearList = NetworkDatabase.YearList.Where(x => NetworkDatabase.NetworkList.AsParallel().SelectMany(y => y.shows).Where(y => y.year == x).Where(y => y.FinalPrediction > 0 && (y.Renewed || y.Canceled)).Count() > 0).ToList();

            network = n;

            InitializeComponent();

            BindingContext = this;

            YearList = FindTemplateElementByName<CarouselViewControl>(this, "YearList");
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            SearchResults = FindTemplateElementByName<ResultsList>(this, "SearchResults");
            SearchResults.NavigationParent = this;

            YearList.ItemsSource = FilteredYearList;
            YearList.IsVisible = true;
            //YearList.Position = FilteredYearList.Count - 1;

            YearList.PositionSelected += YearList_PositionSelected;

            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;
            NetworkDatabase_CurrentYearUpdated(this, new EventArgs());
            NetworkDatabase.CurrentYear = NetworkDatabase.YearList.IndexOf(FilteredYearList[YearList.Position]);

            Activity.IsRunning = false;
            Activity.IsVisible = false;

            SideColumn.SizeChanged += SideColumn_SizeChanged;

            var tapped = new TapGestureRecognizer();
            tapped.Tapped += Network_Tapped;
            NetworkLayout.GestureRecognizers.Add(tapped);

            tapped = new TapGestureRecognizer();
            tapped.Tapped += Year_Tapped;
            YearLayout.GestureRecognizers.Add(tapped);

            tapped = new TapGestureRecognizer();
            tapped.Tapped += Model_Tapped;
            ModelLayout.GestureRecognizers.Add(tapped);

            Appearing += ScoreBoard_Appearing;
        }

        private void SideColumn_SizeChanged(object sender, EventArgs e)
        {
            if (SideColumn.Width > 5)
                ImageRow.Height = SideColumn.Width * 140 / 758;
        }

        private void ScoreBoard_Appearing(object sender, EventArgs e)
        {
            //Because we will be loading predictions, we must remove other prediction pages from memory
            Navigation.NavigationStack.Where(x => x is Predictions).ToList().ForEach(x => Navigation.RemovePage(x));
            LoadPredictions();
        }

        private void Model_Tapped(object sender, EventArgs e)
        {
            CurrentModel = !CurrentModel;
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
            else if (SidePanel.IsVisible && SidePanel.BreakdownView != null && SidePanel.BreakdownView.Opacity > 0)
            {
                FadeOut();
                return true;
            }
            else
                return base.OnBackButtonPressed();
        }

        async void FadeOut()
        {
            await SidePanel.BreakdownView.FadeTo(0);
            Grid1.Children.Remove(SidePanel.BreakdownView);
            SidePanel.BreakdownView = null;
        }

        private void Year_Tapped(object sender, EventArgs e)
        {
            AllYears = !AllYears;
        }

        private void Network_Tapped(object sender, EventArgs e)
        {
            Filtered = !Filtered;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            Device.BeginInvokeOnMainThread(() =>
            {
                var oldyear = NetworkDatabase.CurrentYear;
                NetworkDatabase.CurrentYear = NetworkDatabase.YearList.IndexOf(FilteredYearList[YearList.Position]);
                if (!AllYears && Predictions.Count > 0 && NetworkDatabase.CurrentYear != oldyear) LoadPredictions();
            });

        }

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            var CurrentYear = NetworkDatabase.YearList[NetworkDatabase.CurrentYear];

            if (FilteredYearList.Contains(CurrentYear))
                YearList.Position = FilteredYearList.IndexOf(CurrentYear);
            else if (FilteredYearList.Where(x => x.year < CurrentYear.year).Count() > 0)
                YearList.Position = FilteredYearList.Count - 1;
            else
                YearList.Position = 0;
        }

        private void YearList_PositionSelected(object sender, PositionSelectedEventArgs e)
        {
            if (Navigation.NavigationStack.LastOrDefault() == this && YearList.Position > -1)
            {
                timer.Stop();
                timer.Start();
            }
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

        protected override async void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            // fix for carouselview orientation bug on android
            if (Device.RuntimePlatform == Device.Android)
            {
                YearList.Orientation =
                    CarouselViewOrientation.Vertical;
                YearList.Orientation =
                    CarouselViewOrientation.Horizontal;
            }

            var tmpDesktop = isDesktop;
            isDesktop = width > (1080);
            SideColumn.IsVisible = isDesktop;

            if (isDesktop != tmpDesktop)
            {
                if (isDesktop)
                {
                    FirstColumn.Width = new GridLength(1, GridUnitType.Auto);
                    SecondColumn.Width = new GridLength(1, GridUnitType.Star);
                    if (LastItem != null)
                    {
                        LastItem.ShowDetails = false;
                        ShowImage.Source = null;
                        ImageLoading.IsVisible = true;

                        var ID = await NetworkDatabase.GetShowID(LastItem.Name, LastItem.network.name);

                        if (ID > 0)
                        {
                            ShowImage.Source = await NetworkDatabase.GetImageURI(ID);
                            LastItem.Overview = NetworkDatabase.ShowDescriptions[ID];

                            ShowImage.IsVisible = true;
                            ImageLoading.IsVisible = false;
                        }
                    }


                }
                else
                {
                    FirstColumn.Width = new GridLength(1, GridUnitType.Star);
                    SecondColumn.Width = new GridLength(0);
                    if (LastItem != null)
                        LastItem.ShowDetails = true;

                }
            }

            //Prevent Year position from changing during window resize
            timer.Stop();
            timer.Start();
        }

        void LoadPredictions()
        {
            Predictions.Clear();

            if (Filtered && SelectedNetwork > -1)
                NetworkDatabase.NetworkList[SelectedNetwork].Filter(true, !CurrentModel, AllYears, FilteredYearList);
            else
                foreach (MiniNetwork x in NetworkDatabase.NetworkList)
                    x.Filter(true, !CurrentModel, AllYears, FilteredYearList);

            var AllPredictions = Filtered ?
                NetworkDatabase.NetworkList[SelectedNetwork].Predictions.AsParallel().SelectMany(x => x).Where(x => CurrentModel || (x.finalodds > 0 && (x.show.Renewed || x.show.Canceled || x.Status == ""))).OrderByDescending(x => CurrentModel ? x.odds : x.finalodds) :
                NetworkDatabase.NetworkList.AsParallel().SelectMany(x => x.Predictions).SelectMany(x => x).Where(x => CurrentModel || (x.finalodds > 0 && (x.show.Renewed || x.show.Canceled || x.Status == ""))).OrderByDescending(x => CurrentModel ? x.odds : x.finalodds);

            MiniNetwork.AddPredictions_Odds(AllPredictions, ref Predictions, !CurrentModel);            

            ShowList.ItemsSource = Predictions;

            var WithStatus = Predictions.AsParallel().SelectMany(x => x).Where(x => x.show.Renewed || x.show.Canceled);

            var Accurate = CurrentModel ?
                WithStatus.AsParallel().Where(x => (x.show.Renewed && x.odds > 0.5) || (!x.show.Renewed && x.odds < 0.5)) :
                WithStatus.AsParallel().Where(x => (x.show.Renewed && x.finalodds > 0.5) || (!x.show.Renewed && x.finalodds < 0.5));

            var part = Accurate.Count();
            var whole = WithStatus.Count();

            Accuracy.Text = "Accuracy: " + part + "/" + whole + " (" + ((double) part / whole).ToString("P1") + ")";
        }

        private async void ShowList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            //var p = e.Item as PredictionContainer;
            //if (LastItem != null && LastItem != p)
            //    LastItem.ShowDetails = false;

            //p.ShowDetails = !p.ShowDetails;
            //if (p.ShowDetails == true)
            //    LastItem = p;

            var p = ShowList.SelectedItem as PredictionContainer;

            if (LastItem != null && LastItem != p)
            {
                LastItem.ShowDetails = false;
                LastItem.ShowFinal = false;
            }                

            SidePanel.BindingContext = p;
            SidePanel.IsVisible = true;

            if (!isDesktop)
            {
                p.ShowDetails = !p.ShowDetails;
                p.ShowFinal = !p.ShowFinal;
                if (!p.ShowDetails)
                    ShowList.SelectedItem = null;
            }
            else
            {
                p.ShowFinal = !p.ShowFinal;
                p.ShowDetails = false;
                ShowImage.Source = null;
                ImageLoading.IsVisible = true;

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

                var TVDBText = "TV information and images are provided by TheTVDB.com, but we are not endorsed or certified by TheTVDB.com or its affiliates.";
                var Formatted = new FormattedString();
                Formatted.Spans.Add(new Span { Text = TVDBText });

                if (NetworkDatabase.TVDBerror)
                    Formatted.Spans.Add(new Span()
                    {
                        Text = " Error connecting to TVDB! Some show details and/or images may temporarily be unavailable.",
                        TextColor = Color.DarkRed
                    });

                TVDBNotice.FormattedText = Formatted;
            }


            

            LastItem = p;
        }

        private void ShowPage_Clicked(object sender, EventArgs e)
        {
            LastItem.IsShowPage = true;
            Navigation.PushAsync(new ShowDetailPage(LastItem));
        }

        private async void Options_Clicked(object sender, EventArgs e)
        {
            double start, end, ostart, oend;
            string etext;

            if (Options.IsVisible)
            {
                height = Options.Height;
                start = height;
                end = 0;
                ostart = 1;
                oend = 0;

                etext = "↓ Options ↓";
            }
            else
            {
                start = 0;
                end = height;
                ostart = 0;
                oend = 1;

                etext = "↑ Options ↑";                
            }

            Options.HeightRequest = start;
            Options.Opacity = ostart;
            Options.IsVisible = true;

            if (ostart == 1)
                await Options.FadeTo(oend);

            OptionsButton.Text = etext;

            new Animation(v => Options.HeightRequest = v, start, end).Commit(Options, "OptionsAnimation", 16, 250, Easing.SinInOut, async (d,b) =>
            {

                Options.HeightRequest = end;

                if (end == 0)
                    Options.IsVisible = false;
                else
                    Options.HeightRequest = -1;

                if (ostart == 0)
                    await Options.FadeTo(oend);
            });


            
        }
    }
}