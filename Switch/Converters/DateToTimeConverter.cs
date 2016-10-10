namespace Switch.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class DateToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? date = value as DateTime?;

            if (date.HasValue)
            {
                return date.Value.ToString("HH:mm:ss");
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
