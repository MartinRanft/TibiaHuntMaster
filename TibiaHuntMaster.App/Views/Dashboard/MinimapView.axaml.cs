using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using TibiaHuntMaster.App.ViewModels.Dashboard;

namespace TibiaHuntMaster.App.Views.Dashboard
{
    public partial class MinimapView : UserControl
    {
        private readonly Border _viewport;

        private bool _isDragging;
        private Point _lastPointer;

        public MinimapView()
        {
            InitializeComponent();

            _viewport = this.FindControl<Border>("Viewport")
                ?? throw new InvalidOperationException("Viewport control not found in MinimapView.");

            AttachedToVisualTree += OnAttachedToVisualTree;
            DetachedFromVisualTree += OnDetachedFromVisualTree;
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _viewport.SizeChanged += Viewport_OnSizeChanged;
            await PushViewportSizeToViewModelAsync();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            _viewport.SizeChanged -= Viewport_OnSizeChanged;
        }

        private async void OnDataContextChanged(object? sender, EventArgs e)
        {
            await PushViewportSizeToViewModelAsync();
        }

        private async void Viewport_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (DataContext is MinimapViewModel vm)
            {
                await vm.UpdateViewportSizeAsync((int)e.NewSize.Width, (int)e.NewSize.Height);
            }
        }

        private Task PushViewportSizeToViewModelAsync()
        {
            int width = (int)Math.Round(_viewport.Bounds.Width);
            int height = (int)Math.Round(_viewport.Bounds.Height);

            if (DataContext is not MinimapViewModel vm)
            {
                return Task.CompletedTask;
            }

            return vm.UpdateViewportSizeAsync(width, height);
        }

        private void Viewport_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            PointerPoint point = e.GetCurrentPoint(_viewport);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            _isDragging = true;
            _lastPointer = e.GetPosition(_viewport);

            e.Pointer.Capture(_viewport);
            e.Handled = true;
        }
        
        private void Viewport_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is not MinimapViewModel vm)
            {
                return;
            }

            Point current = e.GetPosition(_viewport);
            vm.UpdateCursorFromViewport(current.X, current.Y);

            if (!_isDragging)
            {
                return;
            }

            Vector delta = current - _lastPointer;
            _lastPointer = current;

            double zoom = vm.Zoom > 0.001 ? vm.Zoom : 1.0;
            vm.ApplyDragDelta(delta.X / zoom, delta.Y / zoom);

            e.Handled = true;
        }
        
        private async void Viewport_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            e.Pointer.Capture(null);

            if (DataContext is MinimapViewModel vm)
            {
                await vm.CommitPanAsync();
            }

            e.Handled = true;
        }

        private async void Viewport_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (DataContext is not MinimapViewModel vm)
            {
                return;
            }

            // Ctrl + Wheel => Z-Level, plain Wheel => zoom
            if ((e.KeyModifiers & KeyModifiers.Control) == 0)
            {
                Point anchor = e.GetPosition(_viewport);
                vm.ApplyWheelZoom(e.Delta.Y, anchor.X, anchor.Y);
                vm.UpdateCursorFromViewport(anchor.X, anchor.Y);
                e.Handled = true;
                return;
            }

            if (e.Delta.Y > 0)
            {
                await vm.ZUpAsync();
            }
            else if (e.Delta.Y < 0)
            {
                await vm.ZDownAsync();
            }

            e.Handled = true;
        }
    }
}
