namespace Switch.Packets
{
    using System;
    using System.Windows.Media;

    using Identity;

    public class Bpdu : Packet
    {
        private bool tcn, tca, tc;

        public Bpdu() : base(Brushes.LightSlateGray, 3, "BPDU")
        {
        }

        public BridgeId BId { get; set; }

        public BridgeId RootBId { get; set; }

        public int RootPathCost { get; set; }

        public bool Tcn
        {
            get
            {
                return this.tcn;
            }

            set
            {
                this.tcn = value;

                if (value)
                {    
                    this.Label = "TCN";
                }
                else
                {
                    if (this.tca)
                    {
                        this.Label = "TCA";
                    }
                    else if (this.tc)
                    {
                        this.Label = "TC";
                    }
                    else
                    {
                        this.Label = "BPDU";
                    }
                }
            }
        }

        public bool Tca
        {
            get
            {
                return this.tca;
            }

            set
            {
                this.tca = value;

                if (value)
                {
                    this.Label = "TCA";
                }
                else
                {
                    if (this.tcn)
                    {
                        this.Label = "TCN";
                    }
                    else if (this.tc)
                    {
                        this.Label = "TC";
                    }
                    else
                    {
                        this.Label = "BPDU";
                    }
                }
            }
        }

        public bool Tc
        {
            get
            {
                return this.tc;
            }

            set
            {
                this.tc = value;

                if (value)
                {
                    this.Label = "TC";
                }
                else
                {
                    if (this.tcn)
                    {
                        this.Label = "TCN";
                    }
                    else if (this.tca)
                    {
                        this.Label = "TCA";
                    }
                    else
                    {
                        this.Label = "BPDU";
                    }
                }
            }
        }

        public class BridgeId : IComparable<BridgeId>
        {
            public BridgeId(int priority, MacAddress mac)
            {
                this.Priority = priority;
                this.Mac = mac;
            }

            public BridgeId(BridgeId bid)
            {
                this.Priority = bid.Priority;
                this.Mac = bid.Mac;
            }

            public int Priority { get; private set; }

            public MacAddress Mac { get; private set; }

            public static bool operator ==(BridgeId a, BridgeId b)
            {
                return a.CompareTo(b) == 0;
            }

            public static bool operator !=(BridgeId a, BridgeId b)
            {
                return a.CompareTo(b) != 0;
            }

            public static bool operator <(BridgeId a, BridgeId b)
            {
                return a.CompareTo(b) < 0;
            }

            public static bool operator >(BridgeId a, BridgeId b)
            {
                return a.CompareTo(b) > 0;
            }

            public int CompareTo(BridgeId other)
            {
                if (this.Priority < other.Priority || ((this.Priority == other.Priority) && this.Mac < other.Mac))
                {
                    return -1;
                }
                else if (other.Priority < this.Priority || ((this.Priority == other.Priority) && this.Mac > other.Mac))
                {
                    return 1;
                }

                return 0;
            }

            public override bool Equals(object obj)
            {
                BridgeId bid = obj as BridgeId;

                if (bid != null)
                {
                    return this.CompareTo(bid) == 0;
                }

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return this.Mac.GetHashCode() ^ this.Priority.GetHashCode();
            }
        }
    }
}
