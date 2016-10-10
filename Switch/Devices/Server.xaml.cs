namespace Switch.Devices
{
    using System.Windows.Controls;

    using Identity;

    /// <summary>
    /// Interaction logic for Server.xaml
    /// </summary>
    public partial class Server : Computer
    {
        public Server() : this(null)
        {
        }

        public Server(Canvas canvas) : base(canvas)
        {
            this.InitializeComponent();
            this.Label = IdentityManager.GenerateLabel(Properties.Resources.Server);
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
    }
}
