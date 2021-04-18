using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TVPredictionsViewer;
using Xamarin.Forms;
using System.Globalization;
using TvDbSharper;
using Plugin.Connectivity;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using MathNet.Numerics.Distributions;
using TMDbLib.Client;

namespace TV_Ratings_Predictions
{
    [Serializable]
    public class MiniNetwork
    {
        public string name;
        public ObservableCollection<string> factors;
        public List<Show> shows;
        public NeuralPredictionModel model;
        public Dictionary<int, double> Adjustments;
        public double[] RatingsAverages, FactorAverages;
        public DateTime PredictionTime;

        public double[][] deviations;
        public double[] typicalDeviation;
        public double TargetError;
        public double SeasonDeviation;

        [NonSerialized]
        public bool pendingFilter = false;

        [NonSerialized]
        public ObservableCollection<Show> FilteredShows = new ObservableCollection<Show>();

        [NonSerialized]
        public ObservableCollection<ListOfPredictions> Predictions = new ObservableCollection<ListOfPredictions>();

        public string Name
        {
            get
            {
                return name;
            }
        }

        public void Filter(bool BlankPredictions = false, bool UseFinal = false, bool UseYear = false, List<Year> years = null)
        {
            Predictions.Clear();
            FilteredShows.Clear();
            var year = NetworkDatabase.YearList[NetworkDatabase.CurrentYear];
            var Adjustments = model.GetAdjustments(true);
            var average = model.GetNetworkRatingsThreshold(year);

            if (!UseYear)
                years = new List<Year>() { new Year(NetworkDatabase.YearList[NetworkDatabase.CurrentYear]) };

            shows.Where(x => years.Select(y => y.year).Contains(x.year)).ToList().ForEach(x => FilteredShows.Add(x));

            if (BlankPredictions)
            {                
                var p = new ListOfPredictions { Category = "Predictions" };
                
                foreach (Show s in FilteredShows)
                    p.Add(new PredictionContainer(s, this, Adjustments[year], average, false, UseYear, UseFinal));

                if (Predictions.Count > 0)
                    throw new Exception("Predictions were not empty!");

                Predictions.Add(p);
            }
            else
            {
                if (Application.Current.Properties.ContainsKey("PredictionSort"))
                {
                    switch (Application.Current.Properties["PredictionSort"] as string)
                    {
                        case "Ratings":
                            {
                                var TempList = FilteredShows.AsParallel().Select(x => new PredictionContainer(x, this, Adjustments[x.year], average, true, false)).OrderByDescending(x => x.show.AverageRating).ToList();
                                MiniNetwork.AddPredictions_Ratings(TempList, ref Predictions);
                                break;
                            }
                        case "Name":
                            {
                                var TempList = shows.AsParallel().Where(x => x.year == year).Select(x => new PredictionContainer(x, this, Adjustments[x.year], average, true, false)).OrderBy(x => x.Name).ToList();
                                MiniNetwork.AddPredictions_Name(TempList, ref Predictions);
                                break;
                            }
                        default:
                            {
                                Filter_Odds(average);
                                break;
                            }
                    }
                }
                else
                    Filter_Odds(average);

                pendingFilter = false;
            }            
        }

        void Filter_Odds(double average)
        {
            var tempPredictions = FilteredShows.AsParallel().Select(x => new PredictionContainer(x, this, Adjustments[x.year], average, true, false)).OrderByDescending(x => x.odds);
            MiniNetwork.AddPredictions_Odds(tempPredictions, ref Predictions);
        }

        public static void AddPredictions_Odds(OrderedParallelQuery<PredictionContainer> tempPredictions, ref ObservableCollection<ListOfPredictions> Predictions, bool UseFinal = false)
        {
            Predictions.Clear();

            ListOfPredictions
                CertainRenewed = new ListOfPredictions { Category = "Certain Renewal" },
                LikelyRenewed = new ListOfPredictions { Category = "Likely Renewal" },
                LeaningRenewed = new ListOfPredictions { Category = "Leaning Towards Renewal" },
                LeaningCanceled = new ListOfPredictions { Category = "Leaning Towards Cancellation" },
                LikelyCanceled = new ListOfPredictions { Category = "Likely Cancellation" },
                CertainCanceled = new ListOfPredictions { Category = "Certain Cancellation" };

            foreach (PredictionContainer p in tempPredictions)
            {
                var odds = UseFinal ? p.finalodds : p.odds;

                if (odds > 0.8)
                    CertainRenewed.Add(p);
                else if (odds > 0.6)
                    LikelyRenewed.Add(p);
                else if (odds > 0.5)
                    LeaningRenewed.Add(p);
                else if (odds > 0.4)
                    LeaningCanceled.Add(p);
                else if (odds > 0.2)
                    LikelyCanceled.Add(p);
                else
                    CertainCanceled.Add(p);
            }

            if (CertainRenewed.Count > 0)
                Predictions.Add(CertainRenewed);
            if (LikelyRenewed.Count > 0)
                Predictions.Add(LikelyRenewed);
            if (LeaningRenewed.Count > 0)
                Predictions.Add(LeaningRenewed);
            if (LeaningCanceled.Count > 0)
                Predictions.Add(LeaningCanceled);
            if (LikelyCanceled.Count > 0)
                Predictions.Add(LikelyCanceled);
            if (CertainCanceled.Count > 0)
                Predictions.Add(CertainCanceled);
        }

        public static void AddPredictions_Ratings(List<PredictionContainer> TempList, ref ObservableCollection<ListOfPredictions> Predictions)
        {
            Predictions.Clear();

            var count = TempList.Count() - 1;

            //Determine Percentile rating scores
            double
                P100 = (count > -1) ? Math.Round(TempList[0].show.AverageRating, 2) : 0,
                P80 = (count > -1) ? Math.Round(TempList[count / 5].show.AverageRating, 2) : 0,
                P60 = (count > -1) ? Math.Round(TempList[count * 2 / 5].show.AverageRating, 2) : 0,
                P40 = (count > -1) ? Math.Round(TempList[count * 3 / 5].show.AverageRating, 2) : 0,
                P20 = (count > -1) ? Math.Round(TempList[count * 4 / 5].show.AverageRating, 2) : 0,
                P0 = (count > -1) ? Math.Round(TempList[count].show.AverageRating, 2) : 0;

            ListOfPredictions
                HighRatings = new ListOfPredictions { Category = (P80 == P100) ? P80.ToString("N2") : P80.ToString("N2") + " - " + P100.ToString("N2") },
                GoodRatings = new ListOfPredictions { Category = (P60 == P80 - 0.01) ? P60.ToString("N2") : P60.ToString("N2") + " - " + (P80 - 0.01).ToString("N2") },
                MediumRatings = new ListOfPredictions { Category = (P40 == P60 - 0.01) ? P40.ToString("N2") : P40.ToString("N2") + " - " + (P60 - 0.01).ToString("N2") },
                PoorRatings = new ListOfPredictions { Category = (P20 == P40 - 0.01) ? P20.ToString("N2") : P20.ToString("N2") + " - " + (P40 - 0.01).ToString("N2") },
                LowRatings = new ListOfPredictions { Category = (P0 == P20 - 0.01) ? P0.ToString("N2") : P0.ToString("N2") + " - " + (P20 - 0.01).ToString("N2") };

            foreach (PredictionContainer p in TempList)
            {
                var rating = Math.Round(p.show.AverageRating, 2);

                if (rating >= P80)
                    HighRatings.Add(p);
                else if (rating >= P60)
                    GoodRatings.Add(p);
                else if (rating >= P40)
                    MediumRatings.Add(p);
                else if (rating >= P20)
                    PoorRatings.Add(p);
                else
                    LowRatings.Add(p);
            }

            if (HighRatings.Count > 0)
                Predictions.Add(HighRatings);
            if (GoodRatings.Count > 0)
                Predictions.Add(GoodRatings);
            if (MediumRatings.Count > 0)
                Predictions.Add(MediumRatings);
            if (PoorRatings.Count > 0)
                Predictions.Add(PoorRatings);
            if (LowRatings.Count > 0)
                Predictions.Add(LowRatings);
        }

        public static void AddPredictions_Name(List<PredictionContainer> TempList, ref ObservableCollection<ListOfPredictions> Predictions)
        {
            Predictions.Clear();

            var count = TempList.Count() - 1;

            //Determine Percentile Letters
            char
                P100 = (count > -1) ? TempList[0].Name.ToUpper()[0] : 'A',
                P80 = (count > -1) ? TempList[count / 5].Name.ToUpper()[0] : 'F',
                P60 = (count > -1) ? TempList[count * 2 / 5].Name.ToUpper()[0] : 'K',
                P40 = (count > -1) ? TempList[count * 3 / 5].Name.ToUpper()[0] : 'P',
                P20 = (count > -1) ? TempList[count * 4 / 5].Name.ToUpper()[0] : 'U',
                P0 = (count > -1) ? TempList[count].Name.ToUpper()[0] : 'Z';

            ListOfPredictions
                First = new ListOfPredictions { Category = (P100 == P80) ? P80.ToString() : P100 + " - " + P80 },
                Second = new ListOfPredictions { Category = (P80 + 1 == P60) ? P60.ToString() : (char)(P80 + 1) + " - " + P60 },
                Third = new ListOfPredictions { Category = (P60 + 1 == P40) ? P40.ToString() : (char)(P60 + 1) + " - " + P40 },
                Fourth = new ListOfPredictions { Category = (P40 + 1 == P20) ? P20.ToString() : (char)(P40 + 1) + " - " + P20 },
                Fifth = new ListOfPredictions { Category = (P20 + 1 == P0) ? P0.ToString() : (char)(P20 + 1) + " - " + P0 };

            foreach (PredictionContainer p in TempList)
            {
                if (p.Name[0] <= P80)
                    First.Add(p);
                else if (p.Name[0] <= P60)
                    Second.Add(p);
                else if (p.Name[0] <= P40)
                    Third.Add(p);
                else if (p.Name[0] <= P20)
                    Fourth.Add(p);
                else
                    Fifth.Add(p);
            }

            if (First.Count > 0)
                Predictions.Add(First);
            if (Second.Count > 0)
                Predictions.Add(Second);
            if (Third.Count > 0)
                Predictions.Add(Third);
            if (Fourth.Count > 0)
                Predictions.Add(Fourth);
            if (Fifth.Count > 0)
                Predictions.Add(Fifth);
        }

        public double AdjustAverage(int currentEpisode, int finalEpisode, double currentDrop = -1)   //This applies the typical ratings falloff values to the current weighted ratings average for a show
        {                                                                   //The result is a prediction for where the show's weighted ratings average will be at the end of the season
            try                                                             //This allows for more of a fair comparison between shows at different points in their seasons
            {
                double ExpectedDrop = (currentDrop == -1 || currentEpisode == 1) ? 1 : RatingsAverages[currentEpisode - 1] / RatingsAverages[0];
                double slope = (currentDrop == -1 || currentEpisode == 1) ? 1 : Math.Log10(currentDrop) / Math.Log10(ExpectedDrop);

                if (currentEpisode == 2) slope = (slope + 1) / 2;

                double PredictedDrop = Math.Log10(RatingsAverages[finalEpisode - 1] / RatingsAverages[currentEpisode - 1]);

                //return ratingsAverages[finalEpisode - 1] / ratingsAverages[currentEpisode - 1];
                return Math.Pow(10, PredictedDrop * slope);
            }
            catch
            {
                return 1;
            }
        }

        
    }

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
                    return (odds > 0.5) ? 1 : -1;
                else if (show.Canceled)
                    return (odds < 0.5) ? 1 : -1;
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
                    return (o > 0.5) ? "✔" : "❌";
                else if (show.Canceled)
                    return (o < 0.5) ? "✔" : "❌";
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
                OnPropertyChanged("isLoaded");
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
            finalodds = s.FinalPrediction;
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

    [Serializable]
    public class Show : IComparable<Show>
    {
        public double[] ratingsAverages;

        [NonSerialized]
        public double _calculatedThreshold;

        [NonSerialized]
        public MiniNetwork network;

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
        public ObservableCollection<bool> factorValues;

        [NonSerialized]
        public ObservableCollection<string> factorNames;
        public int year;
        public List<double> ratings;
        public double AverageRating, ShowIndex, PredictedOdds;
        public double OldRating, OldOdds, FinalPrediction;
        public string RenewalStatus;
        public bool Renewed, Canceled;

        private int _episodes;
        public int Episodes
        {
            get { return _episodes; }
            set
            {
                _episodes = value;
            }
        }

        private bool _halfhour;
        public bool Halfhour
        {
            get { return _halfhour; }
            set
            {
                _halfhour = value;
            }
        }

        public int FactorHash
        {
            get
            {
                int hash = 0;
                hash += Episodes;
                hash += Halfhour ? 32 : 0;
                int level = 64;
                foreach (bool b in factorValues)
                {
                    hash += b ? level : 0;
                    level *= 2;
                }

                return hash;
            }
        }

        private int _season;
        public int Season
        {
            get { return _season; }
            set
            {
                _season = value;
            }
        }

        public int CompareTo(Show other)
        {
            return AverageRating.CompareTo(other.AverageRating);
        }
    }

    [Serializable]
    public class NeuralPredictionModel : IComparable<NeuralPredictionModel>
    {
        [NonSerialized]
        public List<Show> shows;

        int NeuronCount, InputCount;
        Neuron[] FirstLayer, SecondLayer;
        Neuron Output;

        public double mutationrate, mutationintensity, neuralintensity;

        [NonSerialized]
        public double _accuracy, _ratingstheshold, _score;

        [NonSerialized]
        public bool isMutated;

        public NeuralPredictionModel(MiniNetwork n) //New Random Prediction Model
        {
            shows = n.shows;
            isMutated = false;

            InputCount = n.factors.Count + 2;
            NeuronCount = Convert.ToInt32(Math.Round(InputCount * 2.0 / 3.0 + 1, 0));

            FirstLayer = new Neuron[NeuronCount];
            SecondLayer = new Neuron[NeuronCount];

            for (int i = 0; i < NeuronCount; i++)
            {
                FirstLayer[i] = new Neuron(InputCount);
                SecondLayer[i] = new Neuron(NeuronCount);
            }

            Output = new Neuron(NeuronCount);

            Random r = new Random();
            mutationrate = r.NextDouble();
            mutationintensity = r.NextDouble();
            neuralintensity = r.NextDouble();
        }

        public double GetThreshold(Show s, double[] averages, double adjustment)
        {
            if (s.year < NetworkDatabase.MaxYear) adjustment = 1;

            if (averages is null) averages = new double[InputCount + 1];

            var inputs = new double[InputCount + 1];

            double[]
                FirstLayerOutputs = new double[NeuronCount],
                SecondLayerOutputs = new double[NeuronCount];

            for (int i = 0; i < InputCount - 2; i++)
                inputs[i] = (s.factorValues[i] ? 1 : -1) - averages[i];

            inputs[InputCount - 2] = (s.Episodes / 26.0 * 2 - 1) - averages[InputCount - 2];
            inputs[InputCount - 1] = (s.Halfhour ? 1 : -1) - averages[InputCount - 1];
            inputs[InputCount] = (s.Season - averages[InputCount]) / s.network.SeasonDeviation;

            for (int i = 0; i < NeuronCount; i++)
                FirstLayerOutputs[i] = FirstLayer[i].GetOutput(inputs);

            for (int i = 0; i < NeuronCount; i++)
                SecondLayerOutputs[i] = SecondLayer[i].GetOutput(FirstLayerOutputs);

            s._calculatedThreshold = Math.Pow((Output.GetOutput(SecondLayerOutputs, true) + 1) / 2, adjustment);

            return s._calculatedThreshold;
        }

        public double GetAverageThreshold(bool parallel = false)
        {
            double total = 0;
            double count = 0;
            int year = NetworkDatabase.MaxYear;

            var tempList = shows.ToList();

            if (parallel)
            {
                double[]
                    totals = new double[tempList.Count],
                    counts = new double[tempList.Count];

                Parallel.For(0, tempList.Count, i =>
                {
                    var s = tempList[i];
                    double weight = 1.0 / (year - s.year + 1);
                    totals[i] = GetThreshold(s, s.network.FactorAverages, 1) * weight;
                    counts[i] = weight;
                });

                total = totals.Sum();
                count = counts.Sum();
            }
            else
                foreach (Show s in tempList)
                {
                    double weight = 1.0 / (year - s.year + 1);
                    total += GetThreshold(s, s.network.FactorAverages, 1) * weight;
                    count += weight;
                }

            return total / count;
        }

        private double GetAdjustment(double NetworkAverage, double SeasonAverage)
        {
            return Math.Log(NetworkAverage) / Math.Log(SeasonAverage);
        }

        public Dictionary<int, double> GetAdjustments(bool parallel = false)
        {
            double average = GetAverageThreshold(parallel);
            var Adjustments = new Dictionary<int, double>();
            var years = shows.Select(x => x.year).ToList().Distinct();
            foreach (int y in years)
                Adjustments[y] = GetAdjustment(average, GetSeasonAverageThreshold(y));

            return Adjustments;
        }

        public double GetSeasonAverageThreshold(int year)
        {
            double total = 0;

            year = CheckYear(year);

            var tempList = shows.Where(x => x.year == year && x.ratings.Count > 0).ToList();
            var count = tempList.Count;
            var totals = new double[count];            

            Parallel.For(0, tempList.Count, i => totals[i] = GetThreshold(tempList[i], tempList[i].network.FactorAverages, 1));

            total = totals.Sum();

            return total / count;
        }

        private int CheckYear(int year)
        {
            var YearList = shows.Where(x => x.ratings.Count > 0).Select(x => x.year).Distinct().ToList();
            YearList.Sort();

            if (!YearList.Contains(year))
            {
                if (YearList.Contains(year - 1))
                    year--;
                else if (YearList.Contains(year + 1))
                    year++;
                else if (YearList.Where(x => x < year).Count() > 0)
                    year = YearList.Where(x => x < year).Last();
                else
                    year = YearList.Where(x => x > year).First();
            }

            return year;
        }

        public double GetOdds(Show s, double[] averages, double adjustment, bool raw = false, bool modified = false, int index = -1, int index2 = -1, int index3 = -1)
        {
            var threshold = modified ? GetModifiedThreshold(s, averages, adjustment, index, index2, index3) : GetThreshold(s, averages, adjustment);

            var target = GetTargetRating(s.year, threshold);
            var variance = Math.Log(s.AverageRating) - Math.Log(target);
            double deviation;

            //calculate standard deviation
            if (s.ratings.Count > 1)
            {
                var count = s.ratings.Count - 1;
                double ProjectionVariance = 0;
                for (int i = 0; i < count; i++)
                {
                    ProjectionVariance += Math.Pow(Math.Log(s.ratingsAverages[i] * s.network.AdjustAverage(i + 1, s.Episodes)) - Math.Log(s.AverageRating * s.network.AdjustAverage(count + 1, s.Episodes)), 2);
                }

                deviation = s.network.deviations[s.ratings.Count - 1][s.Episodes - 1] * Math.Sqrt(ProjectionVariance / count) / s.network.typicalDeviation[s.ratings.Count - 1];

            }
            else
            {
                deviation = s.network.deviations[0][s.Episodes - 1];
            }

            deviation += s.network.TargetError;

            var zscore = variance / deviation;

            var normal = new Normal();

            var baseOdds = normal.CumulativeDistribution(zscore);

            //var exponent = Math.Log(0.5) / Math.Log(threshold);
            //var baseOdds = Math.Pow(s.ShowIndex, exponent);

            if (raw)
                return baseOdds;

            var accuracy = _accuracy;

            if (baseOdds > 0.5)
            {
                baseOdds -= 0.5;
                baseOdds *= 2;
                return (baseOdds * accuracy) / 2 + 0.5;
            }
            else
            {
                baseOdds *= 2;
                baseOdds = 1 - baseOdds;
                return (1 - (baseOdds * accuracy)) / 2;
            }
        }

        public double GetModifiedThreshold(Show s, double[] averages, double adjustment, int index, int index2 = -1, int index3 = -1)
        {
            if (s.year < NetworkDatabase.MaxYear) adjustment = 1;

            if (averages is null) averages = new double[InputCount + 1];

            var inputs = new double[InputCount + 1];

            double[]
                FirstLayerOutputs = new double[NeuronCount],
                SecondLayerOutputs = new double[NeuronCount];

            if (index > -1)
            {
                for (int i = 0; i < InputCount - 2; i++)
                    inputs[i] = (s.factorValues[i] ? 1 : -1) - averages[i];

                inputs[InputCount - 2] = (s.Episodes / 26.0 * 2 - 1) - averages[InputCount - 2];
                inputs[InputCount - 1] = (s.Halfhour ? 1 : -1) - averages[InputCount - 1];
                inputs[InputCount] = (s.Season - averages[InputCount]) / s.network.SeasonDeviation;

                inputs[index] = 0;  //GetScaledAverage(s, index);
                if (index2 > -1)
                {
                    inputs[index2] = 0; // GetScaledAverage(s, index2);
                    if (index3 > -1) inputs[index3] = 0; // GetScaledAverage(s, index3);
                }
            }


            for (int i = 0; i < NeuronCount; i++)
                FirstLayerOutputs[i] = FirstLayer[i].GetOutput(inputs);

            for (int i = 0; i < NeuronCount; i++)
                SecondLayerOutputs[i] = SecondLayer[i].GetOutput(FirstLayerOutputs);

            s._calculatedThreshold = Math.Pow((Output.GetOutput(SecondLayerOutputs, true) + 1) / 2, adjustment);

            return s._calculatedThreshold;
        }

        public double TestAccuracy(bool parallel = false)
        {
            double average = GetAverageThreshold(parallel);

            double weightAverage = Math.Max(average, 1 - average);

            double scores = 0;
            double totals = 0;
            double weights = 0;
            int year = NetworkDatabase.MaxYear;


            var Adjustments = GetAdjustments(parallel);
            var averages = shows.First().network.FactorAverages;

            var tempList = shows.Where(x => x.Renewed || x.Canceled).ToList();

            if (parallel)
            {
                double[]
                    t = new double[tempList.Count],
                    w = new double[tempList.Count],
                    score = new double[tempList.Count];

                Parallel.For(0, tempList.Count, i =>
                {
                    Show s = tempList[i];
                    double threshold = GetThreshold(s, averages, Adjustments[s.year]);
                    int prediction = (s.ShowIndex > threshold) ? 1 : 0;
                    double distance = Math.Abs(s.ShowIndex - threshold);

                    if (s.Renewed)
                    {
                        int accuracy = (prediction == 1) ? 1 : 0;
                        double weight;

                        if (accuracy == 1)
                            weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
                        else
                            weight = (distance + weightAverage) / weightAverage;

                        weight /= year - s.year + 1;

                        if (s.Canceled)
                        {
                            double odds = GetOdds(s, averages, Adjustments[s.year], true);
                            var tempScore = (1 - Math.Abs(odds - 0.55)) * 4 / 3;

                            score[i] = tempScore;

                            if (odds < 0.6 && odds > 0.4)
                            {
                                accuracy = 1;

                                weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;

                                weight *= tempScore;

                                if (prediction == 0)
                                    weight /= 2;
                            }
                            else
                                weight /= 2;
                        }

                        t[i] = accuracy * weight;
                        w[i] = weight;
                    }
                    else if (s.Canceled)
                    {
                        int accuracy = (prediction == 0) ? 1 : 0;
                        double weight;

                        if (accuracy == 1)
                            weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
                        else
                            weight = (distance + weightAverage) / weightAverage;

                        weight /= year - s.year + 1;

                        t[i] = accuracy * weight;
                        w[i] = weight;
                    }
                });

                scores = score.Sum();
                totals = t.Sum();
                weights = w.Sum();
            }
            else
            {
                foreach (Show s in tempList)
                {
                    double threshold = GetThreshold(s, averages, Adjustments[s.year]);
                    int prediction = (s.ShowIndex > threshold) ? 1 : 0;
                    double distance = Math.Abs(s.ShowIndex - threshold);

                    if (s.Renewed)
                    {
                        int accuracy = (prediction == 1) ? 1 : 0;
                        double weight;

                        if (accuracy == 1)
                            weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
                        else
                            weight = (distance + weightAverage) / weightAverage;

                        weight /= year - s.year + 1;

                        if (s.Canceled)
                        {
                            double odds = GetOdds(s, averages, Adjustments[s.year], true);
                            scores += (1 - Math.Abs(odds - 0.55)) * 4 / 3;

                            if (odds < 0.6 && odds > 0.4)
                            {
                                accuracy = 1;
                                weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
                                weight *= (1 - Math.Abs(odds - 0.55)) * 4 / 3;

                                if (prediction == 0)
                                    weight /= 2;
                            }
                            else
                                weight /= 2;

                        }

                        totals += accuracy * weight;
                        weights += weight;
                    }
                    else if (s.Canceled)
                    {
                        int accuracy = (prediction == 0) ? 1 : 0;
                        double weight;

                        if (accuracy == 1)
                            weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
                        else
                            weight = (distance + weightAverage) / weightAverage;

                        weight /= year - s.year + 1;

                        totals += accuracy * weight;
                        weights += weight;
                    }
                }
            }

            _accuracy = (weights == 0) ? 0.0 : (totals / weights);
            _score = scores;

            return _accuracy;
        }

        public double GetNetworkRatingsThreshold(int year)
        {
            var s = shows.First();

            year = CheckYear(year);

            var Adjustment = GetAdjustments(true)[year];
            _ratingstheshold = GetTargetRating(year, GetModifiedThreshold(s, s.network.FactorAverages, Adjustment, -1));
            return _ratingstheshold;
        }

        public double GetTargetRating(int year, double targetindex)
        {

            var tempShows = shows.Where(x => x.year == year && x.ratings.Count > 0).OrderByDescending(x => x.ShowIndex).ToList();
            if (tempShows.Count == 0)
            {
                //var yearList = shows.Where(x => x.ratings.Count > 0).Select(x => x.year).ToList();
                //yearList.Sort();
                //if (yearList.Contains(year - 1))
                //    year--;
                //else if (yearList.Contains(year + 1))
                //    year++;
                //else if (yearList.Where(x => x < year).Count() > 0)
                //    year = yearList.Where(x => x < year).Last();
                //else
                //    year = yearList.Where(x => x > year).First();

                //year = yearList.Last();

                year = CheckYear(year);
                tempShows = shows.Where(x => x.year == year && x.ratings.Count > 0).OrderByDescending(x => x.ShowIndex).ToList();
            }

            bool found = false;
            int upper = 0, lower = 1;
            for (int i = 0; i < tempShows.Count && !found; i++)
            {
                if (tempShows[i].ShowIndex < targetindex)
                {
                    lower = i;
                    found = true;
                }
                else
                    upper = i;
            }

            if (tempShows.Count > 0)
            {
                double maxIndex, minIndex, maxRating, minRating;
                if (lower != 0 && lower > upper && tempShows.Count > 1) //match is between two values
                {
                    maxIndex = tempShows[upper].ShowIndex;
                    maxRating = tempShows[upper].AverageRating;
                    minIndex = tempShows[lower].ShowIndex;
                    minRating = tempShows[lower].AverageRating;

                    while (maxRating == minRating)
                    {
                        lower++;

                        if (lower < tempShows.Count)
                        {
                            minIndex = tempShows[lower].ShowIndex;
                            minRating = tempShows[lower].AverageRating;
                        }
                        else
                        {
                            minIndex = 0;
                            minRating = 0;
                        }
                    }
                }
                else if (lower == 0 && tempShows.Count > 1) //match is at the beginning of a multiple item list
                {
                    lower = 1;
                    maxIndex = tempShows[0].ShowIndex;
                    maxRating = tempShows[0].AverageRating;
                    minIndex = tempShows[1].ShowIndex;
                    minRating = tempShows[1].AverageRating;

                    while (maxRating == minRating)
                    {
                        lower++;

                        if (lower < tempShows.Count)
                        {
                            minIndex = tempShows[lower].ShowIndex;
                            minRating = tempShows[lower].AverageRating;
                        }
                        else
                        {
                            minIndex = 0;
                            minRating = 0;
                        }
                    }
                }
                else if (upper > 0) //match is at the end of a multiple item list
                {
                    lower = upper - 1;

                    maxIndex = tempShows[upper].ShowIndex;
                    maxRating = tempShows[upper].AverageRating;
                    minIndex = tempShows[upper - 1].ShowIndex;
                    minRating = tempShows[upper - 1].AverageRating;

                    while (maxRating == minRating)
                    {
                        lower--;

                        if (lower >= 0)
                        {
                            minIndex = tempShows[lower].ShowIndex;
                            minRating = tempShows[lower].AverageRating;
                        }
                        else
                        {
                            minIndex = 0;
                            minRating = 0;
                        }
                    }
                }
                else //one item in list
                {
                    maxIndex = tempShows[upper].ShowIndex;
                    maxRating = tempShows[upper].AverageRating;
                    minIndex = 0;
                    minRating = 0;
                }


                return (targetindex - minIndex) / (maxIndex - minIndex) * (maxRating - minRating) + minRating;
            }

            return 0;
        }


        public int CompareTo(NeuralPredictionModel other)
        {
            double otherAcc = other._accuracy;
            double thisAcc = _accuracy;
            double thisWeight = _score;
            double otherWeight = other._score;

            if (thisAcc != otherAcc)
                return otherAcc.CompareTo(thisAcc);
            else
                return otherWeight.CompareTo(thisWeight);
        }

        public override bool Equals(object obj)
        {
            var other = (NeuralPredictionModel)obj;

            if (other._accuracy == _accuracy)
            {
                if (other._score == _score)
                    return true;
                else
                    return false;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(NeuralPredictionModel x, NeuralPredictionModel y)
        {
            if (x._accuracy == y._accuracy)
            {
                if (x._score == y._score)
                    return true;
                else
                    return false;
            }

            return false;
        }

        public static bool operator !=(NeuralPredictionModel x, NeuralPredictionModel y)
        {
            if (x._accuracy == y._accuracy)
            {
                if (x._score == y._score)
                    return false;
                else
                    return true;
            }

            return true;
        }

        public static bool operator >(NeuralPredictionModel x, NeuralPredictionModel y)
        {
            if (x._accuracy > y._accuracy)
                return true;
            else
            {
                if (x._accuracy == y._accuracy)
                {
                    if (x._score > y._score)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        public static bool operator <(NeuralPredictionModel x, NeuralPredictionModel y)
        {
            if (x._accuracy < y._accuracy)
                return true;
            else
            {
                if (x._accuracy == y._accuracy)
                {
                    if (x._score < y._score)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }
    }

    [Serializable]
    public class Neuron
    {
        double bias, outputbias;
        double[] weights;
        int inputSize;
        public bool isMutated;

        public Neuron(int inputs)
        {
            isMutated = false;

            Random r = new Random();
            bias = r.NextDouble() * 2 - 1;
            outputbias = 0;

            weights = new double[inputs];

            Parallel.For(0, inputs, i => weights[i] = r.NextDouble() * 2 - 1);

            inputSize = inputs;
        }

        public double GetOutput(double[] inputs, bool output = false)
        {
            double total = 0;

            for (int i = 0; i < inputSize; i++)
                total += inputs[i] * weights[i];

            total += bias;

            return output ? Activation(total) : Activation(total) + outputbias;
        }

        double Activation(double d)
        {
            return (2 / (1 + Math.Exp(-1 * d))) - 1;
        }
    }

    [Serializable]
    class BackupData
    {
        public Dictionary<int, string> ShowDescriptions, IMDBList, ShowImages;
        public Dictionary<string, int> ShowIDs;
    }

    public static class NetworkDatabase
    {
        public static ObservableCollection<MiniNetwork> NetworkList;
        public static int MaxYear;
        public static string Folder = "Not Loaded";
        public static List<Year> YearList = new List<Year>();
        public static string currentText = "";
        public static TMDbClient Client;
        //public static ITvDbClient client = new TvDbClient();
        public static bool backup = false, TMDBerror = false;
        public static MainPage mainpage;
        public static bool IsLoaded = false;
        public static bool CurrentlyLoading = false;
        public static bool InBackground = false;

        public static Dictionary<int, string>
            ShowDescriptions = new Dictionary<int, string>(),
            IMDBList = new Dictionary<int, string>(),
            //ShowSlugs = new Dictionary<int, string>(),
            ShowImages = new Dictionary<int, string>();

        public static Dictionary<string, int> ShowIDs = new Dictionary<string, int>();
        //public static DateTime ApiTime;

        static int _currentyear;
        public static int CurrentYear
        {
            get
            {
                return _currentyear;
            }
            set
            {
                if (_currentyear != value && value > -1)
                {
                    _currentyear = value;
                    foreach (MiniNetwork n in NetworkList)
                        n.pendingFilter = true;

                    CurrentYearUpdated?.Invoke(_currentyear, new EventArgs());
                }
            }
        }

        public static bool HomeButton
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("HomeButton"))
                    return (bool)Application.Current.Properties["HomeButton"];
                else
                    return true;
            }
            set
            {
                Application.Current.Properties["HomeButton"] = value;
                HomeButtonChanged(new object(), new EventArgs());
            }
        }

        public static event EventHandler HomeButtonChanged;

        public static bool UseOdds = false;

        public static event EventHandler CurrentYearUpdated;
        public static async void ReadSettings(MainPage page)
        {
            var predictions = "https://github.com/morphinapg/TVPredictions/raw/master/Predictions.TVP";                

            Directory.CreateDirectory(Folder);

            if (CrossConnectivity.Current.IsConnected && await CrossConnectivity.Current.IsRemoteReachable("https://github.com/"))
            {
                try
                {
                    var webClient = new WebClient();
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(page.CompletedSettings);
                    string pathToNewFile = Path.Combine(Folder, Path.GetFileName(predictions));
                    webClient.DownloadFileAsync(new Uri(predictions), pathToNewFile);
                    webClient.Dispose();
                }
                catch (Exception ex)
                {
                    ex = new Exception("Error while downloading predictions");
                    var e = new AsyncCompletedEventArgs(ex, false, page);

                    page.CompletedSettings(page, e);
                }
            }
            else
            {
                var ex = new Exception("Not Connected to the Internet! Try again when connected.");
                var e = new AsyncCompletedEventArgs(ex, false, page);

                page.CompletedSettings(page, e);
            }            
        }

        public static void SetMainPage(MainPage page)
        {
            mainpage = page;
        }

        public static async Task ReadUpdateAsync()
        {
            if (!NetworkDatabase.CurrentlyLoading)
            {
                NetworkDatabase.CurrentlyLoading = true;

                var update = "https://github.com/morphinapg/TVPredictions/raw/master/Update.txt";

                var FilePath = Path.Combine(Folder, "Update.txt");

                Directory.CreateDirectory(Folder);

                if (File.Exists(FilePath))
                    using (var fs = new FileStream(FilePath, FileMode.Open))
                    {
                        using (var reader = new StreamReader(fs))
                        {
                            currentText = await reader.ReadToEndAsync();
                        }
                    }

                if (CrossConnectivity.Current.IsConnected && await CrossConnectivity.Current.IsRemoteReachable("https://github.com/"))
                {
                    try
                    {
                        var webClient = new WebClient();
                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(mainpage.CompletedUpdateAsync);
                        webClient.DownloadFileAsync(new Uri(update), FilePath);
                        webClient.Dispose();
                    }
                    catch (Exception ex)
                    {
                        ex = new Exception("Error while downloading predictions");
                        var e = new AsyncCompletedEventArgs(ex, false, mainpage);

                        mainpage.CompletedUpdateAsync(mainpage, e);
                    }

                    await AuthenticateTMDB();
                }
                else
                {
                    var ex = new Exception("Not Connected to the Internet! Try again when connected.");

                    var e = new AsyncCompletedEventArgs(ex, false, mainpage);

                    mainpage.CompletedUpdateAsync(mainpage, e);
                }
            }
        }

        public static async Task AuthenticateTMDB()
        {
            //if (Application.Current.Properties.ContainsKey("TMDB"))
            //    Application.Current.Properties.Remove("TMDB");

            if (!Application.Current.Properties.ContainsKey("TMDB"))
            {
                foreach (string ShowName in ShowIDs.Keys)
                {
                    string key = "SHOWID " + ShowName;

                    if (Application.Current.Properties.ContainsKey(key))
                        Application.Current.Properties.Remove(key);
                }

                ShowIDs = new Dictionary<string, int>();
                //ShowSlugs = new Dictionary<int, string>();
                ShowDescriptions = new Dictionary<int, string>();
                ShowImages = new Dictionary<int, string>();
                IMDBList = new Dictionary<int, string>();

                if (File.Exists(Path.Combine(Folder, "backup")))
                    File.Delete(Path.Combine(Folder, "backup"));

                Application.Current.Properties["TMDB"] = "Initialized";
            }

            try
            {
                TMDBerror = false;

                await Task.Run(() => Client = new TMDbClient("cfa9429b40f59417c6b45db8adef0b15"));
                
                //await client.Authentication.AuthenticateAsync("GU08T4COWROF8RQC");
            }
            catch (Exception)
            {
                TMDBerror = true;
            }
            //finally
            //{
            //    ApiTime = DateTime.Now;
            //}   
        }

        //public static async Task RefreshTVDB()
        //{
        //    try
        //    {
        //        TMDBerror = false;
        //        await client.Authentication.RefreshTokenAsync();
        //        ApiTime = DateTime.Now;
        //    }
        //    catch (Exception)
        //    {
        //        TMDBerror = true;
        //    }
        //}
        
        public static async Task<int> GetShowID(string name, string network, bool ForceDownload = false)
        {
            if (ShowIDs.ContainsKey(name))
                return ShowIDs[name];
            else
            {
                try
                {
                    //if (DateTime.Now - ApiTime > TimeSpan.FromHours(24))
                    //    await RefreshTVDB();

                    if (TMDBerror)
                        await AuthenticateTMDB();

                    var key = "SHOWID " + name;

                    if (Application.Current.Properties.ContainsKey(key) && !ForceDownload)
                    {
                        await ReadBackup(0, name);

                        if (ShowIDs.ContainsKey(name))
                        {
                            var id = ShowIDs[name];
                            if (ShowDescriptions.ContainsKey(id) && ShowImages.ContainsKey(id))
                                return id;
                        }
                    }

                    var result = await GetSearchResults(name); 

                    var data = result.Where(x => !String.IsNullOrEmpty(x.SeriesName)).ToList();
                    var networkdata = data.Where(x => x.Networks.Count > 0).ToList();

                    var showResults = new List<TVSearchResult>();
                    

                    if (Application.Current.Properties.ContainsKey(key))
                        showResults = data.Where(x => x.Id == (int)Application.Current.Properties[key]).ToList();

                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => (x.SeriesName == name || x.SeriesName.Contains(name + " (")) && x.Networks.Contains(network)).ToList();

                    if (showResults.Count == 0)
                        showResults = data.Where(x => x.SeriesName == name || x.SeriesName.Contains(name + " (")).ToList();
                    
                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => (x.SeriesName == name || x.SeriesName.Contains(name + " (") || x.Aliases.Contains(name)) && x.Networks.Contains(network)).ToList();
                        
                    if (showResults.Count == 0)
                        showResults = data.Where(x => x.SeriesName == name || x.SeriesName.Contains(name + " (") || x.Aliases.Contains(name)).ToList();                        

                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => (x.SeriesName.Contains(name) || x.Aliases.Contains(name)) && x.Networks.Contains(network)).ToList();                        

                    if (showResults.Count == 0)
                        showResults = data.Where(x => x.SeriesName.Contains(name) || x.Aliases.Contains(name)).ToList();

                    if (showResults.Count == 0)
                        showResults = networkdata.Where(x => x.Networks.Contains(network)).ToList();
                        
                    if (showResults.Count == 0)
                        showResults = data.ToList();

                    //foreach (TVSearchResult s in showResults)
                    //{
                    //    try
                    //    {
                    //        if (s.FirstAired == "")
                    //        {
                    //            var episodes = await client.Series.GetEpisodesAsync(s.Id, 1);
                    //            s.FirstAired = episodes.Data.First().FirstAired;
                    //        }
                    //    }
                    //    catch (Exception)
                    //    {
                    //        //do nothing
                    //    }
                    //}

                    showResults = showResults.OrderByDescending(x => x.Show.FirstAirDate).ToList();

                    int ID = showResults.First().Id;
                    
                    ShowDescriptions[ID] = showResults.First().Show.Overview;
                    //ShowSlugs[ID] = showResults.First().Slug;

                    //TvDbSharper.Dto.TvDbResponse<TvDbSharper.Dto.Image[]> imgs;

                    //try
                    //{
                    //    imgs = await client.Series.GetImagesAsync(ID, new TvDbSharper.Dto.ImagesQuery() { KeyType = TvDbSharper.Dto.KeyType.Series });
                    //}
                    //catch (Exception)
                    //{
                    //    imgs = new TvDbSharper.Dto.TvDbResponse<TvDbSharper.Dto.Image[]>();
                    //}

                    //if (imgs.Data is null)
                    //    ShowImages[ID] = null;
                    //else
                    //    ShowImages[ID] = imgs.Data.First().FileName;

                    ShowImages[ID] = showResults.First().Show.BackdropPath;

                    IMDBList[ID] = "";

                    Application.Current.Properties[key] = ID;
                    ShowIDs[name] = ID;

                    backup = true;
                    return ID;
                }
                catch (Exception)
                {
                    TMDBerror = true;
                    if (!ShowIDs.ContainsKey(name))
                        await ReadBackup(0, name);

                    if (ShowIDs.ContainsKey(name))
                        return ShowIDs[name];

                    return 0;
                }

            }
        }

        //public static async Task<TvDbSharper.Dto.TvDbResponse<TvDbSharper.Dto.SeriesSearchResult[]>> GetSearchResults(string name)
        //{
        //    name = Regex.Replace(name, @"[^\u0000-\u007F]+", " ");
        //    return await client.Search.SearchSeriesByNameAsync(name);
        //}

        public static async Task<List<TVSearchResult>> GetSearchResults(string name)
        {
            name = Regex.Replace(name, @"[^\u0000-\u007F]+", " ");
            var result = await Client.SearchTvShowAsync(name);

            var DetailedResults = new List<TVSearchResult>();
            foreach (TMDbLib.Objects.Search.SearchTv show in result.Results)
            {
                var ShowDetails = await Client.GetTvShowAsync(show.Id, TMDbLib.Objects.TvShows.TvShowMethods.AlternativeTitles);

                DetailedResults.Add(new TVSearchResult(ShowDetails.Name, ShowDetails.Id, ShowDetails.Networks.Select(x => x.Name).ToList(), ShowDetails.AlternativeTitles.Results.Select(x => x.Title).ToList(), ShowDetails));
            }

            return DetailedResults;
        }

        public static async Task<Uri> GetImageURI(int ID)
        {
            if (!ShowImages.ContainsKey(ID))
                await ReadBackup(ID, "");

            try
            {
                return new Uri("https://www.themoviedb.org/t/p/original/" + ShowImages[ID]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<Uri> GetIMDBuri(string name)
        {
            if (!ShowIDs.ContainsKey(name))
                await ReadBackup(0, name);

            if (!ShowIDs.ContainsKey(name))
            {
                name = name.Replace(" ", "+");
                return new Uri("https://www.imdb.com/find?s=all&q=" + name);
            }
                

            var ID = ShowIDs[name];

            if (string.IsNullOrWhiteSpace(IMDBList[ID]) && CrossConnectivity.Current.IsConnected && await CrossConnectivity.Current.IsRemoteReachable("https://www.themoviedb.org/"))
            {
                try
                {
                    //if (DateTime.Now - ApiTime > TimeSpan.FromHours(24))
                    //    await RefreshTVDB();

                    if (TMDBerror)
                        await AuthenticateTMDB();

                    var result = await Client.GetTvShowExternalIdsAsync(ID);
                    IMDBList[ID] = result.ImdbId;
                }
                catch (Exception)
                {
                    TMDBerror = true;
                    name = name.Replace(" ", "+");
                    return new Uri("https://www.imdb.com/find?s=all&q=" + name);
                }                
            }

            return new Uri(Path.Combine("https://www.imdb.com/title/", IMDBList[ID]));
        }

        public static Uri GetTMDBuri(string name)
        {
            if (!ShowIDs.ContainsKey(name))
            {
                name = name.Replace(" ", "+");
                return new Uri("https://www.themoviedb.org/search?query=" + name);
            }

            var index = ShowIDs[name];

            return new Uri(Path.Combine("https://www.themoviedb.org/tv/", index.ToString()));
        }

        public static async Task SaveBackup()
        {
            await Task.Run(() =>
            {
                var NewBackup = new BackupData
                {
                    ShowIDs = new Dictionary<string, int>(),
                    ShowDescriptions = new Dictionary<int, string>(),
                    IMDBList = new Dictionary<int, string>(),
                    //ShowSlugs = new Dictionary<int, string>(),
                    ShowImages = new Dictionary<int, string>()
                };

                ShowIDs.Where(x => ShowDescriptions.ContainsKey(x.Value) && IMDBList.ContainsKey(x.Value) && ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                {
                    NewBackup.ShowIDs[x.Key] = x.Value;
                    NewBackup.ShowDescriptions[x.Value] = ShowDescriptions[x.Value];
                    NewBackup.IMDBList[x.Value] = IMDBList[x.Value];
                    //NewBackup.ShowSlugs[x.Value] = ShowSlugs[x.Value];
                    NewBackup.ShowImages[x.Value] = ShowImages[x.Value];
                });

                if (File.Exists(Path.Combine(Folder, "backup")))
                {
                    try
                    {
                        var serializer = new DataContractSerializer(typeof(BackupData));
                        BackupData OldBackup;

                        using (var fs = new FileStream(Path.Combine(Folder, "backup"), FileMode.Open))
                        {
                            OldBackup = serializer.ReadObject(fs) as BackupData;
                        }

                        OldBackup.ShowIDs.Where(x => !NewBackup.ShowIDs.ContainsKey(x.Key) && OldBackup.ShowDescriptions.ContainsKey(x.Value) && OldBackup.IMDBList.ContainsKey(x.Value) && OldBackup.ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                        {
                            NewBackup.ShowIDs[x.Key] = x.Value;
                            NewBackup.ShowDescriptions[x.Value] = OldBackup.ShowDescriptions[x.Value];
                            NewBackup.IMDBList[x.Value] = OldBackup.IMDBList[x.Value];
                            //NewBackup.ShowSlugs[x.Value] = OldBackup.ShowSlugs[x.Value];
                            NewBackup.ShowImages[x.Value] = OldBackup.ShowImages[x.Value];
                        });
                    }
                    catch (Exception)
                    {
                        //Error reading backup, cancel
                    }
                }

                //if (Application.Current.Properties.ContainsKey("Backup"))
                //{
                //    var OldBackup = Application.Current.Properties["Backup"] as BackupData;

                //    OldBackup.ShowIDs.Where(x => !NewBackup.ShowIDs.ContainsKey(x.Key) && OldBackup.ShowDescriptions.ContainsKey(x.Value) && OldBackup.IMDBList.ContainsKey(x.Value) && OldBackup.ShowSlugs.ContainsKey(x.Value) && OldBackup.ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                //    {
                //        NewBackup.ShowIDs[x.Key] = x.Value;
                //        NewBackup.ShowDescriptions[x.Value] = OldBackup.ShowDescriptions[x.Value];
                //        NewBackup.IMDBList[x.Value] = OldBackup.IMDBList[x.Value];
                //        NewBackup.ShowSlugs[x.Value] = OldBackup.ShowSlugs[x.Value];
                //        NewBackup.ShowImages[x.Value] = OldBackup.ShowImages[x.Value];
                //    });
                //}

                //Application.Current.Properties["Backup"] = NewBackup;

                try
                {
                    using (var fs = new FileStream(Path.Combine(Folder, "backup"), FileMode.Create))
                    {
                        var serializer = new DataContractSerializer(typeof(BackupData));
                        serializer.WriteObject(fs, NewBackup);
                    }
                }
                catch (Exception)
                {
                    //File is in use, do not back up
                }
            });

            backup = false;
        }

        public static async Task ReadBackup(int ID, string name)
        {
            await Task.Run(() =>
            {
                if (File.Exists(Path.Combine(Folder, "backup")))
                {
                    try
                    {
                        var serializer = new DataContractSerializer(typeof(BackupData));
                        BackupData OldBackup;

                        using (var fs = new FileStream(Path.Combine(Folder, "backup"), FileMode.Open))
                        {
                            OldBackup = serializer.ReadObject(fs) as BackupData;
                        }

                        //Append any missing OldBackup data
                        OldBackup.ShowIDs.Where(x => (x.Key == name || x.Value == ID) && !ShowIDs.ContainsKey(x.Key) && OldBackup.ShowDescriptions.ContainsKey(x.Value) && OldBackup.IMDBList.ContainsKey(x.Value) && OldBackup.ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                        {
                            ShowIDs[x.Key] = x.Value;
                            ShowDescriptions[x.Value] = OldBackup.ShowDescriptions[x.Value];
                            IMDBList[x.Value] = OldBackup.IMDBList[x.Value];
                            //ShowSlugs[x.Value] = OldBackup.ShowSlugs[x.Value];
                            ShowImages[x.Value] = OldBackup.ShowImages[x.Value];
                        });
                    }
                    catch (Exception)
                    {
                        //backup is corrupted, do not read
                    }
                }

                //if (Application.Current.Properties.ContainsKey("Backup"))
                //{
                //    var OldBackup = Application.Current.Properties["Backup"] as BackupData;

                //    //Append any missing OldBackup data
                //    OldBackup.ShowIDs.Where(x => (x.Key == name || x.Value == ID) && !ShowIDs.ContainsKey(x.Key) && OldBackup.ShowDescriptions.ContainsKey(x.Value) && OldBackup.IMDBList.ContainsKey(x.Value) && OldBackup.ShowImages.ContainsKey(x.Value)).ToList().ForEach(x =>
                //    {
                //        ShowIDs[x.Key] = x.Value;
                //        ShowDescriptions[x.Value] = OldBackup.ShowDescriptions[x.Value];
                //        IMDBList[x.Value] = OldBackup.IMDBList[x.Value];
                //        ShowSlugs[x.Value] = OldBackup.ShowSlugs[x.Value];
                //        ShowImages[x.Value] = OldBackup.ShowImages[x.Value];
                //    });
                //}
            });
        }
    }

    public class Year : IComparable<Year>
    {
        public int year;
        public string Season
        {
            get
            {
                return year + " - " + (year + 1);
            }
        }

        public Year(int y)
        {
            year = y;
        }

        public static implicit operator Year(int value)
        {
            return new Year(value);
        }

        public static implicit operator int(Year value)
        {
            return value.year;
        }

        public bool Equals(Year other)
        {
            return year == other.year;
        }

        public bool Equals(int other)
        {
            return year == other;
        }

        public int CompareTo(Year other)
        {
            return year.CompareTo(other.year);
        }

        public int CompareTo(int other)
        {
            return year.CompareTo(other);
        }
    }

    public class StatusColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int number;

            if (value is double doubleVal)
            {
                if (value == null || doubleVal == 0)
                    number = 0;
                else if (doubleVal > 0)
                    number = 1;
                else
                    number = -1;
            }
            else
                number = (int)value;


            if (number > 0)
                return Color.Green; //new SolidColorBrush(Color.FromArgb(255, 0, 176, 80));
            else if (number < 0)
                return Color.Red;// new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            else
                return Color.Gray; //new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NumberColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Color.Transparent;
            else if ((double)value > 0)
                return Color.MediumSeaGreen;
            else if ((double)value < 0)
                return Color.IndianRed;
            else
                return Color.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusColorAlt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int number;

            if (value is double doubleVal)
            {
                if (value == null || doubleVal == 0)
                    number = 0;
                else if (doubleVal > 0)
                    number = 1;
                else
                    number = -1;
            }
            else
                number = (int)value;


            if (number > 0)
                return Color.Green; //new SolidColorBrush(Color.FromArgb(255, 0, 176, 80));
            else if (number < 0)
                return Color.Red;// new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            else
                return Color.Gray; //new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }    
}
