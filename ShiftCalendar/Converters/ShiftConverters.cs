using ShiftCalendar.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShiftCalendar.Converters
{
    public class ShiftToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isWorkDay)
            {
                return isWorkDay
                    ? new SolidColorBrush(Color.FromRgb(212, 165, 116))
                    : new SolidColorBrush(Color.FromRgb(42, 42, 42));
            }
            return new SolidColorBrush(Color.FromRgb(42, 42, 42));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShiftToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isWorkDay)
            {
                return isWorkDay
                    ? Brushes.Black
                    : new SolidColorBrush(Color.FromRgb(136, 136, 136));
            }
            return new SolidColorBrush(Color.FromRgb(136, 136, 136));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ShiftTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ShiftType shift)
            {
                return shift switch
                {
                    ShiftType.День => new SolidColorBrush(Color.FromRgb(212, 165, 116)),
                    ShiftType.Ночь => new SolidColorBrush(Color.FromRgb(92, 107, 192)),
                    ShiftType.Утро => new SolidColorBrush(Color.FromRgb(102, 187, 106)),
                    ShiftType.Выходной => new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                    _ => new SolidColorBrush(Color.FromRgb(42, 42, 42))
                };
            }
            return new SolidColorBrush(Color.FromRgb(42, 42, 42));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                // Если передан параметр "Invert", инвертируем
                if (parameter is string p && p == "Invert")
                {
                    return !b;
                }
                return !b;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}