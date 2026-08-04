using System.Windows.Threading;

namespace artifact.desktop.Services
{
    internal class WpfDispatcherService :IDispatcherService
    {
        private readonly Dispatcher _dispatcher;

        public WpfDispatcherService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void InvokeOnUI(Action action)
        {
            if (_dispatcher.CheckAccess())
                action();
            else
                _dispatcher.Invoke(action);
        }

        public Task InvokeOnUIAsync(Action action)
        {
            if (_dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }
            return _dispatcher.InvokeAsync(action).Task;
        }
    }
}
