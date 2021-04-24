using System;
using System.Collections.Generic;
using System.ComponentModel;
using TV_Ratings_Predictions;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TVPredictionsViewer
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewPage : ContentPage
    {
        public TitleTemplate TitleBar
        {
            get
            {
                return Bar;
            }
        }

        ResultsList SearchResults;

        bool UsePrediction;
        PredictionContainer prediction;

        public ViewPage(ContentView View, string Title)
        {
            foreach (ToolbarItem t in new Toolbar(this, View).ToolBarItems)
                ToolbarItems.Add(t);

            InitializeComponent();

            var isFix = (View is FixShow);

            SearchResults = new ResultsList(isFix) { IsVisible = false };
            

            SearchResults.NavigationParent = this;

            if (View is FixShow)
            {
                SearchResults.ClickResult += SearchResults_ClickResult;

                if ((View as FixShow).IsInitialized)
                {
                    MainGrid.Children.Add(View);
                    view = (View as FixShow);
                    IsFixShowDisplayed = true;
                }                    
                else
                    MainGrid.Children.Add(new Label() { Text = "If a show is displaying the wrong image/description, please use the search button above to select the show that needs correcting.", FontSize = Device.GetNamedSize(NamedSize.Subtitle, typeof(Label)), Margin = 25 });
            }   
            else
                MainGrid.Children.Add(View);

            if (View is Settings)
            {
                var settings = View as Settings;
                UsePrediction = settings.UsePrediction;

                if (UsePrediction) prediction = settings.prediction;
            }
            else if (View is About)
            {
                var about = View as About;
                UsePrediction = about.UsePrediction;

                if (UsePrediction) prediction = about.prediction;
            }
            else if (View is FixShow)
            {
                var fix = View as FixShow;
                UsePrediction = fix.IsInitialized;

                if (UsePrediction) prediction = fix.prediction;
            }

            MainGrid.Children.Add(SearchResults);

            Bar.Title = Title;
        }

        bool IsFixShowDisplayed;
        FixShow view;

        private async void SearchResults_ClickResult(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                var p = SearchResults.SelectedItem;
                MainGrid.Children.Clear();
                SearchResults.Unfocus();


                view = new FixShow(p);

                MainGrid.Children.Add(view);
                MainGrid.Children.Add(SearchResults);

                IsFixShowDisplayed = true;
            }
            else
            {
                await DisplayAlert("TV Predictions", "Not Connected to the Internet! Try again later.", "Close");
                await Navigation.PopAsync();
            }
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
            else if (IsFixShowDisplayed && view.IsConfirmationDisplayed)
            {
                view.No_Clicked(this, new EventArgs());
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
    }

    public class DetailsContainer : INotifyPropertyChanged
    {
        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        double _value;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public double Value
        {
            get
            {
                return _value;
                //return Math.Round(_value, 4);
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
                OnPropertyChanged("FormattedValue");
            }
        }
        public string FormattedValue
        {
            get
            {
                if (Value == 0)
                    return "No Change";
                else
                    return _value.ToString("+0.00%; -0.00%");
            }
        }

        public DetailsContainer(string s, double d)
        {
            Name = s;
            _value = d;
        }
    }

    class DetailsCombo
    {
        public List<DetailsContainer> details;
        public int OptimalEpisodes;
        public double BaseOdds, CurrentOdds;

        public DetailsCombo(List<DetailsContainer> d, double b, double c)
        {
            details = d;
            BaseOdds = b;
            CurrentOdds = c;
        }
    }
}