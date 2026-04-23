using System.ComponentModel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class NavigationServiceTests
    {
        private class TestViewModel : ViewModelBase
        {
        }

        private class TestNavigationAwareViewModel : ViewModelBase, INavigationAware
        {
            public object? ReceivedParameter { get; private set; }
            public int NavigatedToCount { get; private set; }
            public int NavigatedFromCount { get; private set; }

            public void OnNavigatedTo(object? parameter)
            {
                ReceivedParameter = parameter;
                NavigatedToCount++;
            }

            public void OnNavigatedFrom()
            {
                NavigatedFromCount++;
            }
        }

        private IServiceProvider CreateServiceProvider()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddTransient<TestViewModel>();
            services.AddTransient<TestNavigationAwareViewModel>();
            return services.BuildServiceProvider();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithNullCurrentViewModel()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();

            // Act
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Assert
            navigationService.CurrentViewModel.Should().BeNull();
        }

        [Fact]
        public void CanGoBack_ShouldReturnFalse_WhenNavigationStackIsEmpty()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Act
            bool canGoBack = navigationService.CanGoBack;

            // Assert
            canGoBack.Should().BeFalse();
        }

        [Fact]
        public void NavigateTo_ShouldSetCurrentViewModel()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Act
            navigationService.NavigateTo<TestViewModel>();

            // Assert
            navigationService.CurrentViewModel.Should().NotBeNull();
            navigationService.CurrentViewModel.Should().BeOfType<TestViewModel>();
        }

        [Fact]
        public void NavigateTo_ShouldCallOnNavigatedTo_WhenViewModelImplementsINavigationAware()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);
            string parameter = "test parameter";

            // Act
            navigationService.NavigateTo<TestNavigationAwareViewModel>(parameter);

            // Assert
            TestNavigationAwareViewModel? currentViewModel = navigationService.CurrentViewModel as TestNavigationAwareViewModel;
            currentViewModel.Should().NotBeNull();
            currentViewModel!.NavigatedToCount.Should().Be(1);
            currentViewModel.ReceivedParameter.Should().Be(parameter);
        }

        [Fact]
        public void NavigateTo_ShouldPushCurrentViewModelToStack()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Act
            navigationService.NavigateTo<TestViewModel>();
            ViewModelBase? firstViewModel = navigationService.CurrentViewModel;
            navigationService.NavigateTo<TestNavigationAwareViewModel>();

            // Assert
            navigationService.CanGoBack.Should().BeTrue();
            navigationService.CurrentViewModel.Should().BeOfType<TestNavigationAwareViewModel>();
        }

        [Fact]
        public void NavigateTo_ShouldCallOnNavigatedFrom_OnPreviousNavigationAwareViewModel()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Act
            navigationService.NavigateTo<TestNavigationAwareViewModel>();
            TestNavigationAwareViewModel firstViewModel = (TestNavigationAwareViewModel)navigationService.CurrentViewModel!;

            navigationService.NavigateTo<TestViewModel>();

            // Assert
            firstViewModel.NavigatedFromCount.Should().Be(1);
        }

        [Fact]
        public void GoBack_ShouldRestorePreviousViewModel()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Act
            navigationService.NavigateTo<TestViewModel>();
            ViewModelBase? firstViewModel = navigationService.CurrentViewModel;
            navigationService.NavigateTo<TestNavigationAwareViewModel>();
            navigationService.GoBack();

            // Assert
            navigationService.CurrentViewModel.Should().BeSameAs(firstViewModel);
        }

        [Fact]
        public void GoBack_ShouldDoNothing_WhenStackIsEmpty()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);
            navigationService.NavigateTo<TestViewModel>();
            ViewModelBase? currentViewModel = navigationService.CurrentViewModel;

            // Act
            navigationService.GoBack();

            // Assert
            navigationService.CurrentViewModel.Should().BeSameAs(currentViewModel);
        }

        [Fact]
        public void NavigateTo_ShouldRaiseNavigatedEvent()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);
            ViewModelBase? navigatedViewModel = null;
            int eventCount = 0;

            navigationService.Navigated += (vm) =>
            {
                navigatedViewModel = vm;
                eventCount++;
            };

            // Act
            navigationService.NavigateTo<TestViewModel>();

            // Assert
            navigatedViewModel.Should().NotBeNull();
            navigatedViewModel.Should().BeOfType<TestViewModel>();
            eventCount.Should().Be(1);
        }

        [Fact]
        public void GoBack_ShouldRaiseNavigatedEvent()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);
            navigationService.NavigateTo<TestViewModel>();
            ViewModelBase? firstViewModel = navigationService.CurrentViewModel;
            navigationService.NavigateTo<TestNavigationAwareViewModel>();

            ViewModelBase? navigatedViewModel = null;
            int eventCount = 0;
            navigationService.Navigated += (vm) =>
            {
                navigatedViewModel = vm;
                eventCount++;
            };

            // Act
            navigationService.GoBack();

            // Assert
            navigatedViewModel.Should().BeSameAs(firstViewModel);
            eventCount.Should().Be(1);
        }

        [Fact]
        public void NavigationStack_ShouldMaintainCorrectOrder()
        {
            // Arrange
            IServiceProvider serviceProvider = CreateServiceProvider();
            NavigationService navigationService = new NavigationService(serviceProvider);

            // Act
            navigationService.NavigateTo<TestViewModel>();
            ViewModelBase? first = navigationService.CurrentViewModel;

            navigationService.NavigateTo<TestNavigationAwareViewModel>();
            ViewModelBase? second = navigationService.CurrentViewModel;

            navigationService.NavigateTo<TestViewModel>();
            ViewModelBase? third = navigationService.CurrentViewModel;

            // Navigate back twice
            navigationService.GoBack();
            navigationService.GoBack();

            // Assert
            navigationService.CurrentViewModel.Should().BeSameAs(first);
        }
    }
}
