using Avalonia.Controls;
using Avalonia.Input;

using TibiaHuntMaster.App.ViewModels.Analyzer;

namespace TibiaHuntMaster.App.Views.Analyzer
{
    public partial class HuntAnalyzerView : UserControl
    {
        public HuntAnalyzerView()
        {
            InitializeComponent();
        }

        private void SummaryOverlayBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if(SummaryDialogCard.IsPointerOver)
            {
                return;
            }

            if(DataContext is HuntAnalyzerViewModel viewModel &&
               viewModel.CloseSummaryDialogCommand.CanExecute(null))
            {
                viewModel.CloseSummaryDialogCommand.Execute(null);
            }
        }
    }
}
