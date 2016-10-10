namespace Switch.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class TimeConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            int? msvalue = value as int?;

            if (msvalue.HasValue)
            {
                string result = string.Empty;
                int ms = msvalue.Value;

                int current = ms / 3600000;
                if (current > 0)
                {
                    result += current + Properties.Resources.h;
                    ms %= 3600000;
                }

                current = ms / 60000;
                if (current > 0)
                {
                    if (result.Length > 0)
                    {
                        result += ' ';
                    }

                    result += current + Properties.Resources.m;
                    ms %= 60000;
                }

                current = ms / 1000;
                if (current > 0)
                {
                    if (result.Length > 0)
                    {
                        result += ' ';
                    }

                    result += current + Properties.Resources.s;
                    ms %= 1000;
                }

                current = ms;
                if (current > 0)
                {
                    if (result.Length > 0)
                    {
                        result += ' ';
                    }

                    result += current + Properties.Resources.ms;
                }

                return result;
            }

            return value.ToString();
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            string time = value as string;

            if (time != null)
            {
                int index = 0;
                while (char.IsDigit(time[index]) && index < time.Length)
                {
                    ++index;
                }

                string suffix = time.Substring(index);

                if (suffix == Properties.Resources.s)
                {
                    return System.Convert.ToInt32(time.Substring(0, index)) * 1000;
                }

                if (suffix == Properties.Resources.m)
                {
                    return System.Convert.ToInt32(time.Substring(0, index)) * 60000;
                }

                if (suffix == Properties.Resources.h)
                {
                    return System.Convert.ToInt32(time.Substring(0, index)) * 3600000;
                }
            }

            return null;
        }
    }
}
