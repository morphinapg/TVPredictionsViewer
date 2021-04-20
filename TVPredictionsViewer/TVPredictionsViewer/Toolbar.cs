using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TV_Ratings_Predictions;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TVPredictionsViewer
{
    class Toolbar
    {
        public List<ToolbarItem> ToolBarItems
        {
            get
            {
                var list = new List<ToolbarItem>();

                if (!isScoreboardPage)
                {
                    var results = new ToolbarItem() { Text = "Prediction Results", Order = ToolbarItemOrder.Secondary };
                    results.Clicked += Results_Clicked;
                    results.Command = new Command(() => Results());
                    list.Add(results);
                }                

                if (UsePrediction)
                {
                    var prediction = new ToolbarItem() { Text = "Fix Show Details", Order = ToolbarItemOrder.Secondary };
                    prediction.Clicked += Prediction_Clicked;
                    prediction.Command = new Command(() => Prediction());
                    list.Add(prediction);
                }

                if (!isSettingsPage)
                {
                    var settings = new ToolbarItem() { Text = "Settings", Order = ToolbarItemOrder.Secondary };
                    settings.Clicked += Settings_Clicked;
                    settings.Command = new Command(() => Settings());
                    list.Add(settings);
                }

                var about = new ToolbarItem() { Text = "About", Order = ToolbarItemOrder.Secondary };
                about.Clicked += About_Clicked;
                about.Command = new Command(() => About());
                list.Add(about);

                return list;
            }
        }

        private async void Results_Clicked(object sender, EventArgs e)
        {
            await Parent.Navigation.PushAsync(new ScoreBoard(network));
        }

        private async void Results()
        {
            var page = NetworkDatabase.mainpage.Detail;

            await page.Navigation.PopModalAsync();
            await page.Navigation.PushAsync(new ScoreBoard(network));
        }

        private async void About_Clicked(object sender, EventArgs e)
        {
            await Parent.Navigation.PushAsync(new ViewPage(new About(), "About"));
        }

        private async void About()
        {
            var page = NetworkDatabase.mainpage.Detail;

            await page.Navigation.PopModalAsync();

            await page.Navigation.PushAsync(new ViewPage(new About(), "About"));
        }

        private async void Prediction_Clicked(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                await Parent.Navigation.PushAsync(new ViewPage(new FixShow(prediction), "Fix Show Details"));
            else
                await Parent.DisplayAlert("TV Predictions", "Not Connected to the Internet! Try again later.", "Close");
        }

        private async void Prediction()
        {
            var page = NetworkDatabase.mainpage.Detail;

            await page.Navigation.PopModalAsync();

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                await page.Navigation.PushAsync(new ViewPage(new FixShow(prediction), "Fix Show Details"));
            else
                await page.DisplayAlert("TV Predictions", "Not Connected to the Internet! Try again later.", "Close");
        }

        private async void Settings_Clicked(object sender, EventArgs e)
        {
            if (UsePredictionList)
                await Parent.Navigation.PushAsync(new ViewPage(new Settings(Parent, PredictionList), "Settings"));
            else if (UseNetwork && UsePrediction)
                await Parent.Navigation.PushAsync(new ViewPage(new Settings(network, prediction), "Settings"));
            else if (UseNetwork)
                await Parent.Navigation.PushAsync(new ViewPage(new Settings(Parent, network), "Settings"));
            else if (UsePrediction)
                await Parent.Navigation.PushAsync(new ViewPage(new Settings(prediction), "Settings"));
            else
                await Parent.Navigation.PushAsync(new ViewPage(new Settings(), "Settings"));
        }

        private async void Settings()
        {
            var page = NetworkDatabase.mainpage.Detail;

            await page.Navigation.PopModalAsync();

            if (UsePredictionList)
                await page.Navigation.PushAsync(new ViewPage(new Settings(Parent, PredictionList), "Settings"));
            else if (UseNetwork && UsePrediction)
                await page.Navigation.PushAsync(new ViewPage(new Settings(network, prediction), "Settings"));
            else if (UseNetwork)
                await page.Navigation.PushAsync(new ViewPage(new Settings(Parent, network), "Settings"));
            else if (UsePrediction)
                await page.Navigation.PushAsync(new ViewPage(new Settings(prediction), "Settings"));
            else
                await page.Navigation.PushAsync(new ViewPage(new Settings(), "Settings"));
        }

        MiniNetwork network;
        bool UseNetwork;
        ContentPage Parent;

        PredictionContainer prediction;
        bool UsePrediction;

        ObservableCollection<ListOfPredictions> PredictionList;
        bool UsePredictionList;

        bool isSettingsPage, isScoreboardPage;

        public Toolbar(ContentPage page)
        {
            Parent = page;
        }

        public Toolbar(ContentPage page, ContentView view)
        {
            Parent = page;

            isSettingsPage = view is Settings;
        }

        public Toolbar(ContentPage page, ref ObservableCollection<ListOfPredictions> predictions)
        {
            PredictionList = predictions;
            UsePredictionList = true;
            Parent = page;

            isScoreboardPage = page is ScoreBoard;
        }

        public Toolbar(ContentPage page, MiniNetwork n)
        {
            network = n;
            UseNetwork = true;
            Parent = page;
        }

        public Toolbar(ContentPage page, MiniNetwork n, PredictionContainer p)
        {
            network = n;
            UseNetwork = true;
            prediction = p;
            UsePrediction = true;
            Parent = page;
        }

        public Toolbar(ContentPage page, PredictionContainer p)
        {
            prediction = p;
            UsePrediction = true;

            Parent = page;
        }
    }
}
