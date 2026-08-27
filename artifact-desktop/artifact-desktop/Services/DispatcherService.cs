using System.Windows.Threading;
using Utils.Ioc;

namespace artifact.desktop.Services
{
    [Register(ServiceType = typeof(IDispatcherService), Lifetime = Lifetime.Singleton)]
    public class DispatcherService :IDispatcherService
    {
        private readonly Dispatcher _dispatcher;

        public DispatcherService(Dispatcher dispatcher)
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
