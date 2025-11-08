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
            var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
            var workingArea = SystemParameters.WorkArea;
            mainWindow.Left = workingArea.Right - mainWindow.Width;
            mainWindow.Top = workingArea.Bottom - mainWindow.Height;
            mainWindow.Activate();
            mainWindow.Show();

            var handle = new WindowInteropHelper(mainWindow).Handle;

            var trayIcon = serviceProvider.GetRequiredService<TrayIconService>();
            trayIcon.Clicked += TrayIcon_Clicked;
            trayIcon.MouseScroll += TrayIcon_MouseScroll;
            trayIcon.Show(handle, "Sonosk v1.0 - appbyfex");

            void TrayIcon_Clicked(object? sender, EventArgs e)
            {
                if (!mainViewModel.IsActivated || mainViewModel.IsSmallView)
                {
                    mainWindow.Activate();
                    mainWindow.Show();
                    mainViewModel.IsSmallView = false;
                }
            }

            void TrayIcon_MouseScroll(object? sender, TrayIconService.DeltaEventArgs e)
            {
                if (!mainViewModel.IsActivated && mainViewModel.SelectedViewModel != null)
                {
                    mainViewModel.IsSmallView = true;
                }

                if (e.Delta > 0) mainViewModel.IncreaseVolume(1);
                else if (e.Delta < 0) mainViewModel.DecreaseVolume(1);

                if (!mainViewModel.IsActivated && mainViewModel.SelectedViewModel != null)
                {
                    mainWindow.Dispatcher.Invoke(() =>
                    {
                        User32.ActivateWindow(handle);
                        mainWindow.Show();
                        mainWindow.Topmost = true;
                        mainWindow.Topmost = false;
                        mainWindow.Activate();
                    });
                }
            }
        }
    }
}


