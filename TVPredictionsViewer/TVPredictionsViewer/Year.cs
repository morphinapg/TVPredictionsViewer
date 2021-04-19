using System;
using System.Collections.Generic;
using System.Text;

namespace TVPredictionsViewer
{
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
}
