using System;
using System.Threading.Tasks;

using TibiaHuntMaster.App.ViewModels;

namespace TibiaHuntMaster.App.Services.Navigation
{
    /// <summary>
    ///     Service for managing navigation between views in the application.
    ///     Provides type-safe navigation with parameter passing.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        ///     Check if navigation back is possible.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        ///     Get the current active ViewModel.
        /// </summary>
        ViewModelBase? CurrentViewModel { get; }

        /// <summary>
        ///     Navigate to a specific ViewModel with optional parameters.
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type to navigate to.</typeparam>
        /// <param name="parameter">Optional navigation parameter.</param>
        Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;

        /// <summary>
        ///     Navigate to a specific ViewModel synchronously (for UI event handlers).
        /// </summary>
        /// <typeparam name="TViewModel">The ViewModel type to navigate to.</typeparam>
        /// <param name="parameter">Optional navigation parameter.</param>
        void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;

        /// <summary>
        ///     Navigate back to the previous view if possible.
        /// </summary>
        void GoBack();

        /// <summary>
        ///     Event raised when navigation occurs.
        /// </summary>
        event Action<ViewModelBase>? Navigated;
    }
}