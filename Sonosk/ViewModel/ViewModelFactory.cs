using Microsoft.Extensions.DependencyInjection;

namespace Sonosk.ViewModel
{
    public class ViewModelFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public T Create<T>() where T : class
        {
            return serviceProvider.GetRequiredService<T>();
        }
    }
}
