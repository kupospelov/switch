namespace Switch.Parameters
{
    using System.Collections.Generic;

    public class BridgePriority
    {
        public const int DefaultPriority = 32768;

        public static readonly IReadOnlyCollection<int> Priorities = new int[]
        {
            4096,
            8192,
            12228,
            16384,
            20480,
            24567,
            28672,
            32768,
            36864,
            40960
        };
    }
}
