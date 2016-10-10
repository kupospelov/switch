namespace Switch.Devices
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;

    using Identity;
    using Packets;

    public abstract partial class Device : UserControl
    {
        private MacAddress mac;
        private DispatcherTimer sendTimer;

        public Device(Canvas canvas)
        {
            this.TransmittedFrames = new ObservableCollection<TransmittedFrame>();

            this.Canvas = canvas;
            this.mac = IdentityManager.GenerateAddress();

            this.sendTimer = new DispatcherTimer();
            this.sendTimer.IsEnabled = false;
            this.sendTimer.Tick += this.SendTimerTick;
        }

        // It could be a non-static property, but since the object is always the same for all instances,
        // let it be initialized once and shared by every device.
        public static MainWindow Window { get; set; }

        // A new wire is stored here until it has both paired devices selected.
        public static Wire NewWire { get; set; }

        public ObservableCollection<TransmittedFrame> TransmittedFrames { get; private set; }

        public MacAddress FramesDestination
        {
            get;
            private set;
        }

        public int FramesToSend
        {
            get;
            private set;
        }

        public MacAddress Mac
        {
            get
            {
                return this.mac;
            }

            set
            {
                IdentityManager.AddUniqueDevice(value);
                this.mac = value;
            }
        }

        public virtual string Label { get; set; }

        public Canvas Canvas { get; set; }

        public abstract void SendPacket(Packet packet);

        public void StopTransmission()
        {
            this.sendTimer.IsEnabled = false;
            this.FramesToSend = 0;
        }

        public void SendPackets(int count, int interval, MacAddress dest)
        {
            if (this.FramesToSend < 1)
            {
                this.FramesToSend = count;
                this.FramesDestination = dest;
                this.sendTimer.Interval = new TimeSpan(0, 0, 0, 0, interval);
                this.sendTimer.IsEnabled = true;

                // Start immediately
                this.SendTimerTick(null, null);
            }
        }

        public abstract void ReceivePacket(Wire wire, Packet packet);

        public abstract void UpdateLocation();

        public abstract int GetPort(Wire wire);

        public abstract bool AddWire(Wire wire);

        public abstract bool RemoveWire(bool deep, Wire wire = null);

        public abstract void Remove(object sender, RoutedEventArgs e);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(Window);
                DataObject data = new DataObject();
                data.SetData("Device", this);

                // Creation of a new wire
                if (Window.SelectedTool == MainWindow.Tools.Connector)
                {
                    Device.NewWire = new Wire(Canvas)
                    {
                        X1 = Canvas.GetLeft(this) + (Width / 2),
                        Y1 = Canvas.GetTop(this) + (Height / 2),
                        X2 = p.X,
                        Y2 = p.Y,
                        D1 = this
                    };

                    if (this.AddWire(Device.NewWire))
                    {
                        Canvas.SetZIndex(Device.NewWire, -1);
                        Canvas.Children.Add(Device.NewWire);
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Link);
                    }
                    else
                    {
                        Device.NewWire.Remove(null);
                        MainWindow.ShowError(Properties.Resources.NoFreePortsToConnect);
                        Device.NewWire = null;
                    }
                }
                else
                {
                    // Otherwise move the object
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                }

                e.Handled = true;
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (Device.NewWire != null && Device.NewWire.D1 != this)
            {
                if (this.AddWire(Device.NewWire))
                {
                    Device.NewWire.X2 = Canvas.GetLeft(this) + (this.Width / 2);
                    Device.NewWire.Y2 = Canvas.GetTop(this) + (this.Height / 2);
                    Device.NewWire.D2 = this;
                    Device.NewWire.MouseLeftButtonDown += Window.OnWireMouseLeftButtonDown;
                    Device.NewWire.D2.UpdateLocation();
                    Window.Modified = true;
                }
                else
                {
                    Device.NewWire.Remove(null);
                    MainWindow.ShowError(Properties.Resources.NoFreePortsToConnect);
                }

                Device.NewWire = null;
            }
        }

        private void SendTimerTick(object sender, EventArgs e)
        {
            if (this.FramesToSend > 0)
            {
                this.SendPacket(new Packet
                {
                    Source = this.Mac,
                    Destination = this.FramesDestination
                });

                --this.FramesToSend;
            }
            else
            {
                this.sendTimer.IsEnabled = false;
            }
        }

        public class TransmittedFrame
        {
            public enum FrameAction
            {
                Received,
                Sent,
                Forwarded
            }

            public DateTime Time { get; set; }

            public FrameAction Action { get; set; }

            public string Label { get; set; }
        }
    }
}
