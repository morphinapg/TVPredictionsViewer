using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TV_Ratings_Predictions;

namespace TV_Ratings_Predictions
{
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

        public double GetModifiedOdds(Show s, double[] ModifiedFactors, double adjustment, bool raw = false)
        {
            var threshold = GetModifiedThreshold(ModifiedFactors, adjustment);

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

        public double GetModifiedThreshold(double[] inputs, double adjustment)
        {
            double[]
                FirstLayerOutputs = new double[NeuronCount],
                SecondLayerOutputs = new double[NeuronCount];

            for (int i = 0; i < NeuronCount; i++)
                FirstLayerOutputs[i] = FirstLayer[i].GetOutput(inputs);

            for (int i = 0; i < NeuronCount; i++)
                SecondLayerOutputs[i] = SecondLayer[i].GetOutput(FirstLayerOutputs);

            return Math.Pow((Output.GetOutput(SecondLayerOutputs, true) + 1) / 2, adjustment);
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
}
