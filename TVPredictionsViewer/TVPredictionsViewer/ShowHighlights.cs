using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using TV_Ratings_Predictions;
using Xamarin.Forms;

namespace TVPredictionsViewer
{
    public class ShowHighlights : INotifyPropertyChanged
    {
        PredictionContainer show;
        Uri imagesource;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        bool _visibility = true;
        public bool ActivityVisibility => _visibility;

        public UriImageSource ImageUri
        {
            get
            {
                if (imagesource is null)
                    return null;

                return new UriImageSource
                {
                    Uri = imagesource,
                    CachingEnabled = true,
                    CacheValidity = new TimeSpan(90, 0, 0, 0)
                };
            }
        }

        public string ImageURL
        {
            get
            {
                if (imagesource is null)
                    return null;
                return imagesource.AbsoluteUri;
            }
        }

        public string NewShow { get; }
        public FormattedString Description { get; }
        public string Prediction { get; }

        public double RenewalIndex { get; }

        public string ShowName => show.show.Name;

        public ShowHighlights (PredictionContainer p, int category)
        {
            show = p;

            GetImage();

            switch (category)
            {
                case 0:
                    NewShow = (show.show.Season == 1) ? "Series Premiere" : "Season Premiere";
                    Prediction = show.Status == "" ? show.Category : show.Status;
                    if (show.show.PredictedOdds == 0.5) 
                        RenewalIndex = (show.show.ShowIndex > show.show._calculatedThreshold) ? 1 : -1;
                    else
                        RenewalIndex = show.show.PredictedOdds > 0.5 ? 1 : -1;                    
                    Description = show.Season;
                    break;
                case 1:
                    Prediction = show.Status;
                    RenewalIndex = show.show.Renewed ? 1 : -1;
                    {
                        var correct = (show.show.Renewed && show.show.FinalPrediction >= 0.5) || (show.show.Canceled && show.show.FinalPrediction <= 0.5);

                        var formats = new FormattedString();
                        var part1 = new Span { Text = correct ? "Prediction was correct " : "Prediction was incorrect "};
                        var part2 = correct ? new Span { Text = "✔", TextColor = Color.Green } : new Span { Text = "❌", TextColor = Color.Red };
                        formats.Spans.Add(part1);
                        formats.Spans.Add(part2);
                        Description = formats;
                    }         
                    break;
                case 2:
                    Prediction = show.Category;
                    if (show.show.PredictedOdds == 0.5)
                        RenewalIndex = (show.show.ShowIndex > show.show._calculatedThreshold) ? 1 : -1;
                    else
                        RenewalIndex = show.show.PredictedOdds > 0.5 ? 1 : -1;
                    {
                        string OldCategory;
                        if (show.show.OldOdds > 0.8)
                            OldCategory = "Certain Renewal";
                        else if (show.show.OldOdds > 0.6)
                            OldCategory = "Likely Renewal";
                        else if (show.show.OldOdds > 0.5)
                            OldCategory = "Leaning Towards Renewal";
                        else if (show.show.OldOdds == 0.5)
                        {
                            if (show.show.ShowIndex > show.show._calculatedThreshold)
                                OldCategory = "Leaning Towards Renewal";
                            else
                                OldCategory = "Leaning Towards Cancellation";
                        }
                        else if (show.show.OldOdds > 0.4)
                            OldCategory = "Leaning Towards Cancellation";
                        else if (show.show.OldOdds > 0.2)
                            OldCategory = "Likely Cancellation";
                        else
                            OldCategory = "Certain Cancellation";                                        
                        Description = (show.show.PredictedOdds > show.show.OldOdds ? "Upgraded from " : "Downgraded from ") + OldCategory;
                    }
                    break;
            }
        }

        //public async Task<bool> GetImage()
        //{
        //    var ID = await NetworkDatabase.GetShowID(show.Name, show.network.name);

        //    imagesource = await NetworkDatabase.GetImageURI(ID);

        //    return true;
        //}

        async void GetImage()
        {
            await Task.Run(async () =>
            {
                var ID = await NetworkDatabase.GetShowID(show.show.Name, show.network.name);

                imagesource = await NetworkDatabase.GetImageURI(ID);

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    OnPropertyChanged("ImageUri");
                    OnPropertyChanged("ImageURL");
                    _visibility = false;
                    OnPropertyChanged("ActivityVisibility");
                });
            });
        }

        public void Navigate(INavigation Navigation)
        {
            Navigation.PushModalAsync(new ShowDetailPage(show));
        }
    }
}
