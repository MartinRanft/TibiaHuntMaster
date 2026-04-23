using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TibiaHuntMaster.App.Components
{
    public sealed partial class DateRangePicker : UserControl
    {
        public static readonly StyledProperty<DateTimeOffset?> StartDateProperty =
        AvaloniaProperty.Register<DateRangePicker, DateTimeOffset?>(nameof(StartDate));

        public static readonly StyledProperty<DateTimeOffset?> EndDateProperty =
        AvaloniaProperty.Register<DateRangePicker, DateTimeOffset?>(nameof(EndDate));

        public static readonly StyledProperty<DateTimeOffset?> DraftStartDateProperty =
        AvaloniaProperty.Register<DateRangePicker, DateTimeOffset?>(nameof(DraftStartDate));

        public static readonly StyledProperty<DateTimeOffset?> DraftEndDateProperty =
        AvaloniaProperty.Register<DateRangePicker, DateTimeOffset?>(nameof(DraftEndDate));

        public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<DateRangePicker, string>(nameof(EmptyText), "All dates");

        private Button? _fieldButton;
        private Button? _clearFieldButton;
        private Border? _sheetBorder;

        public DateTimeOffset? StartDate
        {
            get => GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        public DateTimeOffset? EndDate
        {
            get => GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        public DateTimeOffset? DraftStartDate
        {
            get => GetValue(DraftStartDateProperty);
            set => SetValue(DraftStartDateProperty, value);
        }

        public DateTimeOffset? DraftEndDate
        {
            get => GetValue(DraftEndDateProperty);
            set => SetValue(DraftEndDateProperty, value);
        }

        public string EmptyText
        {
            get => GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        public DateRangePicker()
        {
            InitializeComponent();
            PropertyChanged += OnPickerPropertyChanged;
            _fieldButton = this.FindControl<Button>("FieldButton");
            _clearFieldButton = this.FindControl<Button>("ClearFieldButton");
            _sheetBorder = this.FindControl<Border>("SheetBorder");
            RefreshDisplay();
        }

        private void FieldButton_Click(object? sender, RoutedEventArgs e) => ToggleSheet();

        private void ClearFieldButton_Click(object? sender, RoutedEventArgs e) => ClearRange();

        private void OnPickerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == StartDateProperty || e.Property == EndDateProperty || e.Property == EmptyTextProperty)
            {
                RefreshDisplay();
            }

            if(e.Property == DraftEndDateProperty)
            {
                if(DraftStartDate == null && DraftEndDate == null)
                {
                    StartDate = null;
                    EndDate = null;
                    RefreshDisplay();
                    return;
                }

                if(DraftEndDate.HasValue)
                {
                    StartDate = DraftStartDate;
                    EndDate = DraftEndDate;
                    RefreshDisplay();
                    CloseSheet();
                }
            }
        }

        private void ToggleSheet()
        {
            if(_sheetBorder == null)
            {
                return;
            }

            bool isOpening = !_sheetBorder.IsVisible;
            if(isOpening)
            {
                DraftStartDate = StartDate;
                DraftEndDate = EndDate;
            }

            _sheetBorder.IsVisible = isOpening;
        }

        private void CloseSheet()
        {
            if(_sheetBorder != null)
            {
                _sheetBorder.IsVisible = false;
            }
        }

        private void ClearRange()
        {
            StartDate = null;
            EndDate = null;
            DraftStartDate = null;
            DraftEndDate = null;
            CloseSheet();
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            TextBlock? displayTextBlock = this.FindControl<TextBlock>("DisplayTextBlock");
            if(displayTextBlock != null)
            {
                displayTextBlock.Text = BuildDisplayText();
            }

            if(_clearFieldButton != null)
            {
                _clearFieldButton.IsVisible = StartDate.HasValue || EndDate.HasValue;
            }
        }

        private string BuildDisplayText()
        {
            if(!StartDate.HasValue && !EndDate.HasValue)
            {
                return EmptyText;
            }

            string start = StartDate.HasValue
            ? StartDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
            : "…";
            string end = EndDate.HasValue
            ? EndDate.Value.ToLocalTime().ToString("dd.MM.yyyy")
            : "…";

            return $"{start} - {end}";
        }
    }
}
