namespace artifact.desktop.Services
{
    public interface IDispatcherService
    {
        void InvokeOnUI(Action action);
        Task InvokeOnUIAsync(Action action);
    }
}
