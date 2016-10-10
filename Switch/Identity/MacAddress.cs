namespace Switch.Identity
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class MacAddress : IComparable<MacAddress>
    {
        public static readonly MacAddress Multicast;

        private static readonly Regex ValidationRegex;

        private readonly byte[] address;

        static MacAddress()
        {
            ValidationRegex = new Regex(@"\A([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}\Z");
            Multicast = Create("01:80:C2:00:00:00");
        }

        private MacAddress(byte[] addr)
        {
            this.address = addr;
        }

        public static MacAddress Create(Random rand)
        {
            var address = new byte[6];

            rand.NextBytes(address);

            return new MacAddress(address);
        }

        public static MacAddress Create(string address)
        {
            if (!Validate(address))
            {
                throw new ArgumentException(string.Format("{0}: {1}", Properties.Resources.NotValidMacAddress, address));
            }

            return new MacAddress(address.Split(':').Select(a => Convert.ToByte(a, 16)).ToArray());
        }

        public static bool operator ==(MacAddress a, MacAddress b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(MacAddress a, MacAddress b)
        {
            return a.CompareTo(b) != 0;
        }

        public static bool operator <(MacAddress a, MacAddress b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(MacAddress a, MacAddress b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool Validate(string mac)
        {
            return ValidationRegex.IsMatch(mac);
        }

        public int CompareTo(MacAddress other)
        {
            for (int i = 0; i < 6; ++i)
            {
                if (this.address[i] < other.address[i])
                {
                    return -1;
                }

                if (this.address[i] > other.address[i])
                {
                    return 1;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return string.Join(":", this.address.Select(a => a.ToString("X2")));
        }

        public override int GetHashCode()
        {
            var result = 0;

            foreach (var s in this.address)
            {
                result ^= s.GetHashCode();
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            MacAddress other = obj as MacAddress;
            if (other != null)
            {
                return this.CompareTo(other) == 0;
            }

            return base.Equals(obj);
        }
    }
}
