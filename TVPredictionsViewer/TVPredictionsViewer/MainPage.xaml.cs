﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Xamarin.Forms;
using TV_Ratings_Predictions;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
using System.Net;

namespace TVPredictionsViewer
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : FlyoutPage
    {
        //ObservableCollection<MiniNetwork> Networks = new ObservableCollection<MiniNetwork>();
        //ObservableCollection<Year> Years = new ObservableCollection<Year>();
        public HomePage home = new HomePage();
        Timer SaveBackup = new Timer(1000);

        public MainPage()
        {

            //Detail = new NavigationPage(home); //{ BarBackgroundColor = (Color)Application.Current.Resources["TitleColor"], BarTextColor = (Color)Application.Current.Resources["TitleText"] };

           

            FlyoutLayoutBehavior = FlyoutLayoutBehavior.Popover;
            IsPresented = false;

            SaveBackup.Elapsed += SaveBackup_Elapsed;
            SaveBackup.Start();            

            InitializeComponent();

            home = (Detail as NavigationPage).CurrentPage as HomePage;

            if (Device.RuntimePlatform != Device.UWP)
                MenuHeader.IsVisible = true;
                

            if (Application.Current.Properties.ContainsKey("UseOdds"))
                NetworkDatabase.UseOdds = (bool)Application.Current.Properties["UseOdds"];

            CheckForUpdate();
        }

        public async void CheckForUpdate(bool resuming = false)
        {
            var BlogPath = Path.Combine(NetworkDatabase.Folder, "Update.txt");
            var SettingsPath = Path.Combine(NetworkDatabase.Folder, "Predictions.TVP");
            NetworkDatabase.mainpage = this;

            //Only download new predictions if it's been at least a week from the Saturday included in those predictions
            if (File.Exists(SettingsPath) && File.Exists(BlogPath))
            {
                var week = new PredictionWeek();
                if (DateTime.Now - week.Saturday > new TimeSpan(7, 0, 0, 0))
                    await NetworkDatabase.ReadUpdateAsync();
                else if (!resuming)
                    await NoUpdate();
            }
            else
                await NetworkDatabase.ReadUpdateAsync();
        }

        async Task NoUpdate()
        {
            await NetworkDatabase.AuthenticateTMDB();

            var FilePath = Path.Combine(NetworkDatabase.Folder, "Update.txt");

            Directory.CreateDirectory(NetworkDatabase.Folder);

            if (File.Exists(FilePath))
                using (var fs = new FileStream(FilePath, FileMode.Open))
                {
                    using (var reader = new StreamReader(fs))
                    {
                        NetworkDatabase.currentText = await reader.ReadToEndAsync();
                    }
                }
            CompletedUpdateAsync(this, new AsyncCompletedEventArgs(null, false, this));
        }

        private async void SaveBackup_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (NetworkDatabase.backup)
                await NetworkDatabase.SaveBackup();
        }

        public async void CompletedUpdateAsync(object sender, AsyncCompletedEventArgs e)
        {

            var FilePath = Path.Combine(NetworkDatabase.Folder, "Update.txt");

            if (e.Error != null && !File.Exists(FilePath))
            {
                _ = DisplayAlert("TV Predictions", e.Error.Message, "Close");
                home.IncompleteUpdate();
            }
            else
            {
                string text;




                try
                {
                    using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new StreamReader(fs))
                        {
                            text = await reader.ReadToEndAsync();
                        }
                    }

                    if (text != NetworkDatabase.currentText)
                    {
                        NetworkDatabase.currentText = text;
                        await home.Navigation.PopToRootAsync();
                        home.Downloading();
                        NetworkDatabase.ReadSettings(this);
                    }
                    else
                    {
                        if (File.Exists(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP")))
                        {

                            var ee = new AsyncCompletedEventArgs(null, false, this);
                            CompletedSettings(this, ee);
                        }
                        else
                        {
                            await home.Navigation.PopToRootAsync();
                            home.Downloading();
                            NetworkDatabase.ReadSettings(this);
                        }
                        NetworkDatabase.IsLoaded = true;
                    }
                }
                catch (Exception)
                {
                    if (!home.Completed)
                    {
                        _ = DisplayAlert("TV Predictions", "Error reading predictions. Try again.", "Close");
                        home.IncompleteUpdate();
                    }
                    else
                        home.CompletedSettings();
                }
            }

            NetworkDatabase.CurrentlyLoading = false;
        }

        public async void CompletedSettings(object sender, AsyncCompletedEventArgs e)
        {
            home.CompletedUpdate(NetworkDatabase.currentText);

            if (e.Error != null)
            {
                await DisplayAlert("TV Predictions", e.Error.Message, "Close");
                home.IncompleteUpdate();
            }
            else
            {
                bool error = false;

                await Task.Run(() =>
                {
                    try
                    {
                        using (var fs = new FileStream(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP"), FileMode.Open))
                        {
                            var serializer = new DataContractSerializer(typeof(List<MiniNetwork>));
                            NetworkDatabase.NetworkList = new ObservableCollection<MiniNetwork>((List<MiniNetwork>)serializer.ReadObject(fs));
                        }

                        NetworkDatabase.YearList = new List<Year>(NetworkDatabase.NetworkList.AsParallel().SelectMany(x => x.shows).Select(x => x.year).Distinct().OrderBy(x => x).Select(x => new Year(x)));
                        var count = NetworkDatabase.YearList.Count;

                        NetworkDatabase.MaxYear = NetworkDatabase.YearList[count - 1];
                        NetworkDatabase.CurrentYear = count - 1;

                        foreach (MiniNetwork n in NetworkDatabase.NetworkList)
                        {
                            n.FilteredShows = new ObservableCollection<Show>();
                            n.Predictions = new ObservableCollection<ListOfPredictions>();
                            n.model.shows = n.shows;
                            n.pendingFilter = true;

                            Parallel.ForEach(n.shows, s =>
                            {
                                s.factorNames = n.factors;
                                s.network = n;
                            });
                            //n.model.TestAccuracy(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        error = true;
                    }

                });

                if (error)
                {
                    if (!home.Completed)
                    {
                        home.IncompleteUpdate();
                        _ = DisplayAlert("TV Predictions", "Could not read predictions file. Please try again.", "Close");                        
                    }
                    else
                        home.CompletedSettings();
                }
                else
                {
                    NetworkList.ItemsSource = NetworkDatabase.NetworkList;
                    NetworkList.IsVisible = true;

                    if (File.Exists(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP")))
                        File.SetLastWriteTime(Path.Combine(NetworkDatabase.Folder, "Predictions.TVP"), NetworkDatabase.NetworkList.FirstOrDefault().PredictionTime);

                    AllNetworks.IsVisible = true;

                    if (!home.Completed)
                        home.CompletedSettings();
                    else
                        home.RefreshYearlist();
                }
            }
        }

        public void HideNetworks()
        {
            NetworkList.IsVisible = false;
            AllNetworks.IsVisible = false;
        }

        private void NetworkList_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            home.NavigateToNetwork(NetworkList.SelectedItem as MiniNetwork);
            IsPresented = false;
        }

        protected override void OnSizeAllocated(double dblWidth, double dblHeight)
        {
            base.OnSizeAllocated(dblWidth, dblHeight);

            
            //bool isDesktop = dblWidth > (1080);

        }

        private void AllNetworks_Clicked(object sender, EventArgs e)
        {
            home.NavigateToAllNetworks();
            IsPresented = false;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            MessagingCenter.Subscribe<object, string>(this, App.NotificationReceivedKey, OnMessageReceived);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<object>(this, App.NotificationReceivedKey);
        }

        void OnMessageReceived(object sender, string msg)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (msg != "IntentTap")
                {
                    DisplayAlert("TV Predictions", "New Predictions Received: \r\n\r\n" + msg, "OK");
                    home.RefreshPredictions_Clicked(this, new EventArgs());
                }
            });
        }
    }
}
