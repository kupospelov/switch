namespace Switch
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using Microsoft.Win32;
    using Converters;
    using Identity;
    using Devices;
    using Parameters;
    using Scheme;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Action<Device> onDeviceMouseDown = null;
        private Wire selectedWire;
        private Device[] transmission = new Device[2];
        private Device selectedDevice;

        private bool modified;
        private string fileName;

        public MainWindow()
        {
            this.InitializeComponent();
            this.FillDropdowns();

            this.Title = string.Format("{0} - {1}", Properties.Resources.Title, Properties.Resources.NewScheme);
            this.Closing += this.OnWindowClosing;

            Device.Window = Wire.Window = this;
            this.TransmissionIsEnabled = true;
            this.SelectedTool = Tools.Cursor;
            this.SwitchPanel.IsExpanded = this.DevicePanel.IsExpanded = this.WirePanel.IsExpanded = true;
        }

        public enum Tools
        {
            Connector,
            Server,
            PC,
            Switch,
            Cursor
        }

        public bool TransmissionIsEnabled
        {
            get;
            set;
        }

        public bool Modified
        {
            get
            {
                return this.modified;
            }

            set
            {
                if (this.modified)
                {
                    if (!value && this.Title.Length > 0 && this.Title[this.Title.Length - 1] == '*')
                    {
                        this.Title = this.Title.Substring(0, Title.Length - 1);
                    }
                }
                else
                {
                    if (value)
                    {
                        this.Title = this.Title + '*';
                    }
                }

                this.modified = value;
            }
        }

        public Tools SelectedTool
        {
            get
            {
                if (this.AddConnectorButton.Opacity > 0.9)
                {
                    return Tools.Connector;
                }

                if (this.AddServerButton.Opacity > 0.9)
                {
                    return Tools.Server;
                }

                if (this.AddPCButton.Opacity > 0.9)
                {
                    return Tools.PC;
                }

                if (this.AddSwitchButton.Opacity > 0.9)
                {
                    return Tools.Switch;
                }

                return Tools.Cursor;
            }

            private set
            {
                switch (value)
                {
                    case Tools.Connector:
                        this.AddSwitchButton.Opacity = this.AddServerButton.Opacity = this.AddPCButton.Opacity = this.CursorButton.Opacity = 0.4;
                        this.AddConnectorButton.Opacity = 1.0;
                        break;

                    case Tools.Server:
                        this.AddConnectorButton.Opacity = this.AddPCButton.Opacity = this.AddSwitchButton.Opacity = this.CursorButton.Opacity = 0.4;
                        this.AddServerButton.Opacity = 1.0;
                        break;

                    case Tools.PC:
                        this.AddConnectorButton.Opacity = this.AddServerButton.Opacity = this.AddSwitchButton.Opacity = this.CursorButton.Opacity = 0.4;
                        this.AddPCButton.Opacity = 1.0;
                        break;

                    case Tools.Switch:
                        this.AddConnectorButton.Opacity = this.AddServerButton.Opacity = this.AddPCButton.Opacity = this.CursorButton.Opacity = 0.4;
                        this.AddSwitchButton.Opacity = 1.0;
                        break;

                    default:
                        this.AddConnectorButton.Opacity = this.AddServerButton.Opacity = this.AddPCButton.Opacity = this.AddSwitchButton.Opacity = 0.4;
                        this.CursorButton.Opacity = 1.0;
                        break;
                }
            }
        }

        private string FileName
        {
            get
            {
                return this.fileName;
            }

            set
            {
                this.fileName = value;

                if (!string.IsNullOrEmpty(this.fileName))
                {
                    this.Title = Properties.Resources.Title + " - " + this.fileName.Split('\\').Last();
                }
                else
                {
                    this.Title = Properties.Resources.Title + " - " + Properties.Resources.NewScheme;
                }
            }
        }

        public static void ShowError(string text)
        {
            MessageBox.Show(text, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void AddressTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteAddressButton.IsEnabled = e.AddedItems.Count > 0;
        }

        public void AddressTableRemoveRow(object sender, RoutedEventArgs e)
        {
            var device = (Bridge)this.selectedDevice;
            var index = SwitchTable.SelectedIndex;

            if (device != null && index >= 0)
            {
                device.AddressTable.RemoveAt(index);
            }
        }

        public void AddressTableAddRow(object sender, RoutedEventArgs e)
        {
            var device = (Bridge)this.selectedDevice;

            if (device != null)
            {
                device.AddressTable.Add(new TableItem(0, MacAddress.Create("00:00:00:00:00:00"), AgingTime.DefaultAgingTime));
            }
        }

        public void AddressTableClear(object sender, RoutedEventArgs e)
        {
            var device = (Bridge)this.selectedDevice;

            if (device != null)
            {
                device.AddressTable.Clear();
            }
        }

        public void TransmittedFramesClear(object sender, RoutedEventArgs e)
        {
            var device = this.selectedDevice;

            if (device != null)
            {
                device.TransmittedFrames.Clear();
            }
        }

        public void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!this.CloseScheme())
            {
                e.Cancel = true;
            }
        }

        public void OnBridgeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Bridge bridge = (Bridge)sender;
            this.selectedDevice = bridge;

            this.NumberOfPorts.Text = bridge.N.ToString();
            this.Label.Text = bridge.Label;
            this.Address.Text = bridge.Mac.ToString();

            this.STPCheckBox.IsChecked = bridge.Stp;
            this.BridgeAgingTimes.Text = AgingTime.GetTitle(bridge.AgingTime);
            this.BridgePriorities.Text = bridge.Priority.ToString();
            this.RootSwitch.Text = bridge.RootBId.Mac.ToString();

            this.SwitchTable.DataContext = bridge.AddressTable;
            this.TransmittedFrames.DataContext = bridge.TransmittedFrames;

            this.SetExpanderVisibility(Visibility.Visible, Visibility.Collapsed, this.WirePanel);

            if (this.onDeviceMouseDown != null)
            {
                this.onDeviceMouseDown(bridge);
                this.onDeviceMouseDown = null;
                e.Handled = true;
            }
        }

        public void OnWireMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.selectedWire = (Wire)sender;
            int port;

            port = this.selectedWire.D1.GetPort(this.selectedWire);
            this.FromMacLabel.Text = this.selectedWire.D1.Mac.ToString();
            this.FromPortStateLabel.Text = this.selectedWire.GetBlocked(this.selectedWire.D1) ?
                Properties.Resources.Blocked :
                Properties.Resources.Open;

            if (port < 0)
            {
                this.FromDeviceLabel.Text = Properties.Resources.Computer;
                this.FromPortLabel.Text = Properties.Resources.DefaultPort;
            }
            else
            {
                this.FromDeviceLabel.Text = Properties.Resources.Switch;
                this.FromPortLabel.Text = port.ToString();
            }

            port = this.selectedWire.D2.GetPort(this.selectedWire);
            this.ToMacLabel.Text = this.selectedWire.D2.Mac.ToString();
            this.ToPortStateLabel.Text = this.selectedWire.GetBlocked(this.selectedWire.D2) ?
                Properties.Resources.Blocked :
                Properties.Resources.Open;

            if (port < 0)
            {
                this.ToDeviceLabel.Text = Properties.Resources.Computer;
                this.ToPortLabel.Text = Properties.Resources.DefaultPort;
            }
            else
            {
                this.ToDeviceLabel.Text = Properties.Resources.Switch;
                this.ToPortLabel.Text = port.ToString();
            }

            this.WireBroadband.Text = BroadbandSpeed.GetTitle(this.selectedWire.Cost);

            this.SetExpanderVisibility(Visibility.Collapsed, Visibility.Visible, this.WirePanel);
        }

        public void OnComputerMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Computer computer = (Computer)sender;
            this.selectedDevice = computer;

            this.Label.Text = computer.Label;
            this.Address.Text = computer.Mac.ToString();
            this.TransmittedFrames.DataContext = computer.TransmittedFrames;

            this.SetExpanderVisibility(Visibility.Collapsed, Visibility.Visible, this.DevicePanel);

            if (this.onDeviceMouseDown != null)
            {
                this.onDeviceMouseDown(computer);
                this.onDeviceMouseDown = null;
                e.Handled = true;
            }
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            if (e.Data.GetDataPresent("Device"))
            {
                Device obj = (Device)e.Data.GetData("Device");
                Point p = e.GetPosition(this);

                // Creation of a new wire
                if (Device.NewWire != null)
                {
                    Device.NewWire.X2 = p.X;

                    // Menubar height
                    Device.NewWire.Y2 = p.Y - 20;
                }
                else
                {
                    // Drag & drop of an object
                    Canvas.SetLeft(obj, p.X - (obj.Width / 2));
                    Canvas.SetTop(obj, p.Y - (obj.Height / 2));
                    obj.UpdateLocation();
                    this.Modified = true;
                }
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (Device.NewWire != null)
            {
                Device.NewWire.Remove(null);
                Device.NewWire = null;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point p = e.GetPosition(this);

            switch (this.SelectedTool)
            {
                case Tools.PC:
                    PC computer = new PC(this.MainCanvas);
                    Canvas.SetLeft(computer, p.X - (computer.Width / 2));
                    Canvas.SetTop(computer, p.Y - (computer.Height / 2));
                    computer.MouseLeftButtonDown += this.OnComputerMouseLeftButtonDown;
                    this.MainCanvas.Children.Add(computer);
                    this.Modified = true;
                    break;

                case Tools.Server:
                    Server server = new Server(this.MainCanvas);
                    Canvas.SetLeft(server, p.X - (server.Width / 2));
                    Canvas.SetTop(server, p.Y - (server.Height / 2));
                    server.MouseLeftButtonDown += this.OnComputerMouseLeftButtonDown;
                    this.MainCanvas.Children.Add(server);
                    this.Modified = true;
                    break;

                case Tools.Switch:
                    Bridge bridge = new Bridge(this.MainCanvas);
                    Canvas.SetLeft(bridge, p.X - (bridge.Width / 2));
                    Canvas.SetTop(bridge, p.Y - (bridge.Height / 2));
                    bridge.MouseLeftButtonDown += this.OnBridgeMouseLeftButtonDown;
                    this.MainCanvas.Children.Add(bridge);
                    this.Modified = true;
                    break;

                case Tools.Connector:
                    break;
            }

            if (this.onDeviceMouseDown != null)
            {
                this.onDeviceMouseDown(null);
                this.onDeviceMouseDown = null;
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            this.SelectedTool = Tools.Cursor;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (Device.NewWire != null)
            {
                Device.NewWire.Remove(null);
                Device.NewWire = null;
            }
        }

        private static void SetHandler<T>(ref Action<T> dest, Action<T> src, bool rewrite = false) where T : class
        {
            if (dest != null && rewrite == false)
            {
                dest(null);
            }

            dest = src;
        }

        private void FillDropdowns()
        {
            foreach (var speed in BroadbandSpeed.Speeds)
            {
                this.WireBroadband.Items.Add(speed);
            }

            foreach (var time in AgingTime.AgingTimes)
            {
                this.BridgeAgingTimes.Items.Add(time);
            }

            foreach (var priority in BridgePriority.Priorities)
            {
                this.BridgePriorities.Items.Add(priority);
            }
        }

        private void AddComputer(object sender, RoutedEventArgs e)
        {
            this.SelectedTool = Tools.PC;
        }

        private void AddServer(object sender, RoutedEventArgs e)
        {
            this.SelectedTool = Tools.Server;
        }

        private void AddSwitch(object sender, RoutedEventArgs e)
        {
            this.SelectedTool = Tools.Switch;
        }

        private void AddConnector(object sender, RoutedEventArgs e)
        {
            this.SelectedTool = Tools.Connector;
        }

        private void ResetSelection(object sender, RoutedEventArgs e)
        {
            this.SelectedTool = Tools.Cursor;
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Properties.Resources.AboutText, Properties.Resources.About, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChangeAddress(object sender, RoutedEventArgs e)
        {
            this.Address.Text = IdentityManager.GenerateAddress().ToString();
        }

        private void SaveDeviceData(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Label.Text.Length == 0)
                {
                    MessageBox.Show(Properties.Resources.NotValidName, Properties.Resources.Error);
                }
                else
                {
                    this.selectedDevice.Mac = MacAddress.Create(this.Address.Text);
                    this.selectedDevice.Label = this.Label.Text;
                    this.Address.Text = this.selectedDevice.Mac.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Error);
            }
        }

        private void BridgeEnableStp(object sender, RoutedEventArgs e)
        {
            Bridge bridge = (Bridge)this.selectedDevice;

            if (!bridge.Stp)
            {
                bridge.Stp = true;
            }

            this.Modified = true;
        }

        private void BridgeDisableStp(object sender, RoutedEventArgs e)
        {
            Bridge bridge = (Bridge)this.selectedDevice;

            if (bridge.Stp)
            {
                bridge.Stp = false;
            }

            this.Modified = true;
        }

        private void BridgeSetPriority(object sender, SelectionChangedEventArgs e)
        {
            Bridge bridge = this.selectedDevice as Bridge;

            if (bridge != null)
            {
                int priority = (int)e.AddedItems[0];
                if (priority != bridge.Priority)
                {
                    bridge.Priority = priority;
                    this.Modified = true;
                }
            }
        }

        private void BridgeSetAgingTime(object sender, SelectionChangedEventArgs e)
        {
            Bridge bridge = this.selectedDevice as Bridge;

            if (bridge != null && e.AddedItems.Count > 0)
            {
                var time = e.AddedItems[0] as AgingTime;
                
                if (time != null && time.Milliseconds != bridge.AgingTime)
                {
                    bridge.AgingTime = time.Milliseconds;
                    this.Modified = true;
                }
            }
        }

        private void WireSetSpeed(object sender, SelectionChangedEventArgs e)
        {
            if (this.selectedWire != null)
            {
                var speed = (BroadbandSpeed)e.AddedItems[0];

                if (speed.Cost != this.selectedWire.Cost)
                {
                    this.selectedWire.Cost = speed.Cost;
                    this.Modified = true;
                }
            }
        }

        private void RemoveDevice(object sender, RoutedEventArgs e)
        {
            if (this.transmission[0] == this.selectedDevice)
            {
                this.transmission[0] = null;
                this.ButtonSource.Content = Properties.Resources.NotChosen;
            }

            if (this.transmission[1] == this.selectedDevice)
            {
                this.transmission[1] = null;
                this.ButtonDestination.Content = Properties.Resources.NotChosen;
            }

            this.selectedDevice.Remove(null, null);
            this.SetExpanderVisibility(Visibility.Collapsed);
        }

        private void RemoveWire(object sender, RoutedEventArgs e)
        {
            this.selectedWire.Remove(null);
            this.SetExpanderVisibility(Visibility.Collapsed);
        }

        private void SetSourceAddress(object sender, RoutedEventArgs e)
        {
            this.ButtonSource.Content = Properties.Resources.ClickOnDevice;
            this.ButtonSource.Background = Brushes.LightBlue;

            if (this.onDeviceMouseDown != null)
            {
                this.onDeviceMouseDown(null);
            }

            this.onDeviceMouseDown = (Device dev) =>
            {
                this.ButtonSource.Background = SystemColors.ControlLightBrush;
                if (dev == null)
                {
                    this.ButtonSource.Content = Properties.Resources.NotChosen;
                    return;
                }

                this.ButtonSource.Content = dev.Mac;
                this.transmission[0] = dev;
            };
        }

        private void SetDestinationAddress(object sender, RoutedEventArgs e)
        {
            this.ButtonDestination.Content = Properties.Resources.ClickOnDevice;
            this.ButtonDestination.Background = Brushes.LightBlue;

            if (this.onDeviceMouseDown != null)
            {
                this.onDeviceMouseDown(null);
            }

            this.onDeviceMouseDown = (Device dev) =>
            {
                this.ButtonDestination.Background = SystemColors.ControlLightBrush;

                if (dev == null)
                {
                    this.ButtonDestination.Content = Properties.Resources.NotChosen;
                    return;
                }

                this.ButtonDestination.Content = dev.Mac;
                this.transmission[1] = dev;
            };
        }

        private void StartTransmission(object sender, RoutedEventArgs e)
        {
            if (this.transmission[0] != null && this.transmission[1] != null)
            {
                this.transmission[0].SendPackets((int)FrameNumberSlider.Value, (int)FrameIntervalSlider.Value, this.transmission[1].Mac);
            }
        }

        private void CreateNewScheme(object sender, RoutedEventArgs e)
        {
            if (this.CloseScheme())
            {
                this.ClearScheme();
            }
        }

        private void ClearScheme()
        {
            this.SetExpanderVisibility(Visibility.Collapsed);
            this.ButtonSource.Content = this.ButtonDestination.Content = Properties.Resources.NotChosen;

            this.MainCanvas.Children.Clear();

            IdentityManager.Reset();

            this.FileName = string.Empty;
            this.Modified = false;
            this.TransmissionIsEnabled = true;
        }

        private bool CloseScheme()
        {
            if (this.Modified)
            {
                switch (MessageBox.Show(Properties.Resources.SaveChanges, Properties.Resources.SchemeChanged, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel))
                {
                    case MessageBoxResult.Cancel:
                        return false;

                    case MessageBoxResult.Yes:
                        this.SaveScheme(null, null);
                        return false;

                    case MessageBoxResult.No:
                        break;
                }
            }

            return true;
        }

        private void SaveScheme(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.FileName))
            {
                try
                {
                    SchemeManager.WriteSchemeToFile(this.FileName, this.MainCanvas);
                    this.Modified = false;
                }
                catch (Exception ex)
                {
                    ShowError(Properties.Resources.CannotSave + ": " + ex.Message);
                }
            }
            else
            {
                this.SaveSchemeAs(sender, e);
            }
        }

        private void SaveSchemeAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = Properties.Resources.SchemeFiles + " (*.scheme)|*.scheme";
            dialog.RestoreDirectory = true;

            try
            {
                if (dialog.ShowDialog() == true)
                {
                    SchemeManager.WriteSchemeToFile(dialog.FileName, this.MainCanvas);
                    this.Modified = false;
                    this.FileName = dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                ShowError(Properties.Resources.CannotSave + ": " + ex.Message);
            }
        }

        private void LoadScheme(object sender, RoutedEventArgs e)
        {
            if (this.CloseScheme())
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = Properties.Resources.SchemeFiles + " (*.scheme)|*.scheme|" + Properties.Resources.AllFiles + "|*";
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        this.ClearScheme();

                        SchemeManager.LoadScheme(
                            dialog.FileName,
                            this.MainCanvas,
                            this.OnBridgeMouseLeftButtonDown,
                            this.OnComputerMouseLeftButtonDown,
                            this.OnWireMouseLeftButtonDown);

                        this.FileName = dialog.FileName;
                        this.Modified = false;
                    }
                    catch (Exception ex)
                    {
                        this.ClearScheme();
                        MessageBox.Show(Properties.Resources.CannotOpen + ": " + ex.Message, Properties.Resources.Error);
                    }
                }
            }
        }

        private void StopTransmission(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.MainCanvas.Children)
            {
                if (item is Wire)
                {
                    Wire wire = (Wire)item;
                    wire.StopTransmission();
                }
                else if (item is Device)
                {
                    Device device = (Device)item;
                    device.StopTransmission();
                }
            }
        }

        private void PauseTransmission(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.MainCanvas.Children)
            {
                Wire wire = item as Wire;

                if (wire != null)
                {
                    wire.PauseTransmission();
                }
            }

            this.TransmissionIsEnabled = false;
        }

        private void ResumeTransmission(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.MainCanvas.Children)
            {
                Wire wire = item as Wire;

                if (wire != null)
                {
                    wire.ResumeTransmission();
                }
            }

            this.TransmissionIsEnabled = true;
        }

        private void HideAllButThis(Expander exp)
        {
            var panels = new[] { this.SwitchPanel, this.WirePanel, this.DevicePanel };

            foreach (var panel in panels.Where(p => p != exp))
            {
                panel.Visibility = Visibility.Collapsed;
            }

            exp.Visibility = Visibility.Visible;
        }

        private void SetExpanderVisibility(Visibility forAll, Visibility forThis, Expander exp)
        {
            var panels = new[] { this.SwitchPanel, this.WirePanel, this.DevicePanel };

            foreach (var panel in panels.Where(p => p != exp))
            {
                panel.Visibility = forAll;
            }

            if (exp != null)
            {
                exp.Visibility = forThis;
            }
        }

        private void SetExpanderVisibility(Visibility forAll)
        {
            this.SetExpanderVisibility(forAll, forAll, null);
        }

        public class TableItem : INotifyPropertyChanged
        {
            private int eta;

            public event PropertyChangedEventHandler PropertyChanged;

            public TableItem(int port, MacAddress mac, int eta)
            {
                this.Port = port;
                this.Mac = mac;
                this.Eta = eta;
            }

            public int Port
            {
                get;
                set;
            }

            public MacAddress Mac
            {
                get;
                set;
            }

            public int Eta
            {
                get
                {
                    return this.eta;
                }

                set
                {
                    this.eta = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("ETA"));
                    }
                }
            }
        }
    }
}
