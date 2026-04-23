using Avalonia;
using Avalonia.Controls;

namespace TibiaHuntMaster.App.Components
{
    public sealed partial class EmptyState : UserControl
    {
        public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<EmptyState, string>(nameof(Message), "No data available");

        public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<EmptyState, string>(nameof(Icon), "📭");

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public string Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public EmptyState()
        {
            InitializeComponent();
        }
    }
}
