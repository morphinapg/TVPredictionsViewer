using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.ComponentModel;
using System.Timers;
using CarouselView.FormsPlugin.Abstractions;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Predictions : ContentPage
    {
        public MiniNetwork network;
        public ObservableCollection<ListOfPredictions> PredictionList;
        CarouselViewControl YearList = new CarouselViewControl();
        ActivityIndicator Activity = new ActivityIndicator();
        public PredictionContainer PreviousItem;
        ResultsList SearchResults = new ResultsList();
        bool isDesktop = false;
        bool Window_Sizing = false;
        bool isAllNetworks = false;
        Timer timer;
        static object PredictionLock = new object();
        //ObservableCollection<ListOfPredictions> Results = new ObservableCollection<ListOfPredictions>();

        public TitleTemplate TitleBar => Bar;

        public Grid MainGrid => Grid1;

        public Predictions()
        {
            Appearing += Predictions_Appearing;
            Disappearing += Predictions_Disappearing;
            PredictionList = new ObservableCollection<ListOfPredictions>();
            isAllNetworks = true;
            foreach (ToolbarItem t in new Toolbar(this, ref PredictionList).ToolBarItems)
                ToolbarItems.Add(t);

            UpdateFilter(ref PredictionList);            

            InitializeComponent();

            

            Bar.Title = "All Networks";
            FinishLoading();
        }

        private void Predictions_Disappearing(object sender, EventArgs e)
        {
            NetworkDatabase.CurrentYearUpdated -= NetworkDatabase_CurrentYearUpdated;
        }

        public static void UpdateFilter(ref ObservableCollection<ListOfPredictions> PredictionList)
        {
            lock (PredictionLock)
            {
                PredictionList.Clear();
                foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                    if (n.pendingFilter)
                        n.Filter(true);

                if (Application.Current.Properties.ContainsKey("PredictionSort"))
                {
                    switch (Application.Current.Properties["PredictionSort"] as string)
                    {
                        case "Ratings":
                            {
                                var TempList = NetworkDatabase.NetworkList.AsParallel().Where(x => x.Predictions.Count > 0).SelectMany(x => x.Predictions).SelectMany(x => x).OrderByDescending(x => x.show.AverageRating).ToList();
                                MiniNetwork.AddPredictions_Ratings(TempList, ref PredictionList);
                                break;
                            }
                        case "Name":
                            {
                                var TempList = NetworkDatabase.NetworkList.AsParallel().Where(x => x.Predictions.Count > 0).SelectMany(x => x.Predictions).SelectMany(x => x).OrderBy(x => x.Name).ToList();
                                MiniNetwork.AddPredictions_Name(TempList, ref PredictionList);
                                break;
                            }
                        default:
                            {
                                Filter_Odds(ref PredictionList);
                                break;
                            }
                    }
                }
                else
                    Filter_Odds(ref PredictionList);
            }            
        }

        static void Filter_Odds(ref ObservableCollection<ListOfPredictions> PredictionList)
        {
            var tempPredictions = NetworkDatabase.NetworkList.AsParallel().Where(x => x.Predictions.Count > 0).SelectMany(x => x.Predictions).SelectMany(x => x).OrderByDescending(x => x.odds);
            MiniNetwork.AddPredictions_Odds(tempPredictions, ref PredictionList);
        }

        public Predictions(MiniNetwork n)
        {
            Appearing += Predictions_Appearing;
            Disappearing += Predictions_Disappearing;
            isAllNetworks = false;
            network = n;
            foreach (ToolbarItem t in new Toolbar(this, n).ToolBarItems)
                ToolbarItems.Add(t);

            PredictionList = n.Predictions;

            if (network.pendingFilter)
                network.Filter();

            InitializeComponent();

            Bar.Title = network.name;
            FinishLoading();
        }

        void FinishLoading()
        {
            ShowsList.ItemsSource = PredictionList;            

            YearList = FindTemplateElementByName<CarouselViewControl>(this, "YearList");
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            SearchResults = FindTemplateElementByName<ResultsList>(this, "SearchResults");
            SearchResults.NavigationParent = this;

            YearList.ItemsSource = NetworkDatabase.YearList;
            YearList.IsVisible = true;
            YearList.Position = NetworkDatabase.CurrentYear;
            YearList.PositionSelected += YearList_PositionSelected;

            Activity.IsRunning = false;
            Activity.IsVisible = false;

            SideColumn.SizeChanged += SideColumn_SizeChanged;

            timer = new Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;

            
        }

        private void Predictions_Appearing(object sender, EventArgs e)
        {
            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;

            //Clear memory of old Predictions pages
            var stack = Navigation.NavigationStack;
            stack.Where(x => (x is Predictions && x != this) || x is ScoreBoard).ToList().ForEach(x => Navigation.RemovePage(x));
        }

        private void YearList_PositionSelected(object sender, PositionSelectedEventArgs e)
        {
            if (YearList.Position > -1 && !Window_Sizing)
                NetworkDatabase.CurrentYear = YearList.Position;

        }

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            if (isAllNetworks)
                UpdateFilter(ref PredictionList);
            else
                network.Filter();

            if (YearList.Position != NetworkDatabase.CurrentYear)
                YearList.Position = NetworkDatabase.CurrentYear;
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

        protected override async void OnSizeAllocated(double dblWidth, double dblHeight)
        {
            base.OnSizeAllocated(dblWidth, dblHeight);

            Window_Sizing = true;

            // fix for carouselview orientation bug on android
            if (Device.RuntimePlatform == Device.Android)
            {
                YearList.Orientation =
                    CarouselViewOrientation.Vertical;
                YearList.Orientation =
                    CarouselViewOrientation.Horizontal;
            }

            var tmpDesktop = isDesktop;
            isDesktop = dblWidth > (960);
            SideColumn.IsVisible = isDesktop;

            if (isDesktop != tmpDesktop)
            {
                if (isDesktop)
                {
                    FirstColumn.Width = new GridLength(1, GridUnitType.Star);
                    SecondColumn.Width = new GridLength(1, GridUnitType.Star);
                    if (PreviousItem != null)
                    {
                        PreviousItem.ShowDetails = false;
                        ShowImage.Source = null;
                        ImageLoading.IsVisible = true;
                        bool reload = false;

                        if (!NetworkDatabase.ShowIDs.ContainsKey(PreviousItem.Name) && Application.Current.Properties.ContainsKey("SHOWID " + PreviousItem.Name))
                            reload = true;

                        var ID = await NetworkDatabase.GetShowID(PreviousItem.Name, PreviousItem.network.name);

                        if (ID > 0)
                        {
                            ShowImage.Source = new UriImageSource
                            {
                                Uri = await NetworkDatabase.GetImageURI(ID),
                                CachingEnabled = true,
                                CacheValidity = new TimeSpan(90, 0, 0, 0)
                            };
                            PreviousItem.Overview = NetworkDatabase.ShowDescriptions[ID];

                            if (ShowImage.Source != null)
                            {
                                PreviousItem.IsLoaded = true;
                                ShowImage.IsVisible = true;
                                ImageLoading.IsVisible = false;
                            }
                        }
                        
                        if (reload)
                        {
                            ID = await NetworkDatabase.GetShowID(PreviousItem.Name, PreviousItem.network.name, true);

                            if (ID > 0)
                            {
                                ShowImage.Source = new UriImageSource
                                {
                                    Uri = await NetworkDatabase.GetImageURI(ID),
                                    CachingEnabled = true,
                                    CacheValidity = new TimeSpan(90, 0, 0, 0)
                                };
                                PreviousItem.Overview = NetworkDatabase.ShowDescriptions[ID];
                            }
                        }
                    }                     

                    
                }
                else
                {
                    FirstColumn.Width = new GridLength(1, GridUnitType.Star);
                    SecondColumn.Width = new GridLength(0);
                    if (PreviousItem != null)
                        PreviousItem.ShowDetails = true;
                        
                }
            }

            //Prevent Year position from changing during window resize
            timer.Stop();
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            Window_Sizing = false;

            Device.BeginInvokeOnMainThread( () =>
            {
                if (Navigation.NavigationStack.LastOrDefault() == this && YearList.Position != NetworkDatabase.CurrentYear)
                    NetworkDatabase.CurrentYear = YearList.Position;
            });
        }

        private void SideColumn_SizeChanged(object sender, EventArgs e)
        {
            if (SideColumn.Width > 5)
                ImageRow.Height = SideColumn.Width * 140 / 758;
        }

        private async void ShowsList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var p = ShowsList.SelectedItem as PredictionContainer;

            SidePanel.BindingContext = p;
            SidePanel.IsVisible = true;

            if (!isDesktop)
            {
                p.ShowDetails = !p.ShowDetails;
                if (!p.ShowDetails)
                    ShowsList.SelectedItem = null;
            }                
            else
            {
                p.ShowDetails = false;
                ShowImage.Source = null;
                ImageLoading.IsVisible = true;

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
                //        p.IsLoaded = true;
                //        ShowImage.IsVisible = true;
                //        ImageLoading.IsVisible = false;
                //    }                    
                //}

                bool reload = false;

                if (!NetworkDatabase.ShowIDs.ContainsKey(p.Name) && Application.Current.Properties.ContainsKey("SHOWID " + p.Name))
                    reload = true;

                var ID = await NetworkDatabase.GetShowID(p.Name, p.network.name);

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
                    ID = await NetworkDatabase.GetShowID(p.Name, p.network.name, true);

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

                if (NetworkDatabase.TVDBerror)
                    Formatted.Spans.Add(new Span()
                    {
                        Text = " Error connecting to TVDB! Some show details and/or images may temporarily be unavailable.",
                        TextColor = Color.DarkRed
                    });

                TVDBNotice.FormattedText = Formatted;
            }
                       

            if (PreviousItem != null && PreviousItem != p)
                PreviousItem.ShowDetails = false;

            PreviousItem = p;
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
    }
}