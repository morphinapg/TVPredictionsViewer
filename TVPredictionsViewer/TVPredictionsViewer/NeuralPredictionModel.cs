﻿using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TV_Ratings_Predictions;

namespace TV_Ratings_Predictions
{
    [Serializable]
    public class NeuralPredictionModel
    {
        [NonSerialized]
        public List<Show> shows;

        int NeuronCount, InputCount;
        Neuron[] FirstLayer, SecondLayer;
        Neuron Output;

        public double mutationrate, mutationintensity, neuralintensity, SeasonDeviation, PreviousEpisodeDeviation, YearDeviation;

        [NonSerialized]
        public double _accuracy, _ratingstheshold, _score;

        [NonSerialized]
        public bool isMutated;

        public double[] FactorBias;

        //public NeuralPredictionModel(MiniNetwork n) //New Random Prediction Model
        //{
        //    shows = n.shows;
        //    isMutated = false;

        //    InputCount = n.factors.Count + 5;
        //    NeuronCount = Convert.ToInt32(Math.Round(InputCount * 2.0 / 3.0 + 1, 0));

        //    FirstLayer = new Neuron[NeuronCount];
        //    SecondLayer = new Neuron[NeuronCount];

        //    for (int i = 0; i < NeuronCount; i++)
        //    {
        //        FirstLayer[i] = new Neuron(InputCount);
        //        SecondLayer[i] = new Neuron(NeuronCount);
        //    }

        //    Output = new Neuron(NeuronCount);

        //    Random r = new Random();
        //    mutationrate = r.NextDouble();
        //    mutationintensity = r.NextDouble();
        //    neuralintensity = r.NextDouble();
        //}

        public double GetThreshold(Show s)
        {
            var inputs = GetInputs(s);

            double[]
                FirstLayerOutputs = new double[NeuronCount],
                SecondLayerOutputs = new double[NeuronCount];

            //for (int i = 0; i < InputCount - 2; i++)
            //    inputs[i] = (s.factorValues[i] ? 1 : -1) - averages[i];

            //inputs[InputCount - 2] = (s.Episodes / 26.0 * 2 - 1) - averages[InputCount - 2];
            //inputs[InputCount - 1] = (s.Halfhour ? 1 : -1) - averages[InputCount - 1];
            //inputs[InputCount] = (s.Season - averages[InputCount]) / s.network.SeasonDeviation;

            for (int i = 0; i < NeuronCount; i++)
                FirstLayerOutputs[i] = FirstLayer[i].GetOutput(inputs);

            for (int i = 0; i < NeuronCount; i++)
                SecondLayerOutputs[i] = SecondLayer[i].GetOutput(FirstLayerOutputs);

            s._calculatedThreshold = Math.Min(Math.Max((Output.GetOutput(SecondLayerOutputs, true) + 1) / 2, 0.000001), 0.999999);

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
                    totals[i] = GetThreshold(s) * weight;
                    counts[i] = weight;
                });

                total = totals.Sum();
                count = counts.Sum();
            }
            else
                foreach (Show s in tempList)
                {
                    double weight = 1.0 / (year - s.year + 1);
                    total += GetThreshold(s) * weight;
                    count += weight;
                }

            return total / count;
        }

        //private double GetAdjustment(double NetworkAverage, double SeasonAverage)
        //{
        //    return Math.Log(NetworkAverage) / Math.Log(SeasonAverage);
        //}

        //public Dictionary<int, double> GetAdjustments(bool parallel = false)
        //{
        //    double average = GetAverageThreshold(parallel);
        //    var Adjustments = new Dictionary<int, double>();
        //    var years = shows.Select(x => x.year).ToList().Distinct();
        //    foreach (int y in years)
        //        Adjustments[y] = (y == NetworkDatabase.MaxYear) ? GetAdjustment(average, GetSeasonAverageThreshold(y)) : 1;

        //    return Adjustments;
        //}

        public double GetSeasonAverageThreshold(int year)
        {
            double total = 0;

            year = CheckYear(year);

            var tempList = shows.Where(x => x.year == year && x.ratings.Count > 0).ToList();
            var count = tempList.Count;
            var totals = new double[count];

            Parallel.For(0, tempList.Count, i => totals[i] = GetThreshold(tempList[i]));

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

        //public double GetOdds(Show s, bool raw = false, bool modified = false, int index = -1, int index2 = -1, int index3 = -1)
        //{
        //    var threshold = modified ? GetModifiedThreshold(s, index, index2, index3) : GetThreshold(s);
        //    var OriginalTarget = GetTargetRating(s.year, threshold);

        //    var adjustment = s.network.Adjustment;
        //    if (adjustment == 0)
        //        adjustment = 1;

        //    if (s.year == NetworkDatabase.MaxYear)
        //        threshold = Math.Pow(threshold, adjustment);

        //    var target = GetTargetRating(s.year, threshold);
        //    var variance = Math.Log(s.AverageRating) - Math.Log(target);
        //    double deviation;

        //    //calculate standard deviation
        //    if (s.ratings.Count > 1)
        //    {
        //        var count = s.ratings.Count - 1;
        //        double ProjectionVariance = 0;
        //        for (int i = 0; i < count; i++)
        //        {
        //            ProjectionVariance += Math.Pow(Math.Log(s.ratingsAverages[i] * s.network.AdjustAverage(i + 1, s.Episodes)) - Math.Log(s.AverageRating * s.network.AdjustAverage(count + 1, s.Episodes)), 2);
        //        }

        //        deviation = s.network.deviations[s.ratings.Count - 1][s.Episodes - 1] * Math.Sqrt(ProjectionVariance / count) / s.network.typicalDeviation[s.ratings.Count - 1];

        //    }
        //    else
        //    {
        //        deviation = s.network.deviations[0][s.Episodes - 1];
        //    }

            
        //    double ErrorAdjustment = (s.year == NetworkDatabase.MaxYear) ? Math.Abs(Math.Log(target) - Math.Log(OriginalTarget)) : 0;

        //    //The more overlap there is, the less confidence you can have in the prediction
        //    var Overlap = AreaOfOverlap(Math.Log(s.AverageRating), deviation, Math.Log(target), s.network.TargetError + ErrorAdjustment);

        //    deviation = s.network.TargetError + ErrorAdjustment;


        //    var zscore = variance / deviation;

        //    var normal = new Normal();

        //    var baseOdds = normal.CumulativeDistribution(zscore);

        //    //var exponent = Math.Log(0.5) / Math.Log(threshold);
        //    //var baseOdds = Math.Pow(s.ShowIndex, exponent);

        //    if (raw)
        //        return baseOdds;

        //    //var accuracy = _accuracy;
        //    var accuracy = 1 - Overlap;

        //    if (baseOdds > 0.5)
        //    {
        //        baseOdds -= 0.5;
        //        baseOdds *= 2;
        //        return (baseOdds * accuracy) / 2 + 0.5;
        //    }
        //    else
        //    {
        //        baseOdds *= 2;
        //        baseOdds = 1 - baseOdds;
        //        return (1 - (baseOdds * accuracy)) / 2;
        //    }
        //}

        public double GetModifiedOdds(Show s, double[] ModifiedFactors, bool raw = false)
        {
            var threshold = GetModifiedThreshold(ModifiedFactors);
            var OriginalTarget = GetTargetRating(s.year, threshold);

            var adjustment = s.network.Adjustment;
            if (adjustment == 0)
                adjustment = 1;

            if (s.year == NetworkDatabase.MaxYear)
                threshold = Math.Pow(threshold, adjustment);

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

            double ErrorAdjustment = (s.year == NetworkDatabase.MaxYear) ? Math.Abs(Math.Log(target) - Math.Log(OriginalTarget)) : 0;

            //The more overlap there is, the less confidence you can have in the prediction
            var Overlap = AreaOfOverlap(Math.Log(s.AverageRating), deviation, Math.Log(target), s.network.TargetError + ErrorAdjustment);

            deviation = s.network.TargetError + ErrorAdjustment;

            var zscore = variance / deviation;

            var normal = new Normal();

            var baseOdds = normal.CumulativeDistribution(zscore);

            //var exponent = Math.Log(0.5) / Math.Log(threshold);
            //var baseOdds = Math.Pow(s.ShowIndex, exponent);

            if (raw)
                return baseOdds;

            //var accuracy = _accuracy;
            var accuracy = 1 - Overlap;

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

        public double GetModifiedThreshold(double[] inputs)
        {
            double[]
                FirstLayerOutputs = new double[NeuronCount],
                SecondLayerOutputs = new double[NeuronCount];

            for (int i = 0; i < NeuronCount; i++)
                FirstLayerOutputs[i] = FirstLayer[i].GetOutput(inputs);

            for (int i = 0; i < NeuronCount; i++)
                SecondLayerOutputs[i] = SecondLayer[i].GetOutput(FirstLayerOutputs);

            return Math.Min(Math.Max((Output.GetOutput(SecondLayerOutputs, true) + 1) / 2, 0.000001), 0.999999);
        }

        public double GetModifiedThreshold(Show s, int index, int index2 = -1, int index3 = -1)
        {
            //if (averages is null) averages = new double[InputCount + 1];

            var FactorCount = s.factorNames.Count;

            var averages = FactorBias;

            var inputs = GetBaseInputs();

            double[]
                FirstLayerOutputs = new double[NeuronCount],
                SecondLayerOutputs = new double[NeuronCount];

            if (index > -1)
            {
                inputs = GetInputs(s);

                inputs[index] = 0;  
                if (index2 > -1)
                {
                    if (index2 == FactorCount + 2)
                        inputs[index2] = (inputs[FactorCount + 2] - averages[FactorCount + 2]) / SeasonDeviation;
                    else if (index2 == FactorCount + 3)
                        inputs[index2] = (inputs[FactorCount + 3] - averages[FactorCount + 3]) / PreviousEpisodeDeviation;
                    else if (index2 == FactorCount + 4)
                        inputs[index2] = (inputs[FactorCount + 4] - averages[FactorCount + 4]) / YearDeviation;
                    else
                        inputs[index2] = s.network.RealAverages[index2] - averages[index2];


                    if (index3 > -1)
                    {
                        if (index3 == FactorCount + 2)
                            inputs[index3] = (inputs[FactorCount + 2] - averages[FactorCount + 2]) / SeasonDeviation;
                        else if (index3 == FactorCount + 3)
                            inputs[index3] = (inputs[FactorCount + 3] - averages[FactorCount + 3]) / PreviousEpisodeDeviation;
                        else if (index3 == FactorCount + 4)
                            inputs[index3] = (inputs[FactorCount + 4] - averages[FactorCount + 4]) / YearDeviation;
                        else
                            inputs[index3] = s.network.RealAverages[index3] - averages[index3];
                    }
                }
            }


            for (int i = 0; i < NeuronCount; i++)
                FirstLayerOutputs[i] = FirstLayer[i].GetOutput(inputs);

            for (int i = 0; i < NeuronCount; i++)
                SecondLayerOutputs[i] = SecondLayer[i].GetOutput(FirstLayerOutputs);

            s._calculatedThreshold = Math.Min(Math.Max((Output.GetOutput(SecondLayerOutputs, true) + 1) / 2, 0.000001), 0.999999);

            return s._calculatedThreshold;
        }

        //public double TestAccuracy(bool parallel = false)
        //{
        //    double average = GetAverageThreshold(parallel);

        //    double weightAverage = Math.Max(average, 1 - average);

        //    double scores = 0;
        //    double totals = 0;
        //    double weights = 0;
        //    int year = NetworkDatabase.MaxYear;


        //    //var Adjustments = GetAdjustments(parallel);
        //    var averages = shows.First().network.FactorAverages;

        //    var tempList = shows.Where(x => x.Renewed || x.Canceled).ToList();

        //    if (parallel)
        //    {
        //        double[]
        //            t = new double[tempList.Count],
        //            w = new double[tempList.Count],
        //            score = new double[tempList.Count];

        //        Parallel.For(0, tempList.Count, i =>
        //        {
        //            Show s = tempList[i];
        //            double threshold = GetThreshold(s);
        //            int prediction = (s.ShowIndex > threshold) ? 1 : 0;
        //            double distance = Math.Abs(s.ShowIndex - threshold);

        //            if (s.Renewed)
        //            {
        //                int accuracy = (prediction == 1) ? 1 : 0;
        //                double weight;

        //                if (accuracy == 1)
        //                    weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
        //                else
        //                    weight = (distance + weightAverage) / weightAverage;

        //                weight /= year - s.year + 1;

        //                if (s.Canceled)
        //                {
        //                    double odds = GetOdds(s, true);
        //                    var tempScore = (1 - Math.Abs(odds - 0.55)) * 4 / 3;

        //                    score[i] = tempScore;

        //                    if (odds < 0.6 && odds > 0.4)
        //                    {
        //                        accuracy = 1;

        //                        weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;

        //                        weight *= tempScore;

        //                        if (prediction == 0)
        //                            weight /= 2;
        //                    }
        //                    else
        //                        weight /= 2;
        //                }

        //                t[i] = accuracy * weight;
        //                w[i] = weight;
        //            }
        //            else if (s.Canceled)
        //            {
        //                int accuracy = (prediction == 0) ? 1 : 0;
        //                double weight;

        //                if (accuracy == 1)
        //                    weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
        //                else
        //                    weight = (distance + weightAverage) / weightAverage;

        //                weight /= year - s.year + 1;

        //                t[i] = accuracy * weight;
        //                w[i] = weight;
        //            }
        //        });

        //        scores = score.Sum();
        //        totals = t.Sum();
        //        weights = w.Sum();
        //    }
        //    else
        //    {
        //        foreach (Show s in tempList)
        //        {
        //            double threshold = GetThreshold(s);
        //            int prediction = (s.ShowIndex > threshold) ? 1 : 0;
        //            double distance = Math.Abs(s.ShowIndex - threshold);

        //            if (s.Renewed)
        //            {
        //                int accuracy = (prediction == 1) ? 1 : 0;
        //                double weight;

        //                if (accuracy == 1)
        //                    weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
        //                else
        //                    weight = (distance + weightAverage) / weightAverage;

        //                weight /= year - s.year + 1;

        //                if (s.Canceled)
        //                {
        //                    double odds = GetOdds(s, true);
        //                    scores += (1 - Math.Abs(odds - 0.55)) * 4 / 3;

        //                    if (odds < 0.6 && odds > 0.4)
        //                    {
        //                        accuracy = 1;
        //                        weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
        //                        weight *= (1 - Math.Abs(odds - 0.55)) * 4 / 3;

        //                        if (prediction == 0)
        //                            weight /= 2;
        //                    }
        //                    else
        //                        weight /= 2;

        //                }

        //                totals += accuracy * weight;
        //                weights += weight;
        //            }
        //            else if (s.Canceled)
        //            {
        //                int accuracy = (prediction == 0) ? 1 : 0;
        //                double weight;

        //                if (accuracy == 1)
        //                    weight = 1 - Math.Abs(average - s.ShowIndex) / weightAverage;
        //                else
        //                    weight = (distance + weightAverage) / weightAverage;

        //                weight /= year - s.year + 1;

        //                totals += accuracy * weight;
        //                weights += weight;
        //            }
        //        }
        //    }

        //    _accuracy = (weights == 0) ? 0.0 : (totals / weights);
        //    _score = scores;

        //    return _accuracy;
        //}

        public double GetNetworkRatingsThreshold(int year)
        {
            var s = shows.First();

            year = CheckYear(year);

            var threshold = shows.AsParallel().Where(x => x.year == year).Select(x => GetThreshold(x)).Average();

            var adjustment = s.network.Adjustment;
            if (adjustment == 0)
                adjustment = 1;

            if (year == NetworkDatabase.MaxYear)
                threshold = Math.Pow(threshold, adjustment);

            //var Adjustment = GetAdjustments(true)[year];
            _ratingstheshold = GetTargetRating(year, threshold);
            return _ratingstheshold;
        }

        public double GetTargetRating(int year, double targetindex)
        {

            var tempShows = shows.Where(x => x.year == year && x.ratings.Count > 0).OrderByDescending(x => x.ShowIndex).ToList();
            if (tempShows.Count == 0)
            {
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

        /// <summary>
        /// Finds the percentage of potential ratings that are above the potential renewal thresholds. Use Log values.
        /// </summary>
        /// <param name="Mean1">Projected Rating</param>
        /// <param name="SD1">Standard Deviation for Projected Rating</param>
        /// <param name="Mean2">Renewal Threshold Rating</param>
        /// <param name="SD2">Standard Deviation for Renewal Thresholds</param>
        /// <returns></returns>
        double AreaOfOverlap(double Mean1, double SD1, double Mean2, double SD2)
        {
            //Find intersection points
            double point1, point2;

            var Distribution1 = new Normal(Mean1, SD1);
            var Distribution2 = new Normal(Mean2, SD2);

            if (SD1 * SD2 == 0)
                return 0;

            if (SD1 == SD2)
            {
                point1 = (Mean1 + Mean2) / 2;
                point2 = point1;
            }
            else
            {
                //Find Square of each SD
                double
                    Squared1 = Math.Pow(SD1, 2),
                    Squared2 = Math.Pow(SD2, 2);

                //Then find part that will be positive and negative radical

                var radical = Math.Sqrt(Math.Pow(Mean1 - Mean2, 2) + 2 * (Squared1 - Squared2) * Math.Log(SD1 / SD2));

                point1 = (Mean2 * Squared1 - SD2 * (Mean1 * SD2 + SD1 * radical)) / (Squared1 - Squared2);
                point2 = (Mean2 * Squared1 - SD2 * (Mean1 * SD2 + SD1 * -radical)) / (Squared1 - Squared2);
            }

            if (point1 == point2)
                return (1 - Distribution1.CumulativeDistribution(point1)) * 2;
            else
            {
                var min = Math.Min(point1, point2);
                var max = Math.Max(point1, point2);

                var beginning1 = Distribution1.CumulativeDistribution(min);
                var beginning2 = Distribution2.CumulativeDistribution(min);


                var middle1 = Distribution1.CumulativeDistribution(max) - Distribution1.CumulativeDistribution(min);
                var middle2 = Distribution2.CumulativeDistribution(max) - Distribution2.CumulativeDistribution(min);

                var end1 = 1 - Distribution1.CumulativeDistribution(max);
                var end2 = 1 - Distribution2.CumulativeDistribution(max);

                return Math.Min(beginning1, beginning2) + Math.Min(middle1, middle2) + Math.Min(end1, end2);
            }
        }

        public double[] GetInputs(Show s)
        {
            var averages = FactorBias;
            var FactorCount = s.factorNames.Count;

            var inputs = new double[InputCount];

            for (int i = 0; i < FactorCount; i++)
                inputs[i] = (s.factorValues[i] ? 1 : -1) - averages[i];

            inputs[FactorCount] = (s.Episodes / 26.0 * 2 - 1) - averages[FactorCount];
            inputs[FactorCount + 1] = (s.Halfhour ? 1 : -1) - averages[FactorCount + 1];
            inputs[FactorCount + 2] = (s.Season - averages[FactorCount + 2]) / SeasonDeviation;
            inputs[FactorCount + 3] = (s.PreviousEpisodes - averages[FactorCount + 3]) / PreviousEpisodeDeviation;
            inputs[FactorCount + 4] = (s.year - averages[FactorCount + 4]) / YearDeviation;

            return inputs;
        }


        public double[] GetBaseInputs()
        {
            var inputs = (double[])shows[0].network.RealAverages.Clone();
            var averages = FactorBias;

            var FactorCount = shows[0].factorNames.Count;

            for (int i = 0; i < FactorCount; i++)
                inputs[i] -= averages[i];

            inputs[FactorCount] = (inputs[FactorCount] / 26.0 * 2 - 1) - averages[FactorCount];
            inputs[FactorCount + 1] = inputs[FactorCount + 1] - averages[FactorCount + 1];
            inputs[FactorCount + 2] = (inputs[FactorCount + 2] - averages[FactorCount + 2]) / SeasonDeviation;
            inputs[FactorCount + 3] = (inputs[FactorCount + 3] - averages[FactorCount + 3]) / PreviousEpisodeDeviation;
            inputs[FactorCount + 4] = (inputs[FactorCount + 4] - averages[FactorCount + 4]) / YearDeviation;

            return inputs;
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
}
