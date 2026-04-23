namespace TibiaHuntMaster.App.Services.Navigation
{
    /// <summary>
    ///     Interface for ViewModels that need to be notified of navigation events.
    ///     Implement this to handle initialization when navigated to, or cleanup when navigated from.
    /// </summary>
    public interface INavigationAware
    {
        /// <summary>
        ///     Called when this ViewModel is navigated to.
        /// </summary>
        /// <param name="parameter">Optional navigation parameter passed from the previous view.</param>
        void OnNavigatedTo(object? parameter);

        /// <summary>
        ///     Called when this ViewModel is navigated away from.
        /// </summary>
        void OnNavigatedFrom();
    }
}