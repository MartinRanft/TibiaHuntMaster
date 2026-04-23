using System;
using System.Collections.ObjectModel;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace TibiaHuntMaster.App.Components
{
    public sealed partial class DateRangeCalendar : UserControl
    {
        public static readonly StyledProperty<DateTimeOffset?> StartDateProperty =
        AvaloniaProperty.Register<DateRangeCalendar, DateTimeOffset?>(
            nameof(StartDate),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<DateTimeOffset?> EndDateProperty =
        AvaloniaProperty.Register<DateRangeCalendar, DateTimeOffset?>(
            nameof(EndDate),
            defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<DateTimeOffset> CurrentMonthProperty =
        AvaloniaProperty.Register<DateRangeCalendar, DateTimeOffset>(
            nameof(CurrentMonth),
            new DateTimeOffset(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeOffset.Now.Offset));

        public static readonly StyledProperty<ObservableCollection<CalendarDay>> DaysProperty =
        AvaloniaProperty.Register<DateRangeCalendar, ObservableCollection<CalendarDay>>(
            nameof(Days),
            new ObservableCollection<CalendarDay>());

        public static readonly StyledProperty<ObservableCollection<string>> WeekdayHeadersProperty =
        AvaloniaProperty.Register<DateRangeCalendar, ObservableCollection<string>>(
            nameof(WeekdayHeaders),
            new ObservableCollection<string>());

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

        public DateTimeOffset CurrentMonth
        {
            get => GetValue(CurrentMonthProperty);
            set => SetValue(CurrentMonthProperty, value);
        }

        public ObservableCollection<CalendarDay> Days
        {
            get => GetValue(DaysProperty);
            set => SetValue(DaysProperty, value);
        }

        public ObservableCollection<string> WeekdayHeaders
        {
            get => GetValue(WeekdayHeadersProperty);
            set => SetValue(WeekdayHeadersProperty, value);
        }

        public DateRangeCalendar()
        {
            InitializeComponent();
            DataContext = this;
            PropertyChanged += OnCalendarPropertyChanged;
            BuildCalendar();
            AddHandler(Button.ClickEvent, OnDayButtonClick, Avalonia.Interactivity.RoutingStrategies.Bubble);
        }

        private void PrevMonthButton_Click(object? sender, RoutedEventArgs e)
        {
            CurrentMonth = GetStartOfMonth(CurrentMonth.AddMonths(-1));
        }

        private void NextMonthButton_Click(object? sender, RoutedEventArgs e)
        {
            CurrentMonth = GetStartOfMonth(CurrentMonth.AddMonths(1));
        }

        private void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            StartDate = null;
            EndDate = null;
            BuildCalendar();
        }

        private void OnDayButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if(e.Source is not Button { Tag: CalendarDay day })
            {
                return;
            }

            SelectDay(day);
            e.Handled = true;
        }

        private void OnCalendarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == StartDateProperty && StartDate.HasValue)
            {
                DateTimeOffset startMonth = GetStartOfMonth(StartDate.Value);
                if(startMonth.Year != CurrentMonth.Year || startMonth.Month != CurrentMonth.Month)
                {
                    CurrentMonth = startMonth;
                    return;
                }
            }

            if(e.Property == CurrentMonthProperty ||
               e.Property == StartDateProperty ||
               e.Property == EndDateProperty)
            {
                BuildCalendar();
            }
        }

        private void SelectDay(CalendarDay day)
        {
            if(!StartDate.HasValue || EndDate.HasValue)
            {
                StartDate = day.Date;
                EndDate = null;
                BuildCalendar();
                return;
            }

            if(day.Date < GetStartOfDay(StartDate.Value))
            {
                EndDate = StartDate;
                StartDate = day.Date;
            }
            else
            {
                EndDate = day.Date;
            }

            BuildCalendar();
        }

        private void BuildCalendar()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
            string[] dayNames = culture.DateTimeFormat.AbbreviatedDayNames;

            ObservableCollection<string> weekdayHeaders = [];
            for(int i = 0; i < 7; i++)
            {
                int index = (((int)firstDayOfWeek) + i) % 7;
                weekdayHeaders.Add(dayNames[index]);
            }

            WeekdayHeaders = weekdayHeaders;

            DateTimeOffset monthStart = GetStartOfMonth(CurrentMonth);
            DateTimeOffset visibleStart = monthStart;
            while(visibleStart.DayOfWeek != firstDayOfWeek)
            {
                visibleStart = visibleStart.AddDays(-1);
            }

            DateTimeOffset? rangeStart = StartDate.HasValue ? GetStartOfDay(StartDate.Value) : null;
            DateTimeOffset? rangeEnd = EndDate.HasValue ? GetStartOfDay(EndDate.Value) : null;
            if(rangeStart.HasValue && rangeEnd.HasValue && rangeStart > rangeEnd)
            {
                (rangeStart, rangeEnd) = (rangeEnd, rangeStart);
            }

            DateTimeOffset today = GetStartOfDay(DateTimeOffset.Now);
            ObservableCollection<CalendarDay> days = [];
            for(int i = 0; i < 42; i++)
            {
                DateTimeOffset currentDate = visibleStart.AddDays(i);
                bool isRangeStart = rangeStart.HasValue && currentDate.Date == rangeStart.Value.Date;
                bool isRangeEnd = rangeEnd.HasValue && currentDate.Date == rangeEnd.Value.Date;
                bool isInRange = rangeStart.HasValue &&
                                 ((!rangeEnd.HasValue && currentDate.Date == rangeStart.Value.Date) ||
                                  (rangeEnd.HasValue && currentDate >= rangeStart.Value && currentDate <= rangeEnd.Value));

                days.Add(new CalendarDay
                {
                    Date = currentDate,
                    IsCurrentMonth = currentDate.Month == monthStart.Month,
                    IsRangeStart = isRangeStart,
                    IsRangeEnd = isRangeEnd,
                    IsInRange = isInRange,
                    IsToday = currentDate.Date == today.Date
                });
            }

            Days = days;
        }

        private static DateTimeOffset GetStartOfDay(DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, value.Offset);
        }

        private static DateTimeOffset GetStartOfMonth(DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, value.Offset);
        }
    }

    public class CalendarDay : AvaloniaObject
    {
        public static readonly StyledProperty<DateTimeOffset> DateProperty =
        AvaloniaProperty.Register<CalendarDay, DateTimeOffset>(nameof(Date));

        public static readonly StyledProperty<bool> IsCurrentMonthProperty =
        AvaloniaProperty.Register<CalendarDay, bool>(nameof(IsCurrentMonth), true);

        public static readonly StyledProperty<bool> IsInRangeProperty =
        AvaloniaProperty.Register<CalendarDay, bool>(nameof(IsInRange));

        public static readonly StyledProperty<bool> IsRangeStartProperty =
        AvaloniaProperty.Register<CalendarDay, bool>(nameof(IsRangeStart));

        public static readonly StyledProperty<bool> IsRangeEndProperty =
        AvaloniaProperty.Register<CalendarDay, bool>(nameof(IsRangeEnd));

        public static readonly StyledProperty<bool> IsTodayProperty =
        AvaloniaProperty.Register<CalendarDay, bool>(nameof(IsToday));

        public DateTimeOffset Date
        {
            get => GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        public bool IsCurrentMonth
        {
            get => GetValue(IsCurrentMonthProperty);
            set => SetValue(IsCurrentMonthProperty, value);
        }

        public bool IsInRange
        {
            get => GetValue(IsInRangeProperty);
            set => SetValue(IsInRangeProperty, value);
        }

        public bool IsRangeStart
        {
            get => GetValue(IsRangeStartProperty);
            set => SetValue(IsRangeStartProperty, value);
        }

        public bool IsRangeEnd
        {
            get => GetValue(IsRangeEndProperty);
            set => SetValue(IsRangeEndProperty, value);
        }

        public bool IsToday
        {
            get => GetValue(IsTodayProperty);
            set => SetValue(IsTodayProperty, value);
        }

        public int Day => Date.Day;
    }
}
