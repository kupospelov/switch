namespace Switch.Devices
{
    using System.Windows.Controls;

    using Identity;

    /// <summary>
    /// Interaction logic for PC.xaml
    /// </summary>
    public partial class PC : Computer
    {
        public PC() : this(null)
        {
        }

        public PC(Canvas canvas) : base(canvas)
        {
            this.InitializeComponent();
            this.Label = IdentityManager.GenerateLabel(Properties.Resources.PC);
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
