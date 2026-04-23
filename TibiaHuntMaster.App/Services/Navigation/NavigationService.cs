using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.App.ViewModels;

namespace TibiaHuntMaster.App.Services.Navigation
{
    /// <summary>
    ///     Implementation of navigation service for managing view transitions.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private readonly Stack<ViewModelBase> _navigationStack = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NavigationService> _logger;
        private ViewModelBase? _currentViewModel;

        public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger ?? NullLogger<NavigationService>.Instance;
        }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            private set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    if (value != null)
                    {
                        Navigated?.Invoke(value);
                    }
                }
            }
        }

        public bool CanGoBack => _navigationStack.Count > 0;

        public event Action<ViewModelBase>? Navigated;

        public async Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
        {
            await Dispatcher.UIThread.InvokeAsync(() => { NavigateTo<TViewModel>(parameter); });
        }

        public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
        {
            if (CurrentViewModel is INavigationAware currentAware)
            {
                currentAware.OnNavigatedFrom();
            }

            // Push current ViewModel to stack if it exists (for GoBack support)
            if (CurrentViewModel != null)
            {
                _navigationStack.Push(CurrentViewModel);
            }

            // Resolve ViewModel from DI container (creates NEW instance each time)
            TViewModel viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            // Initialize ViewModel with parameter if it supports INavigationAware
            if (viewModel is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(parameter);
            }

            CurrentViewModel = viewModel;

            _logger.LogDebug("Navigated to {ViewModelType}. Stack size: {StackCount}", typeof(TViewModel).Name, _navigationStack.Count);
        }

        public void GoBack()
        {
            if (!CanGoBack)
            {
                _logger.LogDebug("GoBack ignored because navigation stack is empty");
                return;
            }

            ViewModelBase previousViewModel = _navigationStack.Pop();

            // Notify current ViewModel that it's being left
            if (CurrentViewModel is INavigationAware currentAware)
            {
                currentAware.OnNavigatedFrom();
            }

            CurrentViewModel = previousViewModel;

            // Notify previous ViewModel that it's being returned to
            if (previousViewModel is INavigationAware previousAware)
            {
                previousAware.OnNavigatedTo(null);
            }

            _logger.LogDebug("Navigated back to {ViewModelType}. Stack size: {StackCount}", previousViewModel.GetType().Name, _navigationStack.Count);
        }

        /// <summary>
        ///     Clear the navigation stack (e.g., when switching characters).
        /// </summary>
        public void ClearStack()
        {
            int previousCount = _navigationStack.Count;
            _navigationStack.Clear();
            _logger.LogDebug("Cleared navigation stack. Removed {PreviousCount} entries.", previousCount);
        }
    }
}
