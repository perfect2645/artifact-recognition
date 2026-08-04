namespace artifact.desktop.Services
{
    internal interface IDispatcherService
    {
        public interface IDispatcherService
        {
            void InvokeOnUI(Action action);
            Task InvokeOnUIAsync(Action action);
        }
    }
}
