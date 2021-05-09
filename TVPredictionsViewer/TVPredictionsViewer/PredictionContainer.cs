using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TV_Ratings_Predictions
{
    public class PredictionContainer : IComparable<PredictionContainer>, INotifyPropertyChanged
    {
        public Show show;
        public MiniNetwork network;
        public bool UseNetwork;
        double targetrating;
        public double odds, finalodds;
        public bool? DisplayYear;

        public bool UseFinal { get; set; }

        public string Name
        {
            get
            {
                bool year = DisplayYear is null ? show.year != NetworkDatabase.YearList[NetworkDatabase.CurrentYear] : (bool)DisplayYear;

                return year ? show.Name + " (" + new Year(show.year).Season + ")" : show.Name;
            }
        }

        public string NewShow
        {
            get
            {
                if (show.OldOdds == 0 && show.OldRating == 0)
                    return "(NEW)";
                else
                    return "";
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Prediction
        {
            get
            {
                if (NetworkDatabase.UseOdds)
                {
                    return Math.Round(odds * 100, 0) + "% Odds of Renewal";
                }
                else
                {
                    if (odds > 0.5)
                    {
                        return "Renewal Confidence: " + Math.Round((odds - 0.5) * 200, 0) + "%";
                    }
                    else if (odds < 0.5)
                    {
                        return "Cancellation Confidence: " + Math.Round((0.5 - odds) * 200, 0) + "%";
                    }
                    else
                    {
                        if (show.AverageRating > targetrating)
                            return "Renewal Confidence: 0%";
                        else
                            return "Cancellation Confidence: 0%";
                    }
                }
            }
        }

        public string FinalPrediction
        {
            get
            {
                if (NetworkDatabase.UseOdds)
                {
                    var o = UseFinal ? finalodds : odds;

                    return Math.Round(o * 100, 0) + "% Odds of Renewal";
                }
                else
                {                   

                    var o = UseFinal ? finalodds : odds;

                    if (o > 0.5)
                    {
                        return "Renewed (" + Math.Round((o - 0.5) * 200, 0) + "% Confidence)";
                    }
                    else if (o < 0.5)
                    {
                        return "Canceled (" + Math.Round((0.5 - o) * 200, 0) + "% Confidence)";
                    }
                    else
                    {
                        if (show.AverageRating > targetrating)
                            return "Renewed (0% Confidence)";
                        else
                            return "Canceled (0% Confidence)";
                    }
                }
            }
        }

        public string FinalText
        {
            get
            {
                if (!(show.Renewed || show.Canceled))
                    return "The current prediction is:";

                if (UseFinal)
                    return "The final prediction was:";
                else
                    return "The current model would predict:";

            }
        }

        public string Category
        {
            get
            {
                if (odds > 0.8)
                    return "Certain Renewal";
                else if (odds > 0.6)
                    return "Likely Renewal";
                else if (odds > 0.5)
                    return "Leaning Towards Renewal";
                else if (odds == 0.5)
                {
                    if (show.AverageRating > targetrating)
                        return "Leaning Towards Renewal";
                    else
                        return "Leaning Towards Cancellation";
                }
                else if (odds > 0.4)
                    return "Leaning Towards Cancellation";
                else if (odds > 0.2)
                    return "Likely Cancellation";
                else
                    return "Certain Cancellation";
            }
        }

        public string Status
        {
            get
            {
                return show.RenewalStatus;
            }
        }

        public int StatusIndex
        {
            get
            {
                if (show.Renewed)
                    return 1;
                else if (show.Canceled)
                    return -1;
                else
                    return 0;
            }
        }

        bool _showdetails;
        public bool ShowDetails
        {
            get
            {
                if (IsShowPage)
                    return false;
                else
                    return _showdetails;
            }
            set
            {
                _showdetails = value;
                OnPropertyChanged("ShowDetails");
            }
        }

        public double AccuracyNumber
        {
            get
            {
                if (show.Renewed)
                    return (odds >= 0.5) ? 1 : -1;
                else if (show.Canceled)
                    return (odds <= 0.5) ? 1 : -1;
                else
                    return 0;
            }
        }
        public string AccuracyString
        {
            get
            {
                var o = UseFinal ? finalodds : odds;

                if (show.Renewed)
                    return (o >= 0.5) ? "✔" : "❌";
                else if (show.Canceled)
                    return (o <= 0.5) ? "✔" : "❌";
                else
                    return "";
            }
        }

        public string Rating
        {
            get
            {
                return ((show.ratings.Count < show.Episodes) ? "Projected " : "") + "Season Rating: " + show.AverageRating.ToString("N2");
            }
        }

        public string TargetRating
        {
            get
            {
                return "Estimated Renewal Threshold: " + targetrating.ToString("N2");
            }
        }

        double _networkaverage;
        public string NetworkAverage
        {
            get
            {
                return network.Name + " Typical Renewal Threshold: " + _networkaverage.ToString("N2");
            }
        }

        public double RatingsDiff
        {
            get
            {
                if (!(show.OldRating == 0 && show.OldOdds == 0))
                    return Math.Round(show.AverageRating - show.OldRating, 2);
                else
                    return 0;
            }
        }

        public string RatingDifference
        {
            get
            {
                if (show.ratings.Count > 0)
                    return (RatingsDiff != 0) ? RatingsDiff.ToString("+0.00; -0.00") : "";
                else
                    return "";
            }
        }

        public double PredictionDiff
        {
            get
            {
                if (!(show.OldRating == 0 && show.OldOdds == 0))
                {
                    if (NetworkDatabase.UseOdds)
                        return (show.OldRating == 0) ? 0 : Math.Round(odds - show.OldOdds, 2);
                    else
                        return (show.OldRating == 0) ? 0 : Math.Round((odds - show.OldOdds) * 2, 2);
                }
                else
                    return 0;
            }
        }

        public string PredictionDifference
        {
            get
            {
                if (show.ratings.Count > 0)
                {
                    return (PredictionDiff != 0 && Status == "") ? PredictionDiff.ToString("0% better than last week; 0% worse than last week") : "";
                }
                else
                    return "";
            }
        }

        public string Change
        {
            get
            {
                if (show.ratings.Count > 0)
                {
                    if (PredictionDiff != 0 && Status == "")
                        return (PredictionDiff > 0) ? "↑" : "↓";
                    else
                        return "";
                }
                else
                    return "";
            }
        }

        string _overview;
        public string Overview
        {
            get
            {
                return _overview;
            }
            set
            {
                _overview = value;
                OnPropertyChanged("Overview");
            }
        }

        bool _showpage;
        public bool IsShowPage
        {
            get
            {
                return _showpage;
            }
            set
            {
                _showpage = value;
                OnPropertyChanged("ShowDetails");
            }
        }

        bool _isLoaded;
        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
            set
            {
                _isLoaded = value;
                OnPropertyChanged("IsLoaded");
            }
        }

        public int Year
        {
            get
            {
                return show.year;
            }
        }

        public string NetworkName
        {
            get
            {
                return network.name;
            }
        }

        bool _final;
        public bool ShowFinal
        {
            get
            {
                return _final;
            }
            set
            {
                _final = value;
                OnPropertyChanged("ShowFinal");
            }
        }

        public string Season
        {
            get
            {
                var hundredpart = show.Season / 100;
                var remainder = show.Season - hundredpart * 100;
                var tenpart = remainder / 10;
                if (tenpart == 1)
                    return show.Season + "th Season";
                else
                {
                    switch (show.Season % 10)
                    {
                        case 1:
                            return show.Season + "st Season";
                        case 2:
                            return show.Season + "nd Season";
                        case 3:
                            return show.Season + "rd Season";
                        default:
                            return show.Season + "th Season";
                    }
                }
            }
        }

        public PredictionContainer(Show s, MiniNetwork n, double adjustment, double average, bool FromNetwork = false, bool? year = null, bool final = false)
        {
            network = n;
            UseNetwork = FromNetwork;
            var model = network.model;
            show = s;
            odds = s.PredictedOdds;
            targetrating = model.GetTargetRating(s.year, model.GetThreshold(s, n.FactorAverages, adjustment));
            ShowDetails = false;
            IsShowPage = false;
            IsLoaded = false;
            _networkaverage = average;
            finalodds = s.Renewed || s.Canceled ? s.FinalPrediction : s.PredictedOdds;
            NetworkDatabase.CurrentYearUpdated += NetworkDatabase_CurrentYearUpdated;
            DisplayYear = year;
            UseFinal = final;
        }

        private void NetworkDatabase_CurrentYearUpdated(object sender, EventArgs e)
        {
            OnPropertyChanged("Name");
        }

        public int CompareTo(PredictionContainer other)
        {
            return other.odds.CompareTo(odds);
        }
    }
    public class ListOfPredictions : List<PredictionContainer>
    {
        public string Category { get; set; }
        public List<PredictionContainer> Predictions => this;
    }
}
