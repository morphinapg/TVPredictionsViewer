using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TV_Ratings_Predictions;
using Xamarin.Forms;

namespace TVPredictionsViewer
{
    public class ShowHighlights : INotifyPropertyChanged
    {
        PredictionContainer show;
        Uri imagesource;
        int Category;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
        public string Description { get; }
        public string Prediction { get; }

        public double RenewalIndex { get; }

        public string ShowName => show.show.Name;

        public ShowHighlights (PredictionContainer p, int category)
        {
            show = p;
            Category = category;

            GetImage();

            switch (category)
            {
                case 0:
                    NewShow = (show.show.Season == 1) ? "Series Premiere" : "Season Premiere";
                    Prediction = show.Prediction;
                    RenewalIndex = show.show.PredictedOdds > 0.5 ? 1 : -1;
                    break;
                case 1:
                    Prediction = show.Status;
                    RenewalIndex = show.show.Renewed ? 1 : -1;
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
            var ID = await NetworkDatabase.GetShowID(show.show.Name, show.network.name);

            imagesource = await NetworkDatabase.GetImageURI(ID);

            OnPropertyChanged("ImageUri");
            OnPropertyChanged("ImageURL");
        }
    }
}
