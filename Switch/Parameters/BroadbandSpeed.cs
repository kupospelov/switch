namespace Switch.Parameters
{
    using System.Collections.Generic;
    using System.Linq;

    public class BroadbandSpeed
    {
        public const int DefaultCost = 19;

        public static readonly IReadOnlyCollection<BroadbandSpeed> Speeds = new BroadbandSpeed[]
        {
            new BroadbandSpeed(250, Properties.Resources.S4Mbps),

            new BroadbandSpeed(100, Properties.Resources.S10Mbps),

            new BroadbandSpeed(62, Properties.Resources.S16Mbps),

            new BroadbandSpeed(19, Properties.Resources.S100Mbps),

            new BroadbandSpeed(4, Properties.Resources.S1Gbps),

            new BroadbandSpeed(3, Properties.Resources.S2Gbps),

            new BroadbandSpeed(2, Properties.Resources.S10Gbps)
        };

        public BroadbandSpeed(int cost, string title)
        {
            this.Cost = cost;
            this.Title = title;
        }

        public int Cost { get; private set; }

        public string Title { get; private set; }

        public static string GetTitle(int cost)
        {
            return Speeds.Single(s => s.Cost == cost).Title;
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
