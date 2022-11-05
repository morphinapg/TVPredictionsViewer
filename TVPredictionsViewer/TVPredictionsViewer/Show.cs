using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace TV_Ratings_Predictions
{
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

        public string NameWithSeason => network.shows.Where(x => x._name == Name && x.year == year).Count() > 1 ? Name + " (Season " + Season + ")" : Name;


        public ObservableCollection<bool> factorValues;

        [NonSerialized]
        public ObservableCollection<string> factorNames;
        public int year, PreviousEpisodes;
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
}
