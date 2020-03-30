﻿using CarouselView.FormsPlugin.Abstractions;
using System;
using System.Collections.Concurrent;
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
    public partial class HomePage : ContentPage
    {
        CarouselViewControl YearList = new CarouselViewControl();
        ActivityIndicator Activity = new ActivityIndicator();
        ResultsList SearchResults = new ResultsList();
        public TitleTemplate TitleBar => Bar;
        Timer timer, HyperlinkTimer;

        public HomePage()
        {
            Appearing += HomePage_Appearing;
            Disappearing += HomePage_Disappearing;

            timer = new Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;

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

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (Navigation.NavigationStack.Last() == this)
                    NetworkDatabase.CurrentYear = YearList.Position;
            });
        }

        public void CompletedUpdate(string update)
        {
            

            CurrentStatus.Text = update;

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
                    if (i > CurrentIndex)
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

                await Param.Parent.Navigation.PushAsync(new ShowDetailPage(SelectedShow));
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

            YearList = FindTemplateElementByName<CarouselViewControl>(this, "YearList");
            Activity = FindTemplateElementByName<ActivityIndicator>(this, "Activity");
            SearchResults = FindTemplateElementByName<ResultsList>(this, "SearchResults");
            SearchResults.NavigationParent = this;

            YearList.ItemsSource = NetworkDatabase.YearList;
            YearList.IsVisible = true;
            YearList.Position = NetworkDatabase.YearList.Count - 1;
            YearList.PositionSelected += YearList_PositionSelected;


            foreach (ToolbarItem t in new Toolbar(this).ToolBarItems)
                ToolbarItems.Add(t);

            PredictionYear.Text = NetworkDatabase.YearList[NetworkDatabase.YearList.Count - 1].Season + " Predictions";

            Activity.IsRunning = false;
            Activity.IsVisible = false;
            UseMenu.IsVisible = true;

            CurrentStatus.FormattedText = await FetchLabels();

            Completed = true;
        }

        private void HomePage_Disappearing(object sender, EventArgs e)
        {
            NetworkDatabase.CurrentYearUpdated -= NetworkDatabase_CurrentYearUpdated;
        }

        private void HomePage_Appearing(object sender, EventArgs e)
        {
            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;
        }

        public void RefreshYearlist()
        {
            PredictionWeek.Text = CurrentWeek();
            YearList.ItemsSource = NetworkDatabase.YearList;
        }

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                YearList.Position = NetworkDatabase.CurrentYear;
            });

        }

        private void YearList_PositionSelected(object sender, CarouselView.FormsPlugin.Abstractions.PositionSelectedEventArgs e)
        {
            if (YearList.Position > -1)
            {
                PredictionYear.Text = NetworkDatabase.YearList[YearList.Position].Season + " Predictions";
                if (NetworkDatabase.MaxYear > NetworkDatabase.YearList[YearList.Position].year)
                {
                    PredictionWeek.Text = "";
                    CurrentStatus.Text = "";
                    UseMenu.Text = "You are now viewing historical data. We have ratings and renewal/cancellation data going back to 2014. \r\n\r\n" +
                    "use the menu on the left to see what the current prediction models would have predicted for those shows";
                }
                else
                {
                    PredictionWeek.Text = CurrentWeek();
                    CurrentStatus.Text = NetworkDatabase.currentText;
                    UseMenu.Text = "use the menu on the left to see predictions for each network";

                    HyperlinkTimer.Stop();
                    HyperlinkTimer.Start();
                }

                timer.Stop();
                timer.Start();
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
            if (Device.RuntimePlatform == Device.Android)
            {
                YearList.Orientation =
                    CarouselViewOrientation.Vertical;
                YearList.Orientation =
                    CarouselViewOrientation.Horizontal;
            }

            //re-label Blog Post
            timer.Stop();
            timer.Start();
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

        private async void RefreshPredictions_Clicked(object sender, EventArgs e)
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