using Avalonia;
using Avalonia.Controls;

namespace TibiaHuntMaster.App.Components
{
    public sealed partial class ErrorMessage : UserControl
    {
        public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ErrorMessage, string>(nameof(Message), "An error occurred");

        public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ErrorMessage, string>(nameof(Title), "Error");

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ErrorMessage()
        {
            InitializeComponent();
        }
    }
}
