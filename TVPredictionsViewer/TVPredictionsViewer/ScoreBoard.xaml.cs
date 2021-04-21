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
        Picker YearList;
        ActivityIndicator Activity = new ActivityIndicator();
        ResultsList SearchResults = new ResultsList();
        public TitleTemplate TitleBar => Bar;
        public PredictionContainer LastItem;
        bool isDesktop = false;
        double height;
        public bool UsePrediction;
        public PredictionContainer prediction;

        List<Year> FilteredYearList;

        public ObservableCollection<ListOfPredictions> Predictions = new ObservableCollection<ListOfPredictions>();

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

        public Grid MainGrid
        {
            get
            {
                return Grid1;
            }
        }

        string _uri;
        public string ShowImageUri
        {
            get { return _uri; }
            set
            {
                _uri = value;
                OnPropertyChanged("ShowImageUri");
            }
        }

        public int CurrentYear
        {
            get 
            {
                if (SeasonList.Contains(NetworkDatabase.YearList[NetworkDatabase.CurrentYear]))
                    return SeasonList.IndexOf(NetworkDatabase.YearList[NetworkDatabase.CurrentYear]);
                else if (NetworkDatabase.YearList[NetworkDatabase.CurrentYear].year > SeasonList[SeasonList.Count - 1].year)
                    return SeasonList.Count - 1;
                else
                    return 0;
                    
            }
            set
            {
                NetworkDatabase.CurrentYear = NetworkDatabase.YearList.IndexOf(SeasonList[value]);
                LoadPredictions();
                OnPropertyChanged("CurrentYear");
            }
        }

        public ObservableCollection<Year> _seasonList = new ObservableCollection<Year>();
        public ObservableCollection<Year> SeasonList => _seasonList;

        public List<string> NetworkList { get { return NetworkDatabase.NetworkList.Select(x => x.Name).ToList(); } }

        public ScoreBoard(MiniNetwork n = null)
        {
            Load(n);
        }

        public ScoreBoard(PredictionContainer p)
        {
            UsePrediction = true;
            prediction = p;

            Load(p.network);
        }

        void Load(MiniNetwork n)
        {
            foreach (ToolbarItem t in new Toolbar(this, ref Predictions).ToolBarItems)
                ToolbarItems.Add(t);

            FilteredYearList = NetworkDatabase.YearList.Where(x => NetworkDatabase.NetworkList.AsParallel().SelectMany(y => y.shows).Where(y => y.year == x).Where(y => y.FinalPrediction > 0 && (y.Renewed || y.Canceled)).Count() > 0).ToList();
            foreach (Year y in FilteredYearList)
                SeasonList.Add(y);

            network = n;

            InitializeComponent();

            BindingContext = this;

            YearList = FindTemplateElementByName<Picker>(this, "YearList");
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            SearchResults = FindTemplateElementByName<ResultsList>(this, "SearchResults");
            SearchResults.NavigationParent = this;

            YearList.BindingContext = this;
            //YearList.ItemsSource = FilteredYearList;
            YearList.IsVisible = true;
            //YearList.Position = FilteredYearList.Count - 1;

            //YearList.PositionSelected += YearList_PositionSelected;
            //YearList.PositionChanged += YearList_PositionChanged;
            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;


            NetworkDatabase_CurrentYearUpdated(this, new EventArgs());
            //NetworkDatabase.CurrentYear = NetworkDatabase.YearList.IndexOf(SeasonList[SeasonList.Count-1]);

            Activity.IsRunning = false;
            Activity.IsVisible = false;

            SideColumn.SizeChanged += SideColumn_SizeChanged;
            SidePanel.PanelOpened += SidePanel_PanelOpened;

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
            Disappearing += ScoreBoard_Disappearing;
        }

        

        private void ScoreBoard_Disappearing(object sender, EventArgs e)
        {
            //NetworkDatabase.CurrentYearUpdated -= NetworkDatabase_CurrentYearUpdated;
        }

        private void SideColumn_SizeChanged(object sender, EventArgs e)
        {
            var width = SideColumn.Width;

            if (SidePanel.isDesktop && SidePanel.BreakdownView != null) width /= 2;

            if (SideColumn.Width > 5)
                ImageRow.Height = width * 9 / 16;
        }

        private void SidePanel_PanelOpened(object sender, EventArgs e)
        {
            SideColumn_SizeChanged(this, new EventArgs());
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
            else if (UsePrediction)
            {
                PopAndPush();
                return true;
            }
            else
                return base.OnBackButtonPressed();
        }

        async void PopAndPush()
        {
            await Navigation.PushModalAsync(new ShowDetailPage(prediction));
            await Navigation.PopAsync();
        }

        async void FadeOut()
        {
            await SidePanel.BreakdownView.FadeTo(0);
            SideColumn.Children.Remove(SidePanel.BreakdownView);
            SidePanel.BreakdownView = null;
            SideColumn_SizeChanged(this, new EventArgs());
        }

        private void Year_Tapped(object sender, EventArgs e)
        {
            AllYears = !AllYears;
        }

        private void Network_Tapped(object sender, EventArgs e)
        {
            Filtered = !Filtered;
        }

        //private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    timer.Stop();

        //    Device.BeginInvokeOnMainThread(() =>
        //    {
        //        if (YearList.Position > -1)
        //        {
        //            var oldyear = NetworkDatabase.CurrentYear;
        //            NetworkDatabase.CurrentYear = NetworkDatabase.YearList.IndexOf(FilteredYearList[YearList.Position]);
        //            if (!AllYears && Predictions.Count > 0 && NetworkDatabase.CurrentYear != oldyear) LoadPredictions();
        //        }                
        //    });

        //}

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            //var CurrentYear = NetworkDatabase.YearList[NetworkDatabase.CurrentYear];

            //if (FilteredYearList.Contains(CurrentYear))
            //    YearList.Position = FilteredYearList.IndexOf(CurrentYear);
            //else if (FilteredYearList.Where(x => x.year < CurrentYear.year).Count() > 0)
            //    YearList.Position = FilteredYearList.Count - 1;
            //else
            //    YearList.Position = 0;

            OnPropertyChanged("CurrentYear");
        }

        //private void YearList_PositionChanged(object sender, PositionChangedEventArgs e)
        //{
        //    if (Navigation.NavigationStack.LastOrDefault() == this && YearList.Position > -1)
        //    {
        //        timer.Stop();
        //        timer.Start();
        //    }
        //}

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
            //if (Device.RuntimePlatform == Device.Android)
            //{
            //    YearList.Orientation =
            //        CarouselViewOrientation.Vertical;
            //    YearList.Orientation =
            //        CarouselViewOrientation.Horizontal;
            //}

            var tmpDesktop = isDesktop;
            isDesktop = width > (1080);
            SideColumn.IsVisible = isDesktop;

            if (isDesktop != tmpDesktop)
            {
                if (isDesktop)
                {
                    FirstColumn.Width = new GridLength(1, GridUnitType.Star);
                    SecondColumn.Width = new GridLength(1, GridUnitType.Star);
                    if (LastItem != null)
                    {
                        LastItem.ShowDetails = false;
                        ShowImage.Source = null;
                        ImageLoading.IsVisible = true;

                        //var ID = await NetworkDatabase.GetShowID(LastItem.Name, LastItem.network.name);

                        //if (ID > 0)
                        //{
                        //    ShowImage.Source = await NetworkDatabase.GetImageURI(ID);
                        //    LastItem.Overview = NetworkDatabase.ShowDescriptions[ID];

                        //    ShowImage.IsVisible = true;
                        //    ImageLoading.IsVisible = false;
                        //}

                        //bool reload = false;

                        //if (!NetworkDatabase.ShowIDs.ContainsKey(LastItem.Name) && Application.Current.Properties.ContainsKey("SHOWID " + LastItem.Name))
                        //    reload = true;

                        var ID = await NetworkDatabase.GetShowID(LastItem.Name, LastItem.network.name);

                        if (ID > 0)
                        {
                            if (Device.RuntimePlatform == Device.UWP)
                            {
                                var uri = await NetworkDatabase.GetImageURI(ID);
                                ShowImageUri = uri.AbsoluteUri;

                                ShowImage.BindingContext = this;
                                ShowImage.SetBinding(ImageEffect.TextProperty, new Binding("ShowImageUri"));

                                LastItem.IsLoaded = true;
                                ShowImage.IsVisible = true;
                                //ImageLoading.IsVisible = false;
                            }
                            else
                            {
                                ShowImage.Source = new UriImageSource
                                {
                                    Uri = await NetworkDatabase.GetImageURI(ID),
                                    CachingEnabled = true,
                                    CacheValidity = new TimeSpan(90, 0, 0, 0)
                                };
                                LastItem.Overview = NetworkDatabase.ShowDescriptions[ID];

                                if (ShowImage.Source != null)
                                {
                                    LastItem.IsLoaded = true;
                                    ShowImage.IsVisible = true;
                                    ImageLoading.IsVisible = false;
                                }
                            }
                        }

                        //if (reload)
                        //{
                        //    ID = await NetworkDatabase.GetShowID(LastItem.Name, LastItem.network.name, true);

                        //    if (ID > 0)
                        //    {
                        //        ShowImage.Source = new UriImageSource
                        //        {
                        //            Uri = await NetworkDatabase.GetImageURI(ID),
                        //            CachingEnabled = true,
                        //            CacheValidity = new TimeSpan(90, 0, 0, 0)
                        //        };
                        //        LastItem.Overview = NetworkDatabase.ShowDescriptions[ID];
                        //    }
                        //}
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
        }

        void LoadPredictions()
        {
            Predictions.Clear();

            if (Filtered && SelectedNetwork > -1)
                NetworkDatabase.NetworkList[SelectedNetwork].Filter(true, !CurrentModel, AllYears, FilteredYearList);
            else
                foreach (MiniNetwork x in NetworkDatabase.NetworkList)
                    x.Filter(true, !CurrentModel, AllYears, FilteredYearList);

            var AllPredictions = (Filtered && SelectedNetwork > -1) ?
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

                //var ID = await NetworkDatabase.GetShowID(p.show.Name, p.network.name);

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
                //        p.IsLoaded = true;
                //        ShowImage.IsVisible = true;
                //        ImageLoading.IsVisible = false;
                //    }
                //}

                //bool reload = false;

                //if (!NetworkDatabase.ShowIDs.ContainsKey(p.show.Name) && Application.Current.Properties.ContainsKey("SHOWID " + p.show.Name))
                //    reload = true;

                var ID = await NetworkDatabase.GetShowID(p.show.Name, p.network.name);

                if (ID > 0)
                {
                    if (Device.RuntimePlatform == Device.UWP)
                    {
                        var uri = await NetworkDatabase.GetImageURI(ID);
                        ShowImageUri = uri.AbsoluteUri;

                        ShowImage.BindingContext = this;
                        ShowImage.SetBinding(ImageEffect.TextProperty, new Binding("ShowImageUri"));

                        p.IsLoaded = true;
                        ShowImage.IsVisible = true;
                        //ImageLoading.IsVisible = false;
                    }
                    else
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
                }

                //if (reload)
                //{
                //    ID = await NetworkDatabase.GetShowID(p.Name, p.network.name, true);

                //    if (ID > 0)
                //    {
                //        var image = new UriImageSource
                //        {
                //            Uri = await NetworkDatabase.GetImageURI(ID),
                //            CachingEnabled = true,
                //            CacheValidity = new TimeSpan(90, 0, 0, 0)
                //        };

                //        ShowImage.Source = image;
                //        p.Overview = NetworkDatabase.ShowDescriptions[ID];
                //    }
                //}

                var TMDBText = "This product uses the TMDb API but is not endorsed or certified by TMDb.";
                var Formatted = new FormattedString();
                Formatted.Spans.Add(new Span { Text = TMDBText });

                if (NetworkDatabase.TMDBerror)
                    Formatted.Spans.Add(new Span()
                    {
                        Text = "Error connecting to TMDB! Some show details and/or images may temporarily be unavailable.",
                        TextColor = Color.DarkRed
                    });

                TMDBNotice.FormattedText = Formatted;

                var DelayedScroll = new Timer(1000);
                DelayedScroll.Elapsed += DelayedScroll_Elapsed;
                DelayedScroll.AutoReset = false;
                DelayedScroll.Start();
            }

            LastItem = p;
        }

        private async void DelayedScroll_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(async () => await DetailScroll.ScrollToAsync(SidePanel, ScrollToPosition.Start, true));
        }

        private async void ShowPage_Clicked(object sender, EventArgs e)
        {
            LastItem.IsShowPage = true;
            await Navigation.PushModalAsync(new ShowDetailPage(LastItem));
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