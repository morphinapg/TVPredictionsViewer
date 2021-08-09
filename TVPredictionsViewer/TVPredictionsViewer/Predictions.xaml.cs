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

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Predictions : ContentPage, INotifyPropertyChanged
    {
        public MiniNetwork network;
        public ObservableCollection<ListOfPredictions> PredictionList;
        Picker YearList;
        ActivityIndicator Activity = new ActivityIndicator();
        public PredictionContainer PreviousItem;
        ResultsList SearchResults = new ResultsList();
        bool isDesktop = false;
        bool isAllNetworks = false, FadingOut = false;
        static object PredictionLock = new object();
        Timer DelayedScroll = new Timer(1000), CheckVisible = new Timer(100);
        //ObservableCollection<ListOfPredictions> Results = new ObservableCollection<ListOfPredictions>();

        public TitleTemplate TitleBar => Bar;

        public Grid MainGrid => Grid1;

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
            get { return NetworkDatabase.CurrentYear; }
            set
            {
                NetworkDatabase.CurrentYear = value;
                OnPropertyChanged("CurrentYear");
            }
        }

        public ObservableCollection<Year> _seasonList = new ObservableCollection<Year>();
        public ObservableCollection<Year> SeasonList => _seasonList;

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
            //NetworkDatabase.CurrentYearUpdated -= NetworkDatabase_CurrentYearUpdated;
        }

        public static void UpdateFilter(ref ObservableCollection<ListOfPredictions> PredictionList)
        {
            lock (PredictionLock)
            {
                PredictionList.Clear();
                foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                    if (n.pendingFilter)
                        n.Filter(false);

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

            YearList = FindTemplateElementByName<Picker>(this, "YearList");
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            SearchResults = FindTemplateElementByName<ResultsList>(this, "SearchResults");
            SearchResults.NavigationParent = this;

            SeasonList.Clear();
            foreach (Year y in NetworkDatabase.YearList)
                SeasonList.Add(y);
            YearList.BindingContext = this;
            YearList.IsVisible = true;
            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;

            Activity.IsRunning = false;
            Activity.IsVisible = false;

            SideColumn.SizeChanged += SideColumn_SizeChanged;
            SidePanel.PanelOpened += SidePanel_PanelOpened;

            
        }

        

        private void Predictions_Appearing(object sender, EventArgs e)
        {
            

            //Clear memory of old Predictions pages
            var stack = Navigation.NavigationStack;
            stack.Where(x => (x is Predictions && x != this) || x is ScoreBoard).ToList().ForEach(x => Navigation.RemovePage(x));
        }

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            if (isAllNetworks)
                UpdateFilter(ref PredictionList);
            else
                network.Filter();

            OnPropertyChanged("CurrentYear");
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
                        //bool reload = false;

                        //if (!NetworkDatabase.ShowIDs.ContainsKey(PreviousItem.Name) && Application.Current.Properties.ContainsKey("SHOWID " + PreviousItem.Name))
                        //    reload = true;

                        var ID = await NetworkDatabase.GetShowID(PreviousItem.Name, PreviousItem.network.name);

                        if (ID > 0)
                        {
                            if (Device.RuntimePlatform == Device.UWP)
                            {
                                var uri = await NetworkDatabase.GetImageURI(ID);
                                ShowImageUri = uri.AbsoluteUri;

                                ShowImage.BindingContext = this;
                                ShowImage.SetBinding(ImageEffect.TextProperty, new Binding("ShowImageUri"));

                                PreviousItem.IsLoaded = true;
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
                                
                                if (ShowImage.Source != null)
                                {
                                    PreviousItem.IsLoaded = true;
                                    ShowImage.IsVisible = true;
                                    ImageLoading.IsVisible = false;
                                }
                            }

                            PreviousItem.Overview = NetworkDatabase.ShowDescriptions[ID];
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
        }

        private void SideColumn_SizeChanged(object sender, EventArgs e)
        {
            var width = SideColumn.Width;

            if (SidePanel.isDesktop && SidePanel.BreakdownView != null) width /= 2;

            if (SideColumn.Width > 5)
                ImageRow.Height = width * 9 / 16;

            DelayedScroll.Stop();
            DelayedScroll.Start();
        }

        private void SidePanel_PanelOpened(object sender, EventArgs e)
        {
            SideColumn_SizeChanged(this, new EventArgs());
        }

        private async void ShowsList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var p = ShowsList.SelectedItem as PredictionContainer;

            if (PreviousItem != null && PreviousItem != p)
            {
                PreviousItem.ShowDetails = false;
                if (SidePanel.BreakdownView != null) FadeOut(false);
            }                

            SidePanel.BindingContext = p;
            SidePanel.IsVisible = true;

            SidePanel_PanelOpened(this, new EventArgs());

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
                ShowImageUri = null;
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

                //bool reload = false;

                //if (!NetworkDatabase.ShowIDs.ContainsKey(p.Name) && Application.Current.Properties.ContainsKey("SHOWID " + p.Name))
                //    reload = true;

                var ID = await NetworkDatabase.GetShowID(p.Name, p.network.name);

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
                        
                        if (ShowImage.Source != null)
                        {
                            p.IsLoaded = true;
                            ShowImage.IsVisible = true;
                            ImageLoading.IsVisible = false;
                        }
                    }

                    p.Overview = NetworkDatabase.ShowDescriptions[ID];
                }



                //if (reload)
                //{
                //    ID = await NetworkDatabase.GetShowID(p.Name, p.network.name, true);

                //    if (ID > 0)
                //    {
                //        ShowImage.Source = new UriImageSource
                //        {
                //            Uri = await NetworkDatabase.GetImageURI(ID),
                //            CachingEnabled = true,
                //            CacheValidity = new TimeSpan(90, 0, 0, 0)
                //        };
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

                DelayedScroll.Elapsed += DelayedScroll_Elapsed;
                DelayedScroll.AutoReset = false;
                DelayedScroll.Start();

                CheckVisible.Elapsed += CheckVisible_Elapsed;
            }
                       

            
                

            PreviousItem = p;
        }

        private async void CheckVisible_Elapsed(object sender, ElapsedEventArgs e)
        {
            var Visibility = DetailScroll.ScrollY + DetailScroll.Height;
            if (Visibility < ShowImage.Height && !ScrollDown.IsVisible)
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    ScrollDown.Opacity = 0;
                    ScrollDown.IsVisible = true;
                    await ScrollDown.FadeTo(1);
                });
            else if (Visibility > ShowImage.Height && ScrollDown.IsVisible)
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    await ScrollDown.FadeTo(0);
                    ScrollDown.IsVisible = false;
                });
        }

        private async void DelayedScroll_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Device.InvokeOnMainThreadAsync(async () => await DetailScroll.ScrollToAsync(SidePanel, ScrollToPosition.Start, true));

            CheckVisible.Start();
        }

        private async void DownButton_Clicked(object sender, EventArgs e)
        {
            await DetailScroll.ScrollToAsync(SidePanel, ScrollToPosition.Start, true);
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
            {
                return base.OnBackButtonPressed();
            }
                
        }

        async void FadeOut(bool animation = true)
        {
            if (!FadingOut)
            {
                FadingOut = true;
                if (animation)
                    await SidePanel.BreakdownView.FadeTo(0);
                SideColumn.Children.Remove(SidePanel.BreakdownView);
                SidePanel.BreakdownView = null;

                if (animation)
                    SidePanel_PanelOpened(this, new EventArgs());
                FadingOut = false;
            }
            
        }
    }
}