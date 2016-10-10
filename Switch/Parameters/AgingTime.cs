namespace Switch.Parameters
{
    using System.Collections.Generic;
    using System.Linq;

    public class AgingTime
    {
        public const int DefaultAgingTime = 300000;

        public static readonly IReadOnlyCollection<AgingTime> AgingTimes = new AgingTime[]
        {
            new AgingTime(5000, Properties.Resources.FiveSecs),

            new AgingTime(15000, Properties.Resources.FiveteenSecs),

            new AgingTime(30000, Properties.Resources.ThirtySecs),

            new AgingTime(60000, Properties.Resources.OneMin),

            new AgingTime(120000, Properties.Resources.TwoMins),

            new AgingTime(300000, Properties.Resources.FiveMins),

            new AgingTime(600000, Properties.Resources.TenMins)
        };

        private AgingTime(int ms, string title)
        {
            this.Milliseconds = ms;
            this.Title = title;
        }

        public int Milliseconds { get; private set; }

        public string Title { get; private set; }

        public static string GetTitle(int value)
        {
            return AgingTimes.Single(t => t.Milliseconds == value).Title;
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
