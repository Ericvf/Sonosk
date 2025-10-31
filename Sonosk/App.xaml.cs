using Microsoft.Extensions.DependencyInjection;
using Sonosk.Sonos;
using Sonosk.TrayIcon;
using Sonosk.ViewModel;
using System.Windows;
using System.Windows.Interop;

namespace Sonosk
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            services.AddHttpClient();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<SonosDiscoverService>();
            services.AddSingleton<ViewModelFactory>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<GroupViewModel>();
            services.AddTransient<DeviceViewModel>();
            services.AddTransient<SingleEventTimer>();

            services.AddTransient<TrayIconService>();
            var serviceProvider = services.BuildServiceProvider();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            var workingArea = SystemParameters.WorkArea;
            mainWindow.Left = workingArea.Right - mainWindow.Width;
            mainWindow.Top = workingArea.Bottom - mainWindow.Height;

            var trayIcon = serviceProvider.GetRequiredService<TrayIconService>();
            trayIcon.Clicked += (_, _) => mainWindow.Activate();
            mainWindow.Activate();
            mainWindow.Show();
  
            var handle = new WindowInteropHelper(mainWindow).Handle;
            trayIcon.Show(handle);
        }
    }
}
