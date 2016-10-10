namespace Switch.Packets
{
    using System.Windows.Media;

    using Identity;

    public class Packet
    {
        private static int packetCounter = 0;

        public Packet() : this(Brushes.LightBlue, 7, string.Format("{0} {1}", Properties.Resources.Frame, packetCounter))
        {
            packetCounter++;
        }

        public Packet(Brush brush, int radius, string label)
        {
            this.Brush = brush;
            this.Radius = radius;
            this.Label = label;
        }

        public MacAddress Source { get; set; }

        public MacAddress Destination { get; set; }

        public bool Reply { get; set; }

        public Brush Brush { get; set; }

        public int Radius { get; set; }

        public string Label { get; set; }
    }
}
