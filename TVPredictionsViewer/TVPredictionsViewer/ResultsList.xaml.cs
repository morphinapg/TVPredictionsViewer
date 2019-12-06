using System;
using System.Collections.Concurrent;
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
    public partial class ResultsList : ContentView
    {
        ObservableCollection<ListOfPredictions> Results = new ObservableCollection<ListOfPredictions>();
        public Timer timer, UnfocusTimer;

        ContentPage _navigationParent = new ContentPage();
        public ContentPage NavigationParent
        {
            get
            {
                return _navigationParent;
            }
            set
            {
                _navigationParent = value;

                if (_navigationParent is HomePage)
                    TitleBar = (_navigationParent as HomePage).TitleBar;
                else if (_navigationParent is Predictions)
                    TitleBar = (_navigationParent as Predictions).TitleBar;
                else if (_navigationParent is ShowDetailPage)
                    TitleBar = (_navigationParent as ShowDetailPage).TitleBar;
                else if (_navigationParent is ViewPage)
                    TitleBar = (_navigationParent as ViewPage).TitleBar;
                else if (_navigationParent is ScoreBoard)
                    TitleBar = (_navigationParent as ScoreBoard).TitleBar;
            }
        }



        SearchBar Search = new SearchBar();
        Button SearchButton = new Button();
        Label TitleLabel = new Label();

        TitleTemplate _titleBar = new TitleTemplate();
        TitleTemplate TitleBar
        {
            get
            {
                return _titleBar;
            }
            set
            {
                _titleBar = value;
                Search = _titleBar.TBar;
                SearchButton = _titleBar.TButton;
                TitleLabel = _titleBar.TLabel;

                Search.Focused += Search_Focused;
                Search.TextChanged += Search_TextChanged;
                Search.SearchCommand = new Command(() =>
                {
                    Search.Unfocused -= Search_Unfocused;
                    MakeVisible();
                });

                SearchButton.Clicked += SearchButton_Clicked;

                if (Device.RuntimePlatform == Device.UWP)
                    SearchButton.Padding = new Thickness(15, 5, 15, 5);
            }
        }

        public new bool IsFocused
        {
            get
            {
                return Search.IsFocused;
            }
        }

        private void SearchButton_Clicked(object sender, EventArgs e)
        {
            MakeVisible();

            Search.Focus();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            Query = Search.Text;
        }

        private void Search_Focused(object sender, FocusEventArgs e)
        {
            MakeVisible();

            timer?.Dispose();
            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        void MakeVisible()
        {
            TitleLabel.IsVisible = false;
            SearchButton.IsVisible = false;
            Search.IsVisible = true;
            this.IsVisible = true;
        }

        public void MakeInvisible()
        {
            Search.IsVisible = false;
            TitleLabel.IsVisible = true;
            SearchButton.IsVisible = true;
            this.IsVisible = false;
            Search.Text = "";
        }

        private void Search_Unfocused(object sender, FocusEventArgs e)
        {
            UnfocusTimer?.Dispose();
            UnfocusTimer = new Timer(100);
            UnfocusTimer.Elapsed += (s, ex) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    MakeInvisible();
                    (s as Timer).Stop();
                });
            };
            UnfocusTimer.Start();

            Search.Unfocused -= Search_Unfocused;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Search.IsFocused)
            {
                Search.Unfocused += Search_Unfocused;
                (sender as Timer).Stop();
            }
            else
                Device.BeginInvokeOnMainThread(() => Search.Focus());
        }

        string _query;
        public string Query
        {
            get
            {
                return _query;
            }
            set
            {
                _query = value;
                Update_Results();
            }
        }

        bool Fix;

        public ResultsList()
        {
            InitializeComponent();
        }

        public ResultsList(bool isFix)
        {   
            InitializeComponent();

            Fix = isFix;
            SearchResults.IsGroupingEnabled = !isFix;
        }

        async void Update_Results()
        {
            Results.Clear();

            var FixResults = new ObservableCollection<PredictionContainer>();

            if (Query != "")
            {
                var Results_Local = new List<ListOfPredictions>();

                await Task.Run(() =>
                {
                    var tmpResults = new ConcurrentBag<PredictionContainer>();

                    foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                    {
                        var Adjustments = n.model.GetAdjustments(true);

                        n.shows.AsParallel().Where(x => x.Name.ToLower().Contains(Query.ToLower())).ForAll(s => tmpResults.Add(new PredictionContainer(s, n, Adjustments[s.year], n.model.GetNetworkRatingsThreshold(s.year), false, false)));
                    }

                    if (Fix)
                    {
                        tmpResults.OrderByDescending(x => x.Year).GroupBy(x => x.Name).Select(x => x.First()).OrderBy(x => x.Name).ToList().ForEach(x => FixResults.Add(x));
                    }
                    else
                    {
                        var YearList = tmpResults.Select(x => x.Year).Distinct().OrderByDescending(x => x);

                        foreach (int year in YearList)
                        {
                            var tmpList = tmpResults.Where(x => x.Year == year).OrderBy(x => x.Name);

                            var ResultsForYear = new ListOfPredictions() { Category = new Year(year).Season };

                            foreach (PredictionContainer p in tmpList)
                                ResultsForYear.Add(p);

                            Results_Local.Add(ResultsForYear);
                        }
                    }
                    
                });

                foreach (ListOfPredictions l in Results_Local)
                    Results.Add(l);
            }

            if (Fix)
                SearchResults.ItemsSource = FixResults;
            else
                SearchResults.ItemsSource = Results;
        }

        private void SearchResults_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (Fix)
            {
                ClickResult(this, new EventArgs());
            }
            else
            {
                var p = SearchResults.SelectedItem as PredictionContainer;
                p.IsShowPage = true;
                p.DisplayYear = null;
                NavigationParent.Navigation.PushAsync(new ShowDetailPage(p));
            }
        }

        public new void Unfocus()
        {
            Search.Unfocus();
        }

        public event EventHandler ClickResult;

        public PredictionContainer SelectedItem { get { return SearchResults.SelectedItem as PredictionContainer; } }
    }
}