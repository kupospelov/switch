namespace Switch.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using static Devices.Device;

    public class FrameTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TransmittedFrame.FrameAction? type = value as TransmittedFrame.FrameAction?;

            if (type != null)
            {
                switch (type.Value)
                {
                    case TransmittedFrame.FrameAction.Forwarded:
                        return Properties.Resources.Forwarded;

                    case TransmittedFrame.FrameAction.Received:
                        return Properties.Resources.Received;

                    case TransmittedFrame.FrameAction.Sent:
                        return Properties.Resources.Sent;
                }
            }

            return Properties.Resources.Error;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
