using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xamarin.Forms;

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

        public void Filter(bool UseFinal = false, bool UseYear = false, List<Year> years = null)
        {
            Predictions.Clear();
            FilteredShows.Clear();
            var year = NetworkDatabase.YearList[NetworkDatabase.CurrentYear];
            var Adjustments = model.GetAdjustments(true);
            var average = model.GetNetworkRatingsThreshold(year);

            if (!UseYear)
                years = new List<Year>() { new Year(NetworkDatabase.YearList[NetworkDatabase.CurrentYear]) };

            shows.Where(x => years.Select(y => y.year).Contains(x.year)).ToList().ForEach(x => FilteredShows.Add(x));

            if (UseYear)
            {
                var p = new ListOfPredictions { Category = "Predictions" };

                foreach (Show s in FilteredShows)
                    p.Add(new PredictionContainer(s, this, Adjustments[year], average, false, UseYear, UseFinal));

                if (Predictions.Count > 0)
                    throw new Exception("Predictions were not empty!");

                Predictions.Add(p);

                pendingFilter = true;
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
}
