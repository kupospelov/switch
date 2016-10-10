namespace Switch.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Shapes;

    using Identity;
    using Packets;
    using Parameters;

    /// <summary>
    /// Interaction logic for Wire.xaml
    /// </summary>
    public partial class Wire : UserControl
    {
        private readonly Storyboard storyboard = new Storyboard();
        private readonly Queue<PacketQueueItem> packetQueue = new Queue<PacketQueueItem>();
        private readonly List<Path> inAnimation = new List<Path>();

        private int cost;
        private bool busy;
        private EllipseGeometry circle;

        public Wire(Canvas canvas)
        {
            this.InitializeComponent();

            this.Canvas = canvas;
            this.Cost = BroadbandSpeed.DefaultCost;

            this.circle = new EllipseGeometry
            {
                Center = new Point(this.X1, this.Y1),
                RadiusX = 5.0,
                RadiusY = 5.0
            };

            this.RegisterName("Circle", this.circle);
        }

        public enum PortState
        {
            Opened,
            Blocked
        }

        public static MainWindow Window
        {
            get; set;
        }

        public Canvas Canvas
        {
            get;
            set;
        }

        public Device D1
        {
            get;
            set;
        }

        public Device D2
        {
            get;
            set;
        }

        public Brush Stroke
        {
            get
            {
                return Line.Stroke;
            }

            set
            {
                Line.Stroke = value;
            }
        }

        public Stretch Stretch
        {
            get
            {
                return Line.Stretch;
            }

            set
            {
                Line.Stretch = value;
                Highlight.Stretch = value;
            }
        }

        public double X1
        {
            get
            {
                return Line.X1;
            }

            set
            {
                Line.X1 = value;
                Highlight.X1 = value;
            }
        }

        public double X2
        {
            get
            {
                return Line.X2;
            }

            set
            {
                Line.X2 = value;
                Highlight.X2 = value;
            }
        }

        public double Y1
        {
            get
            {
                return Line.Y1;
            }

            set
            {
                Line.Y1 = value;
                Highlight.Y1 = value;
            }
        }

        public double Y2
        {
            get
            {
                return Line.Y2;
            }

            set
            {
                Line.Y2 = value;
                Highlight.Y2 = value;
            }
        }

        public PortState P1
        {
            get;
            private set;
        }

        public PortState P2
        {
            get;
            private set;
        }

        public bool Busy
        {
            get
            {
                return this.busy;
            }

            private set
            {
                this.busy = value;

                if (value)
                {
                    this.WireBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    this.WireBorder.Visibility = Visibility.Hidden;
                }
            }
        }

        public int Cost
        {
            get
            {
                return this.cost;
            }

            set
            {
                this.cost = value;
            }
        }

        public bool GetBlocked(Device caller)
        {
            if (this.D1 == caller)
            {
                return this.P1 == PortState.Blocked;
            }

            if (this.D2 == caller)
            {
                return this.P2 == PortState.Blocked;
            }

            return false;
        }

        public void SetBlocked(Device caller, bool value)
        {
            if (value)
            {
                if (this.D1 == caller)
                {
                    this.P1 = PortState.Blocked;
                }

                if (this.D2 == caller)
                {
                    this.P2 = PortState.Blocked;
                }

                if (this.P1 == PortState.Blocked || this.P2 == PortState.Blocked)
                {
                    this.Line.Stroke = Brushes.Khaki;
                }
            }
            else
            {
                if (this.D1 == caller)
                {
                    this.P1 = PortState.Opened;
                }

                if (this.D2 == caller)
                {
                    this.P2 = PortState.Opened;
                }

                if (this.P1 == PortState.Opened && this.P2 == PortState.Opened)
                {
                    this.Line.Stroke = Brushes.DarkSeaGreen;
                }
            }
        }

        public Device GetPairedDevice(Device caller)
        {
            if (this.D1 == caller)
            {
                return this.D2;
            }

            if (this.D2 == caller)
            {
                return this.D1;
            }

            return null;
        }

        public void SendPacket(Device caller, Packet packet, Func<Wire, bool> relevant = null)
        {
            PacketQueueItem item = new PacketQueueItem
            {
                Packet = packet,
                Sender = caller,
                Receiver = this.GetPairedDevice(caller),
                Relevant = relevant
            };

            if (item.Receiver != null)
            {
                item.AnimationPath = new Path();
                item.AnimationPath.Fill = packet.Brush ?? Brushes.LightBlue;
                item.AnimationPath.Stroke = Brushes.DarkGray;
                item.AnimationPath.StrokeThickness = 1;
                item.AnimationPath.Data = this.circle;
                this.packetQueue.Enqueue(item);
                this.SendPacketFromQueue();
            }
        }

        public void StopTransmission()
        {
            this.packetQueue.Clear();

            foreach (var item in this.inAnimation)
            {
                this.storyboard.Stop(item);
            }

            this.inAnimation.Clear();
            this.Busy = false;
        }

        public void PauseTransmission()
        {
            foreach (var item in this.inAnimation)
            {
                this.storyboard.Pause(item);
            }
        }

        public void ResumeTransmission()
        {
            foreach (var item in this.inAnimation)
            {
                this.storyboard.Resume(item);
            }
        }

        public void UpdateLocation(Device device, double x, double y)
        {
            if (this.D1 == device)
            {
                this.X1 = x;
                this.Y1 = y;
            }
            else if (this.D2 == device)
            {
                this.X2 = x;
                this.Y2 = y;
            }

            Canvas.SetTop(this.WireBorder, (this.Y1 + this.Y2 - this.WireBorder.Height) / 2);
            Canvas.SetLeft(this.WireBorder, (this.X1 + this.X2 - this.WireBorder.Width) / 2);
        }

        public void Remove(object sender = null, RoutedEventArgs e = null)
        {
            this.StopTransmission();

            if (this.D1 != null && this.D1 != sender)
            {
                this.D1.RemoveWire(false, this);
            }

            if (this.D2 != null && this.D2 != sender)
            {
                this.D2.RemoveWire(false, this);
            }

            if (this.Canvas != null)
            {
                this.Canvas.Children.Remove(this);
            }

            Window.Modified = true;
        }

        private void SendPacketFromQueue()
        {
            if (this.Busy == false && this.packetQueue.Count > 0)
            {
                PacketQueueItem item = this.packetQueue.Dequeue();

                if (item.Relevant == null || item.Relevant(this))
                {
                    PointAnimation animation = new PointAnimation();
                    if (item.Sender == this.D1)
                    {
                        animation.From = new Point(this.X1, this.Y1);
                        animation.To = new Point(this.X2, this.Y2);
                    }
                    else
                    {
                        animation.To = new Point(this.X1, this.Y1);
                        animation.From = new Point(this.X2, this.Y2);
                    }

                    animation.Duration = new Duration(TimeSpan.FromMilliseconds(Window.AnimationTime.Value));
                    animation.AutoReverse = false;
                    animation.Completed += (object sender, EventArgs e) =>
                    {
                        WireCanvas.Children.Remove(item.AnimationPath);
                        storyboard.Children.Remove(animation);
                        inAnimation.Remove(item.AnimationPath);

                        if (item.Packet.Destination == MacAddress.Multicast || !this.GetBlocked(item.Receiver))
                        {
                            item.Receiver.ReceivePacket(this, item.Packet);
                        }

                        Busy = false;
                        SendPacketFromQueue();
                    };

                    Storyboard.SetTargetName(animation, "Circle");
                    Storyboard.SetTargetProperty(animation, new PropertyPath(EllipseGeometry.CenterProperty));

                    this.Busy = true;
                    this.WireLabel.Content = item.Packet.Label;
                    this.WireBorder.BorderBrush = item.Packet.Brush;

                    this.circle.RadiusX = this.circle.RadiusY = item.Packet.Radius;

                    this.WireCanvas.Children.Add(item.AnimationPath);
                    this.storyboard.Children.Add(animation);
                    this.inAnimation.Add(item.AnimationPath);
                    this.storyboard.Begin(item.AnimationPath, true);

                    if (!Window.TransmissionIsEnabled)
                    {
                        this.PauseTransmission();
                    }
                }
            }
        }

        private class PacketQueueItem
        {
            public Path AnimationPath { get; set; }

            public Device Sender { get; set; }

            public Device Receiver { get; set; }

            public Packet Packet { get; set; }

            public Func<Wire, bool> Relevant { get; set; }
        }
    }
}
