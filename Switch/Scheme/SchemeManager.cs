namespace Switch.Scheme
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Xml;

    using Devices;
    using Identity;
    using Parameters;

    internal static class SchemeManager
    {
        private static readonly IReadOnlyCollection<int> AgingTimes = AgingTime.AgingTimes.Select(t => t.Milliseconds).ToArray();

        public static void LoadScheme(
            string filename,
            Canvas canvas,
            MouseButtonEventHandler onBridgeMouseDown,
            MouseButtonEventHandler onComputerMouseDown,
            MouseButtonEventHandler onWireMouseDown)
        {
            Device[] pair = new Device[2];

            Dictionary<string, Device> devices = new Dictionary<string, Device>();
            string srchash, desthash;
            int value;
            bool flag;

            using (XmlReader reader = XmlReader.Create(filename, new XmlReaderSettings()))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "Switch":
                                Bridge bridge = new Bridge(canvas);
                                Canvas.SetTop(bridge, 0);
                                Canvas.SetLeft(bridge, 0);

                                while (reader.MoveToNextAttribute())
                                {
                                    switch (reader.Name)
                                    {
                                        case "HashCode":
                                            devices[reader.Value] = bridge;
                                            break;

                                        case "Position":
                                            SetPosition(bridge, reader.Value);
                                            break;

                                        case "AgingTime":
                                            SetAgingTime(bridge, reader.Value);
                                            break;

                                        case "Label":
                                            bridge.Label = reader.Value;
                                            break;

                                        case "MAC":
                                            bridge.Mac = MacAddress.Create(reader.Value);
                                            break;

                                        case "STP":
                                            if (bool.TryParse(reader.Value, out flag))
                                            {
                                                bridge.Stp = flag;
                                            }
                                            else
                                            {
                                                throw new FormatException(Properties.Resources.InvalidAttribute + " STP=\"" + reader.Value + "\"");
                                            }

                                            break;

                                        case "Priority":
                                            SetPriority(bridge, reader.Value);
                                            break;
                                    }
                                }

                                if (onBridgeMouseDown != null)
                                {
                                    bridge.MouseLeftButtonDown += onBridgeMouseDown;
                                }

                                canvas.Children.Add(bridge);
                                break;

                            case "PC":
                            case "Server":
                                Computer computer;

                                if (reader.Name == "PC")
                                {
                                    computer = new PC(canvas);
                                }
                                else
                                {
                                    computer = new Server(canvas);
                                }

                                Canvas.SetTop(computer, 0);
                                Canvas.SetLeft(computer, 0);

                                while (reader.MoveToNextAttribute())
                                {
                                    switch (reader.Name)
                                    {
                                        case "HashCode":
                                            devices[reader.Value] = computer;
                                            break;

                                        case "Position":
                                            SetPosition(computer, reader.Value);
                                            break;

                                        case "Label":
                                            computer.Label = reader.Value;
                                            break;

                                        case "MAC":
                                            computer.Mac = MacAddress.Create(reader.Value);
                                            break;
                                    }
                                }

                                if (onComputerMouseDown != null)
                                {
                                    computer.MouseLeftButtonDown += onComputerMouseDown;
                                }

                                canvas.Children.Add(computer);
                                break;

                            case "Wire":
                                reader.MoveToAttribute("Source");
                                pair[0] = GetDevice(devices, reader.Value);
                                srchash = reader.Value;

                                reader.MoveToAttribute("Destination");
                                pair[1] = GetDevice(devices, reader.Value);
                                desthash = reader.Value;

                                Wire wire = new Wire(canvas)
                                {
                                    X1 = Canvas.GetLeft(pair[0]) + (pair[0].Width / 2),
                                    Y1 = Canvas.GetTop(pair[0]) + (pair[0].Height / 2),
                                    X2 = Canvas.GetLeft(pair[1]) + (pair[1].Width / 2),
                                    Y2 = Canvas.GetTop(pair[1]) + (pair[1].Height / 2),
                                    D1 = pair[0],
                                    D2 = pair[1]
                                };

                                if (onWireMouseDown != null)
                                {
                                    wire.MouseLeftButtonDown += onWireMouseDown;
                                }

                                Canvas.SetZIndex(wire, -1);
                                canvas.Children.Add(wire);

                                reader.MoveToAttribute("SourcePort");
                                if (int.TryParse(reader.Value, out value))
                                {
                                    if (value < 0)
                                    {
                                        computer = pair[0] as Computer;

                                        if (computer == null)
                                        {
                                            throw new FormatException(
                                                Properties.Resources.DeviceWithHashCode + " \"" + srchash + "\" " + Properties.Resources.IsNotComputer.ToLower());
                                        }

                                        if (computer.Wire != null)
                                        {
                                            computer.Wire.Remove();
                                        }

                                        computer.Wire = wire;
                                    }
                                    else
                                    {
                                        bridge = pair[0] as Bridge;

                                        if (bridge == null)
                                        {
                                            throw new FormatException(
                                                Properties.Resources.DeviceWithHashCode + " \"" + srchash + "\" " + Properties.Resources.IsNotSwitch.ToLower());
                                        }

                                        bridge.ConnectHost(wire, value);
                                    }
                                }
                                else
                                {
                                    throw new FormatException(
                                        Properties.Resources.InvalidAttribute + " SourcePort=\"" + reader.Value + "\"");
                                }

                                reader.MoveToAttribute("DestinationPort");
                                if (int.TryParse(reader.Value, out value))
                                {
                                    if (value < 0)
                                    {
                                        computer = pair[1] as Computer;

                                        if (computer == null)
                                        {
                                            throw new FormatException(
                                                Properties.Resources.DeviceWithHashCode + " \"" + desthash + "\" " + Properties.Resources.IsNotComputer.ToLower());
                                        }

                                        if (computer.Wire != null)
                                        {
                                            computer.Wire.Remove();
                                        }

                                        computer.Wire = wire;
                                    }
                                    else
                                    {
                                        bridge = pair[1] as Bridge;

                                        if (bridge == null)
                                        {
                                            throw new FormatException(
                                                Properties.Resources.DeviceWithHashCode + " \"" + desthash + "\" " + Properties.Resources.IsNotSwitch.ToLower());
                                        }

                                        bridge.ConnectHost(wire, value);
                                    }
                                }
                                else
                                {
                                    throw new FormatException(Properties.Resources.InvalidAttribute + " DestinationPort=\"" + reader.Value + "\"");
                                }

                                wire.D2.UpdateLocation();
                                break;
                        }
                    }
                }
            }
        }

        public static void WriteSchemeToFile(string filename, Canvas canvas)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";

            using (XmlWriter writer = XmlWriter.Create(filename, settings))
            {
                writer.WriteStartElement("Scheme");

                foreach (var item in canvas.Children)
                {
                    Bridge bridge = item as Bridge;
                    if (bridge != null)
                    {
                        writer.WriteStartElement("Switch");
                        writer.WriteAttributeString("HashCode", null, bridge.GetHashCode().ToString());
                        writer.WriteAttributeString("Position", null, Canvas.GetLeft(bridge).ToString() + "," + Canvas.GetTop(bridge).ToString());
                        writer.WriteAttributeString("AgingTime", null, bridge.AgingTime.ToString());
                        writer.WriteAttributeString("MAC", null, bridge.Mac.ToString());
                        writer.WriteAttributeString("Label", null, bridge.Label);
                        writer.WriteAttributeString("STP", null, bridge.Stp.ToString());
                        writer.WriteAttributeString("Priority", null, bridge.Priority.ToString());
                        writer.WriteEndElement();
                        continue;
                    }

                    Computer computer = item as Computer;
                    if (computer != null)
                    {
                        writer.WriteStartElement(item is PC ? "PC" : "Server");
                        writer.WriteAttributeString("HashCode", null, computer.GetHashCode().ToString());
                        writer.WriteAttributeString("Position", null, Canvas.GetLeft(computer).ToString() + "," + Canvas.GetTop(computer).ToString());
                        writer.WriteAttributeString("Label", null, computer.Label);
                        writer.WriteAttributeString("MAC", null, computer.Mac.ToString());
                        writer.WriteEndElement();
                        continue;
                    }

                    Wire wire = item as Wire;
                    if (wire != null)
                    {
                        writer.WriteStartElement("Wire");
                        writer.WriteAttributeString("Source", null, wire.D1.GetHashCode().ToString());
                        writer.WriteAttributeString("SourcePort", null, wire.D1.GetPort(wire).ToString());
                        writer.WriteAttributeString("Destination", null, wire.D2.GetHashCode().ToString());
                        writer.WriteAttributeString("DestinationPort", null, wire.D2.GetPort(wire).ToString());
                        writer.WriteEndElement();
                        continue;
                    }
                }

                writer.WriteEndElement();
            }
        }

        private static void SetPosition(Device dev, string value)
        {
            string[] xy = value.Split(',');
            double left, top;

            if (xy.Length == 2 && double.TryParse(xy[0], out left) && double.TryParse(xy[1], out top))
            {
                Canvas.SetLeft(dev, left);
                Canvas.SetTop(dev, top);
            }
            else
            {
                throw new FormatException(Properties.Resources.InvalidAttribute + " Position=\"" + value + "\"");
            }
        }

        private static void SetAgingTime(Bridge bridge, string value)
        {
            int time;

            if (int.TryParse(value, out time) && AgingTimes.Contains(time))
            {
                bridge.AgingTime = time;
            }
            else
            {
                throw new FormatException(Properties.Resources.InvalidAttribute + " AgingTime=\"" + value + "\"");
            }
        }

        private static void SetPriority(Bridge bridge, string value)
        {
            int priority;

            if (int.TryParse(value, out priority) && BridgePriority.Priorities.Contains(priority))
            {
                bridge.Priority = priority;
            }
            else
            {
                throw new FormatException(Properties.Resources.InvalidAttribute + " Priority=\"" + value + "\"");
            }
        }
        
        private static Device GetDevice(Dictionary<string, Device> dictionary, string hashcode)
        {
            Device device;
            if (dictionary.TryGetValue(hashcode, out device))
            {
                return device;
            }

            throw new FormatException(Properties.Resources.UnknownDeviceWithHashCode + " \"" + hashcode + "\"");
        }
    }
}
