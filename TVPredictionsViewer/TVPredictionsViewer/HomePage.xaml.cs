//using CarouselView.FormsPlugin.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TV_Ratings_Predictions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : ContentPage, INotifyPropertyChanged
    {
        Picker YearList;
        ActivityIndicator Activity = new ActivityIndicator();
        ResultsList SearchResults = new ResultsList();
        public TitleTemplate TitleBar => Bar;
        Timer HyperlinkTimer;

        public ObservableCollection<ShowHighlights> Highlights { get; } = new ObservableCollection<ShowHighlights>();

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

        public HomePage()
        {
            HyperlinkTimer = new Timer(100);
            HyperlinkTimer.Elapsed += HyperlinkTimer_Elapsed;
            HyperlinkTimer.AutoReset = false;
            InitializeComponent();
        }

        private void HyperlinkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                CurrentStatus.FormattedText = await FetchLabels();
            });
        }

        public void CompletedUpdate(string update)
        {           

            CurrentStatus.Text = "Loading Predictions...";

            NetworkDatabase.currentText = update;
        }

        public void Downloading()
        {
            CurrentStatus.Text = "Downloading Latest Predictions...";
        }

        async Task<FormattedString> FetchLabels()
        {
            var BlogPost = new FormattedString();
            var CurrentText = NetworkDatabase.currentText;

            await Task.Run(() =>
            {
                //Find All Unique Show and Network Names
                var Names = new ConcurrentBag<string>();
                foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                {
                    Names.Add(n.Name);
                    n.shows.AsParallel().Select(x => x.Name).Distinct().ForAll(x => Names.Add(x));
                }


                //Find All Matches in Blog Post
                var SearchMatches = new ConcurrentDictionary<string, int>();
                Parallel.ForEach(Names, s =>
                {
                    if (CurrentText.Contains(s))
                        SearchMatches[s] = CurrentText.IndexOf(s);
                });

                //Create a list of all match indexes
                var MatchIndexes = SearchMatches.Values.OrderBy(x => x).ToList();

                //Iterate through each match
                //adding a span of regular text followed by a link
                int CurrentIndex = 0;
                foreach (int i in MatchIndexes)
                {
                    if (i >= CurrentIndex)
                    {
                        var RegularText = CurrentText.Substring(CurrentIndex, i - CurrentIndex);
                        BlogPost.Spans.Add(RegularSpan(RegularText));

                        var LinkText = SearchMatches.Where(x => x.Value == i).OrderByDescending(x => x.Key.Length).First().Key;
                        BlogPost.Spans.Add(LinkSpan(LinkText));

                        CurrentIndex = i + LinkText.Length;
                    }
                }

                if (CurrentIndex < CurrentText.Length - 1)
                {
                    var RegularText = CurrentText.Substring(CurrentIndex);
                    BlogPost.Spans.Add(RegularSpan(RegularText));
                }
            });

            return BlogPost;
        }

        Span RegularSpan(string text)
        {
            return new Span() { Text = text };
        }

        Span LinkSpan(string text)
        {
            var span = new Span()
            {
                Text = text,
                TextColor = (Color)Application.Current.Resources["LinkColor"],
                TextDecorations = TextDecorations.Underline
            };

            span.GestureRecognizers.Add
            (
                new TapGestureRecognizer()
                {
                    Command = NavigateToShow,
                    CommandParameter = new NavigateParameter(text, this)
                }
            );

            return span;
        }

        private Command NavigateToShow = new Command<NavigateParameter>(async (Param) =>
        {
            //This will Navigate to the most recent show with this name
            var Shows = new ConcurrentBag<PredictionContainer>();
            foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                n.shows.AsParallel().Where(x => x.Name == Param.Text).ForAll(x => Shows.Add(new PredictionContainer(x, n, n.Adjustments[x.year], n.model.GetNetworkRatingsThreshold(x.year))));

            if (Shows.Count > 0)
            {
                var SelectedShow = Shows.OrderByDescending(x => x.show.year).First();

                await Param.Parent.Navigation.PushModalAsync(new ShowDetailPage(SelectedShow));
            }
            else
            {
                var SelectedNetwork = NetworkDatabase.NetworkList.Where(x => x.name == Param.Text).First();

                await Param.Parent.Navigation.PushAsync(new Predictions(SelectedNetwork));
            }

        });

        public void IncompleteUpdate()
        {
            CurrentStatus.Text = "";
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            Activity.IsRunning = false;

            RefreshPredictions.IsVisible = true;
        }

        public string CurrentWeek()
        {
            var week = new PredictionWeek();

            return week.Sunday.ToString("MMMM d") + " - " + (week.Sunday.Month == week.Saturday.Month ? week.Saturday.Day.ToString() : week.Saturday.ToString("MMMM d"));
        }

        public bool Completed;

        public async void CompletedSettings()
        {
            

            PredictionWeek.Text = CurrentWeek();

            YearList = FindTemplateElementByName<Picker>(this, "YearList");
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            SearchResults = FindTemplateElementByName<ResultsList>(this, "SearchResults");
            SearchResults.NavigationParent = this;

            SeasonList.Clear();
            foreach (Year y in NetworkDatabase.YearList)
                SeasonList.Add(y);
            YearList.BindingContext = this;
            YearList.IsVisible = true;
            //YearList.SelectedIndexChanged += YearList_SelectedIndexChanged;
            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;


            foreach (ToolbarItem t in new Toolbar(this).ToolBarItems)
                ToolbarItems.Add(t);

            PredictionYear.Text = NetworkDatabase.YearList[NetworkDatabase.YearList.Count - 1].Season + " Predictions";

            

            var year = NetworkDatabase.YearList[CurrentYear].year;

            var Thresholds = new Dictionary<string, double>();

            foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                Thresholds[n.name] = n.model.GetNetworkRatingsThreshold(year);

            HighlightsList.BindingContext = this;

            await Task.Run(async () =>
            {
                try
                {
                    var NewShows = NetworkDatabase.NetworkList.SelectMany(x => x.shows).Where(x => x.year == year && x.OldOdds == 0 && x.OldRating == 0).OrderByDescending(x => x.PredictedOdds);

                    foreach (Show s in NewShows)
                    {
                        var prediction = new PredictionContainer(s, s.network, s.network.Adjustments[s.year], Thresholds[s.network.name]);
                        var highlight = new ShowHighlights(prediction, 0);
                        await Device.InvokeOnMainThreadAsync(() => Highlights.Add(highlight));
                    }

                    var RenewedOrCanceledShows = NetworkDatabase.NetworkList.SelectMany(x => x.shows).Where(x => x.year == year && (x.Renewed || x.Canceled) && x.FinalPrediction == x.OldOdds).OrderByDescending(x => x.PredictedOdds);

                    foreach (Show s in RenewedOrCanceledShows)
                    {
                        var prediction = new PredictionContainer(s, s.network, s.network.Adjustments[s.year], Thresholds[s.network.name]);
                        var highlight = new ShowHighlights(prediction, 1);
                        await Device.InvokeOnMainThreadAsync(() => Highlights.Add(highlight));
                    }

                    var PredictionChanged = NetworkDatabase.NetworkList.SelectMany(x => x.shows).Where(x => x.year == year && x.RenewalStatus == "" && ((int)(x.OldOdds / 0.5) != (int)(x.PredictedOdds / 0.5))).OrderByDescending(x => x.PredictedOdds);

                    foreach (Show s in PredictionChanged)
                    {
                        var prediction = new PredictionContainer(s, s.network, s.network.Adjustments[s.year], Thresholds[s.network.name]);
                        var highlight = new ShowHighlights(prediction, 2);
                        await Device.InvokeOnMainThreadAsync(() => Highlights.Add(highlight));
                    }

                    var UpgradedOrDownGradedShows = NetworkDatabase.NetworkList.SelectMany(x => x.shows).Where(x => !PredictionChanged.Contains(x) && x.year == year && x.RenewalStatus == "" && ((int)(x.OldOdds / 0.2) != (int)(x.PredictedOdds / 0.2))).OrderByDescending(x => x.PredictedOdds);

                    foreach (Show s in UpgradedOrDownGradedShows)
                    {
                        var prediction = new PredictionContainer(s, s.network, s.network.Adjustments[s.year], Thresholds[s.network.name]);
                        var highlight = new ShowHighlights(prediction, 2);
                        await Device.InvokeOnMainThreadAsync(() => Highlights.Add(highlight));
                    }

                    await Device.InvokeOnMainThreadAsync(async () =>
                    {
                        Activity.IsRunning = false;
                        Activity.IsVisible = false;
                        UseMenu.IsVisible = true;
                        CurrentStatus.IsVisible = false;

                        CurrentStatus.FormattedText = await FetchLabels();

                        if (Highlights.Count > 0)
                        {
                            HighlightsTitle.IsVisible = true;
                            ViewPost.IsVisible = true;
                            TMDBNotice.IsVisible = true;
                        }

                        else
                            CurrentStatus.IsVisible = true;

                        Completed = true;
                    });
                }
                catch (Exception ex)
                {
                    string error = ex.ToString();
                }
                
            });            
        }

        public void RefreshYearlist()
        {
            PredictionWeek.Text = CurrentWeek();
            YearList.ItemsSource = NetworkDatabase.YearList;
        }

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            OnPropertyChanged("CurrentYear");
            if (YearList.SelectedIndex > -1)
            {
                PredictionYear.Text = NetworkDatabase.YearList[YearList.SelectedIndex].Season + " Predictions";
                if (NetworkDatabase.MaxYear > NetworkDatabase.YearList[CurrentYear].year)
                {
                    PredictionWeek.Text = "";
                    CurrentStatus.Text = "";
                    UseMenu.Text = "You are now viewing historical data. We have ratings and renewal/cancellation data going back to 2014. \r\n\r\n" +
                    "use the menu on the left to see what the current prediction models would have predicted for those shows";

                    HighlightsList.IsVisible = false;
                    HighlightsTitle.IsVisible = false;
                    ViewPost.IsVisible = false;
                    TMDBNotice.IsVisible = false;
                }
                else
                {
                    PredictionWeek.Text = CurrentWeek();
                    CurrentStatus.Text = NetworkDatabase.currentText;
                    UseMenu.Text = "use the menu on the left to see predictions for each network";

                    HighlightsList.IsVisible = true;
                    HighlightsTitle.IsVisible = Highlights.Count > 0;
                    ViewPost.IsVisible = HighlightsTitle.IsVisible;
                    TMDBNotice.IsVisible = ViewPost.IsVisible;

                    HyperlinkTimer.Stop();
                    HyperlinkTimer.Start();
                }
            }
        }

        public async void NavigateToNetwork(MiniNetwork n)
        {
            //HomeLayout.IsVisible = false;
            //PredictionFrame.IsVisible = true;
            await Navigation.PushAsync(new Predictions(n));
        }

        public async void NavigateToAllNetworks()
        {
            await Navigation.PushAsync(new Predictions());
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

        protected override void OnSizeAllocated(double dblWidth, double dblHeight)
        {
            base.OnSizeAllocated(dblWidth, dblHeight);

            // fix for carouselview orientation bug on android
            //if (Device.RuntimePlatform == Device.Android)
            //{
            //    YearList.Orientation =
            //        CarouselViewOrientation.Vertical;
            //    YearList.Orientation =
            //        CarouselViewOrientation.Horizontal;
            //}

            //re-label Blog Post
            //timer.Stop();
            //timer.Start();
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

        public async void RefreshPredictions_Clicked(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(NetworkDatabase.Folder, "Update.txt")))
                File.Delete(Path.Combine(NetworkDatabase.Folder, "Update.txt"));

            if (File.Exists(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP")))
                File.Delete(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP"));

            Activity.IsRunning = true;
            CurrentStatus.Text = "Downloading Latest Predictions...";

            RefreshPredictions.IsVisible = false;
            await NetworkDatabase.ReadUpdateAsync();
        }

        private void ViewPost_Clicked(object sender, EventArgs e)
        {
            if (CurrentStatus.IsVisible)
            {
                CurrentStatus.IsVisible = false;
                ViewPost.Text = "View Full Post";
            }
            else
            {
                CurrentStatus.IsVisible = true;
                ViewPost.Text = "Hide Post";

            }
        }

        private void HighlightButton_Clicked(object sender, EventArgs e)
        {
            var p = (sender as Element).BindingContext as ShowHighlights;

            p.Navigate(Navigation);
        }
    }

    class NavigateParameter
    {
        public string Text;
        public Page Parent;

        public NavigateParameter(string t, Page p)
        {
            Text = t;
            Parent = p;
        }
    }

    class PredictionWeek
    {
        public DateTime Saturday, Sunday;
        public PredictionWeek()
        {
            var path = Path.Combine(NetworkDatabase.Folder, "Predictions.TVP");
            var now = (NetworkDatabase.NetworkList != null && NetworkDatabase.NetworkList.Count > 0) ? NetworkDatabase.NetworkList[0].PredictionTime :
                (File.Exists(path)) ? File.GetLastWriteTime(path) : DateTime.Now;

            Saturday = now.AddDays(-1);

            while (Saturday.DayOfWeek != DayOfWeek.Saturday)
                Saturday = Saturday.AddDays(-1);

            Sunday = Saturday.AddDays(-6);
        }
    }


}