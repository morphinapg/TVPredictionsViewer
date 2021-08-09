using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace TVPredictionsViewer
{
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

    public class NumberColorAlt : IValueConverter
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
                return Color.Silver;
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
