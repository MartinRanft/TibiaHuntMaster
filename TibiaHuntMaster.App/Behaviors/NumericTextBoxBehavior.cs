using Avalonia;
using Avalonia.Controls;

namespace TibiaHuntMaster.App.Behaviors
{
    public static class NumericTextBoxBehavior
    {
        public static readonly AttachedProperty<bool> DigitsOnlyProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "DigitsOnly",
            typeof(NumericTextBoxBehavior));

        private static readonly AttachedProperty<string> LastValidTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string>(
            "LastValidText",
            typeof(NumericTextBoxBehavior),
            "0");

        static NumericTextBoxBehavior()
        {
            DigitsOnlyProperty.Changed.AddClassHandler<TextBox>((tb, e) =>
            {
                if(e.NewValue is not bool enabled)
                {
                    return;
                }

                if(enabled)
                {
                    // Startwert: wenn leer -> 0
                    string start = string.IsNullOrWhiteSpace(tb.Text) ? "0" : tb.Text!;
                    tb.Text = start;
                    tb.SetValue(LastValidTextProperty, start);

                    tb.TextChanged += OnTextChanged;
                }
                else
                {
                    tb.TextChanged -= OnTextChanged;
                }
            });
        }

        public static void SetDigitsOnly(AvaloniaObject element, bool value)
        {
            element.SetValue(DigitsOnlyProperty, value);
        }

        public static bool GetDigitsOnly(AvaloniaObject element)
        {
            return element.GetValue(DigitsOnlyProperty);
        }

        private static void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if(sender is not TextBox tb)
            {
                return;
            }

            string text = tb.Text ?? "";
            string lastValid = tb.GetValue(LastValidTextProperty);

            // Leer soll immer 0 sein
            if(text.Length == 0)
            {
                if(tb.Text != "0")
                {
                    tb.Text = "0";
                    tb.CaretIndex = 1;
                }
                tb.SetValue(LastValidTextProperty, "0");
                return;
            }

            // Nur Ziffern erlauben
            bool isValid = text.All(char.IsDigit);

            if(isValid)
            {
                // Optional: führende Nullen bereinigen (außer "0")
                // text = text.TrimStart('0');
                // if (text.Length == 0) text = "0";

                tb.SetValue(LastValidTextProperty, text);
            }
            else
            {
                // Ungültig -> zurück
                tb.Text = lastValid;
                tb.CaretIndex = tb.Text?.Length ?? 0;
            }
        }
    }
}