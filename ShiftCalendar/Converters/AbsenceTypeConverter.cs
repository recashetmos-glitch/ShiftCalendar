using ShiftCalendar.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ShiftCalendar.Converters
{
    public class AbsenceTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AbsenceType status)
            {
                return status switch
                {
                    AbsenceType.НаСмене => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    AbsenceType.Отпуск => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    AbsenceType.Больничный => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    AbsenceType.Отгул => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    _ => new SolidColorBrush(Color.FromRgb(136, 136, 136))
                };
            }
            return new SolidColorBrush(Color.FromRgb(136, 136, 136));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}