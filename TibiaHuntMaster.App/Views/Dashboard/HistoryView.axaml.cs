using Avalonia.Controls;
using Avalonia.Input;

using TibiaHuntMaster.App.ViewModels.Dashboard;

namespace TibiaHuntMaster.App.Views.Dashboard
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        private void OverlayBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if(DialogCard.IsPointerOver)
            {
                return;
            }

            if(DataContext is HistoryViewModel viewModel &&
               viewModel.DismissDateRangeDialogCommand.CanExecute(null))
            {
                viewModel.DismissDateRangeDialogCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
