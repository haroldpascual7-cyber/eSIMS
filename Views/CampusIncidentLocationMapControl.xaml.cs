using System.Windows;
using System.Windows.Controls;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class CampusIncidentLocationMapControl : UserControl
    {
        public static readonly DependencyProperty LocationNameProperty =
            DependencyProperty.Register(nameof(LocationName), typeof(string), typeof(CampusIncidentLocationMapControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ZoneNameProperty =
            DependencyProperty.Register(nameof(ZoneName), typeof(string), typeof(CampusIncidentLocationMapControl), new PropertyMetadata("Campus Grounds"));

        public static readonly DependencyProperty ZoneKeyProperty =
            DependencyProperty.Register(nameof(ZoneKey), typeof(string), typeof(CampusIncidentLocationMapControl), new PropertyMetadata("GROUNDS"));

        public static readonly DependencyProperty PinXProperty =
            DependencyProperty.Register(nameof(PinX), typeof(double), typeof(CampusIncidentLocationMapControl), new PropertyMetadata(350.0));

        public static readonly DependencyProperty PinYProperty =
            DependencyProperty.Register(nameof(PinY), typeof(double), typeof(CampusIncidentLocationMapControl), new PropertyMetadata(200.0));

        public string LocationName
        {
            get => (string)GetValue(LocationNameProperty);
            set => SetValue(LocationNameProperty, value);
        }

        public string ZoneName
        {
            get => (string)GetValue(ZoneNameProperty);
            set => SetValue(ZoneNameProperty, value);
        }

        public string ZoneKey
        {
            get => (string)GetValue(ZoneKeyProperty);
            set => SetValue(ZoneKeyProperty, value);
        }

        public double PinX
        {
            get => (double)GetValue(PinXProperty);
            set => SetValue(PinXProperty, value);
        }

        public double PinY
        {
            get => (double)GetValue(PinYProperty);
            set => SetValue(PinYProperty, value);
        }

        public CampusIncidentLocationMapControl()
        {
            InitializeComponent();
        }
    }
}
