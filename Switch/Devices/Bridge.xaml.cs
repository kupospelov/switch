namespace Switch.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Threading;

    using Identity;
    using Packets;
    using Parameters;

    /// <summary>
    /// Interaction logic for Bridge.xaml
    /// </summary>
    public partial class Bridge : Device
    {
        public readonly ObservableCollection<MainWindow.TableItem> AddressTable = new ObservableCollection<MainWindow.TableItem>();

        // Constants
        private const int ForwardDelay = 15000;
        private const int HelloInterval = 2000;
        private const int MaxAge = 20000;
        private const int NumberOfPorts = 8;

        private bool reducedAgingTimer;
        private Wire[] ports;
        private Action<int> portChoiceAction;
        private DispatcherTimer dpduTimer, tcTimer, agingTimer;

        // STP
        private bool stp, tcn, tc;
        private int priority;
        private int agingTime;
        private int rootPathCost;

        private Func<Wire, bool> checkNotBlocked, checkNotBlockedAndTcn;

        public Bridge() : this(null)
        {
        }

        public Bridge(Canvas canvas) : base(canvas)
        {
            this.InitializeComponent();
            this.AgingTime = Parameters.AgingTime.DefaultAgingTime;
            this.priority = BridgePriority.DefaultPriority;

            this.checkNotBlocked = (w) => !w.GetBlocked(this);
            this.checkNotBlockedAndTcn = (w) => (!w.GetBlocked(this) && this.Tcn);

            this.dpduTimer = new DispatcherTimer();
            this.dpduTimer.Tick += this.SendDpdu;
            this.dpduTimer.Interval = new TimeSpan(0, 0, 0, 0, HelloInterval);
            this.dpduTimer.IsEnabled = false;

            this.tcTimer = new DispatcherTimer();
            this.tcTimer.Tick += (object sender, EventArgs e) => this.Tc = false;
            this.tcTimer.Interval = new TimeSpan(0, 0, 0, 0, MaxAge + ForwardDelay);
            this.tcTimer.IsEnabled = false;

            this.agingTimer = new DispatcherTimer();
            this.agingTimer.Tick += this.RefreshAddressTable;
            this.agingTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            this.agingTimer.IsEnabled = true;

            this.N = NumberOfPorts;
            this.RootBId = new Bpdu.BridgeId(this.Priority, this.Mac);
            this.Label = IdentityManager.GenerateLabel(Properties.Resources.Switch);
        }

        private enum PortState
        {
            Blocked,
            Root,
            Designated
        }

        public Bpdu.BridgeId RootBId { get; private set; }

        public int RootPort { get; private set; }

        public int AgingTime
        {
            get
            {
                return this.reducedAgingTimer ? ForwardDelay : this.agingTime;
            }

            set
            {
                this.reducedAgingTimer = false;
                this.agingTime = value;
            }
        }

        public override string Label
        {
            get
            {
                return base.Label;
            }

            set
            {
                base.Label = value;
                LabelBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
            }
        }

        public int Priority
        {
            get
            {
                return this.priority;
            }

            set
            {
                this.priority = value;
                this.IsRoot = true;
                this.UnblockAllPorts();
                this.AddressTable.Clear();
            }
        }

        public bool IsRoot
        {
            get
            {
                return this.RootBId == this.GetBId() && this.Stp;
            }

            private set
            {
                if (value)
                {
                    this.RootBId = this.GetBId();
                    this.RootPort = -1;
                    this.rootPathCost = 0;
                    this.BaseRectangle.Fill = Brushes.LightSlateGray;
                    this.Tcn = true;
                    this.dpduTimer.IsEnabled = true;
                }
                else
                {
                    this.BaseRectangle.Fill = Brushes.Silver;
                    this.tcTimer.IsEnabled = false;
                    this.Tc = false;
                }
            }
        }

        public int N
        {
            get
            {
                return this.ports.Length;
            }

            set
            {
                this.AddressTable.Clear();
                this.ports = new Wire[value];

                this.PortStack.Children.Clear();
                for (int i = 0; i < value; ++i)
                {
                    DockPanel dp = new DockPanel
                    {
                        Margin = new Thickness(0, 0, 0, i != value - 1 ? 3 : 0),
                        LastChildFill = false
                    };

                    Label lb = new Label { Content = Properties.Resources.Port + " " + i.ToString() };
                    DockPanel.SetDock(lb, Dock.Left);
                    dp.Children.Add(lb);

                    Button bn = new Button { Content = Properties.Resources.Down, MinWidth = 100 };
                    bn.Click += this.Curry(i);

                    DockPanel.SetDock(bn, Dock.Right);
                    dp.Children.Add(bn);

                    this.PortStack.Children.Add(dp);
                }
            }
        }

        public bool Stp
        {
            get
            {
                return this.stp;
            }

            set
            {
                this.stp = value;
                this.IsRoot = value;

                if (!value)
                {
                    this.Tcn = false;
                    this.UnblockAllPorts();
                }
            }
        }

        public bool Tcn
        {
            get
            {
                return this.tcn;
            }

            private set
            {
                this.tcn = value;

                if (value)
                {
                    if (!this.dpduTimer.IsEnabled)
                    {
                        this.dpduTimer.IsEnabled = true;
                    }
                }
                else
                {
                    if (!this.IsRoot)
                    {
                        this.dpduTimer.IsEnabled = false;
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

            private set
            {
                this.tc = value;
            }
        }

        public RoutedEventHandler Curry(int port)
        {
            return (object sender, RoutedEventArgs e) =>
            {
                if (portChoiceAction != null)
                {
                    portChoiceAction(port);
                    portChoiceAction = null;
                    HideChoosePortPopup();
                }
            };
        }

        public Bpdu.BridgeId GetBId()
        {
            return new Bpdu.BridgeId(this.Priority, this.Mac);
        }

        public void ShowChoosePortPopup(Action<int> action)
        {
            this.PortsPopup.IsOpen = true;
            this.portChoiceAction = action;
        }

        public void HideChoosePortPopup()
        {
            this.PortsPopup.IsOpen = false;
        }

        public void UnblockAllPorts()
        {
            for (int i = 0; i < this.N; ++i)
            {
                if (this.ports[i] != null)
                {
                    this.ports[i].SetBlocked(this, false);
                }
            }
        }

        public void ClearPortInformation(int port)
        {
            int i = 0;

            while (i < this.AddressTable.Count)
            {
                if (this.AddressTable[i].Port == port)
                {
                    this.AddressTable.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
        }

        public override void SendPacket(Packet packet)
        {
            this.TransmittedFrames.Add(new TransmittedFrame
            {
                Action = TransmittedFrame.FrameAction.Sent,
                Label = packet.Label,
                Time = DateTime.Now
            });

            this.HandlePacket(packet);
        }

        public override void ReceivePacket(Wire wire, Packet packet)
        {
            if (packet.Destination == MacAddress.Multicast)
            {
                if (this.Stp)
                {
                    Bpdu bpdu = packet as Bpdu;
                    bool changes = false;

                    if (bpdu != null)
                    {
                        int portcost = bpdu.RootPathCost + wire.Cost, port = this.GetPort(wire);

                        // Check if received RootBID is better than the one of this bridge
                        if (bpdu.RootBId < this.RootBId)
                        {
                            if (this.IsRoot)
                            {
                                this.IsRoot = false;
                            }

                            this.RootBId = new Bpdu.BridgeId(bpdu.RootBId);
                            this.RootPort = port;
                            this.rootPathCost = portcost;

                            this.UnblockAllPorts();
                            this.Tcn = changes = true;
                        }

                        // Check if RootBID of this bridge is better than received RootBID
                        if (this.RootBId > this.GetBId())
                        {
                            if (!this.IsRoot)
                            {
                                this.IsRoot = true;
                            }

                            this.UnblockAllPorts();
                        }

                        // Check if the bridge received a packet from the root
                        if (!this.IsRoot && bpdu.RootBId.Mac == this.RootBId.Mac)
                        {
                            // Network change acknowledged
                            if (bpdu.Tca)
                            {
                                if (!changes)
                                {
                                    this.Tcn = false;
                                }
                            }

                            // Set reduced aging time value
                            if (bpdu.Tc)
                            {
                                if (!this.reducedAgingTimer)
                                {
                                    for (int i = 0; i < this.AddressTable.Count; ++i)
                                    {
                                        if (this.AddressTable[i].Eta > ForwardDelay)
                                        {
                                            this.AddressTable[i].Eta = ForwardDelay;
                                        }
                                    }

                                    this.reducedAgingTimer = true;
                                }
                            }
                            else
                            {
                                this.reducedAgingTimer = false;
                            }

                            if (port != this.RootPort)
                            {
                                if (portcost < this.rootPathCost || ((portcost == this.rootPathCost) && port < this.RootPort))
                                {
                                    if (this.rootPathCost < portcost + (2 * this.ports[this.RootPort].Cost))
                                    {
                                        this.ports[this.RootPort].SetBlocked(this, true);
                                    }

                                    wire.SetBlocked(this, false);
                                    this.RootPort = port;
                                    this.rootPathCost = portcost;
                                    this.Tcn = true;
                                }
                                else
                                {
                                    if (bpdu.RootPathCost < this.rootPathCost + wire.Cost)
                                    {
                                        wire.SetBlocked(this, true);
                                    }
                                    else
                                    {
                                        wire.SetBlocked(this, false);
                                    }
                                }
                            }
                            else
                            {
                                this.RootBId = bpdu.RootBId;
                                this.rootPathCost = portcost;

                                Bpdu update = new Bpdu
                                {
                                    Destination = bpdu.Destination,
                                    BId = this.GetBId(),
                                    RootBId = this.RootBId,
                                    RootPathCost = this.rootPathCost,
                                    Tc = bpdu.Tc
                                };

                                for (int i = 0; i < this.N; ++i)
                                {
                                    if (this.ports[i] != null && this.ports[i] != wire && !this.ports[i].GetBlocked(this))
                                    {
                                        this.ports[i].SendPacket(this, update, this.checkNotBlocked);
                                    }
                                }
                            }
                        }

                        if (bpdu.Tcn)
                        {
                            this.Tcn = true;

                            wire.SendPacket(
                                this,
                                new Bpdu
                                {
                                    Source = Mac,
                                    Destination = MacAddress.Multicast,
                                    BId = this.GetBId(),
                                    RootBId = this.RootBId,
                                    RootPathCost = this.rootPathCost,
                                    Tca = true
                                },
                                this.checkNotBlockedAndTcn);

                            if (this.IsRoot)
                            {
                                this.Tc = true;
                                this.tcTimer.IsEnabled = true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (packet.Destination == this.Mac)
                {
                    // Receive the packet
                    TransmittedFrames.Add(
                        new TransmittedFrame
                        {
                            Action = TransmittedFrame.FrameAction.Received,
                            Label = packet.Label,
                            Time = DateTime.Now
                        });
                }
                else
                {
                    // Forward the packet
                    TransmittedFrames.Add(
                        new TransmittedFrame
                        {
                            Action = TransmittedFrame.FrameAction.Forwarded,
                            Label = packet.Label,
                            Time = DateTime.Now
                        });

                    this.HandlePacket(packet, wire);
                }
            }
        }

        public override void UpdateLocation()
        {
            for (int i = 0; i < this.N; ++i)
            {
                if (this.ports[i] != null)
                {
                    this.ports[i].UpdateLocation(this, Canvas.GetLeft(this) + (this.Width / 2), Canvas.GetTop(this) + (this.Height / 2));
                }
            }
        }

        public override void Remove(object sender, RoutedEventArgs e)
        {
            this.RemoveWire(true);
            if (this.Canvas != null)
            {
                this.Canvas.Children.Remove(this);
            }

            Window.Modified = true;
        }

        public override int GetPort(Wire wire)
        {
            for (int i = 0; i < this.N; ++i)
            {
                if (this.ports[i] != null && this.ports[i] == wire)
                {
                    return i;
                }
            }

            return -2;
        }

        public override bool AddWire(Wire wire)
        {
            for (int i = 0; i < this.N; ++i)
            {
                if (this.ports[i] == null)
                {
                    this.ports[i] = wire;
                    return true;
                }
            }

            return false;
        }

        public override bool RemoveWire(bool deep, Wire wire = null)
        {
            bool flag = false;
            
            if (wire == null)
            {
                this.AddressTable.Clear();
            }

            for (int i = 0; i < this.N; ++i)
            {
                if (this.ports[i] != null && (wire == null || this.ports[i] == wire))
                {
                    Button bn = (Button)((DockPanel)PortStack.Children[i]).Children[1];
                    bn.Content = Properties.Resources.Down;
                    bn.Background = SystemColors.ControlLightBrush;

                    if (this.Stp)
                    {
                        if (i == this.RootPort)
                        {
                            this.IsRoot = true;
                        }
                        else
                        {
                            if (!wire.GetBlocked(this))
                            {
                                flag = true;
                            }
                        }
                    }

                    if (deep)
                    {
                        this.ports[i].Remove(this);
                    }

                    this.ports[i] = null;
                    this.ClearPortInformation(i);
                }
            }

            if (flag)
            {
                this.Tcn = true;
            }

            return flag;
        }

        public void ConnectHost(Wire wire, int port = 0)
        {
            if (port < 0 || port >= this.N)
            {
                throw new ArgumentOutOfRangeException(Properties.Resources.PortNumberIsNotInSwitchRange);
            }

            if (this.ports[port] != null)
            {
                this.ports[port].Remove();
            }

            this.ports[port] = wire;
            DockPanel dp = (DockPanel)PortStack.Children[port];
            Button bn = (Button)dp.Children[1];
            bn.Background = Brushes.DarkSeaGreen;
            bn.Content = Properties.Resources.Connected;
        }

        public void RefreshAddressTable(object sender, EventArgs e)
        {
            List<MainWindow.TableItem> removable = new List<MainWindow.TableItem>();

            for (int i = 0; i < this.AddressTable.Count; ++i)
            {
                if (this.AddressTable[i].Eta > 0)
                {
                    this.AddressTable[i].Eta = Math.Max(this.AddressTable[i].Eta - 1000, 0);
                }
                else
                {
                    removable.Add(this.AddressTable[i]);
                }
            }

            for (int i = 0; i < removable.Count; ++i)
            {
                this.AddressTable.Remove(removable[i]);
            }
        }

        private void SendDpdu(object sender, EventArgs e)
        {
            if (this.IsRoot)
            {
                Bpdu dpdu = new Bpdu
                {
                    Source = this.Mac,
                    Destination = MacAddress.Multicast,
                    BId = this.GetBId(),
                    RootBId = this.GetBId(),
                    RootPathCost = 0,
                    Tca = this.Tcn,
                    Tc = this.Tc
                };

                for (int i = 0; i < this.N; ++i)
                {
                    if (this.ports[i] != null)
                    {
                        this.ports[i].SendPacket(this, dpdu, this.checkNotBlocked);
                    }
                }

                this.Tcn = false;
            }
            else if (this.Tcn)
            {
                // Forward TCN to the root switch
                if (this.RootPort >= 0 && this.ports[this.RootPort] != null)
                {
                    this.ports[this.RootPort].SendPacket(
                        this,
                        new Bpdu
                        {
                            Source = this.Mac,
                            Destination = MacAddress.Multicast,
                            BId = this.GetBId(),
                            RootBId = this.RootBId,
                            RootPathCost = this.rootPathCost,
                            Tcn = true
                        },
                        this.checkNotBlocked);
                }
            }
        }

        private void HandlePacket(Packet packet, Wire wire = null)
        {
            int inport = -1, outport = -1;

            foreach (var item in this.AddressTable)
            {
                if (item.Mac == packet.Source)
                {
                    inport = item.Port;
                    item.Eta = this.AgingTime;
                }

                if (item.Mac == packet.Destination)
                {
                    outport = item.Port;
                }
            }

            if (inport < 0 && wire != null)
            {
                this.AddressTable.Add(new MainWindow.TableItem(this.GetPort(wire), packet.Source, this.AgingTime));
            }

            if (outport < 0 || this.ports[outport].GetBlocked(this))
            {
                if (outport >= 0)
                {
                    this.ClearPortInformation(outport);
                }

                for (int i = 0; i < this.N; ++i)
                {
                    if (this.ports[i] != null && this.ports[i] != wire && !this.ports[i].GetBlocked(this))
                    {
                        this.ports[i].SendPacket(this, packet, this.checkNotBlocked);
                    }
                }
            }
            else
            {
                this.ports[outport].SendPacket(this, packet, this.checkNotBlocked);
            }
        }
    }
}
