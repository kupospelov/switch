namespace Switch.Devices
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    using Identity;
    using Packets;

    public class Computer : Device
    {
        public Computer(Canvas canvas) : base(canvas)
        {
        }

        public Wire Wire
        {
            get;
            set;
        }

        public override void SendPacket(Packet packet)
        {
            if (Wire != null)
            {
                Wire.SendPacket(this, packet);
                this.TransmittedFrames.Add(new TransmittedFrame
                {
                    Action = TransmittedFrame.FrameAction.Sent,
                    Label = packet.Label,
                    Time = DateTime.Now
                });
            }
        }

        public override void ReceivePacket(Wire wire, Packet packet)
        {
            if (packet.Destination != MacAddress.Multicast)
            {
                this.TransmittedFrames.Add(new TransmittedFrame
                {
                    Action = TransmittedFrame.FrameAction.Received,
                    Label = packet.Label,
                    Time = DateTime.Now
                });
            }
        }

        public override void UpdateLocation()
        {
            if (this.Wire != null)
            {
                this.Wire.UpdateLocation(this, Canvas.GetLeft(this) + (this.Width / 2), Canvas.GetTop(this) + (this.Height / 2));
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
            if (wire == Wire)
            {
                return -1;
            }

            return -2;
        }

        public override bool AddWire(Wire wire)
        {
            if (Wire == null)
            {
                Wire = wire;
                return true;
            }

            return false;
        }

        public override bool RemoveWire(bool deep, Wire wire = null)
        {
            if (Wire != null)
            {
                if (wire == null || wire == Wire)
                {
                    if (deep)
                    {
                        Wire.Remove(this);
                    }

                    Wire = null;
                    return true;
                }
            }

            return false;
        }
    }
}
