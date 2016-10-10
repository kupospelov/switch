namespace Switch.Identity
{
    using System;
    using System.Collections.Generic;

    public static class IdentityManager
    {
        private static readonly HashSet<MacAddress> UniqueAddresses = new HashSet<MacAddress>();
        private static readonly HashSet<string> UniqueLabels = new HashSet<string>();

        public static void Reset()
        {
            UniqueLabels.Clear();
            UniqueAddresses.Clear();
            UniqueAddresses.Add(MacAddress.Multicast);

            GenerateLabel(Properties.Resources.Switch);
            GenerateLabel(Properties.Resources.PC);
            GenerateLabel(Properties.Resources.Server);
        }

        public static void AddUniqueDevice(MacAddress address)
        {
            UniqueAddresses.Add(address);
        }

        public static MacAddress GenerateAddress()
        {
            Random rand = new Random();
            var address = MacAddress.Create(rand);
            
            while (UniqueAddresses.Contains(address))
            {
                address = MacAddress.Create(rand);
            }

            return address;
        }

        public static string GenerateLabel(string baseword)
        {
            string name = baseword;

            if (UniqueLabels.Contains(baseword))
            {
                for (int i = 1; i < 10000; ++i)
                {
                    string suffix = i.ToString();

                    if (!UniqueLabels.Contains(name + suffix))
                    {
                        name += suffix;
                        break;
                    }
                }
            }

            UniqueLabels.Add(name);
            return name;
        }
    }
}
