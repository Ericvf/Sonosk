using Microsoft.Extensions.DependencyInjection;
using Sonosk.Sonos;
using Sonosk.TrayIcon;
using Sonosk.ViewModel;
using System.Runtime.InteropServices;
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
            trayIcon.Show(handle);


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
                        ActivateWindow(handle);
                        mainWindow.Show();
                        mainWindow.Topmost = true;
                        mainWindow.Topmost = false;
                        mainWindow.Activate();
                    });
                }
            }

            static void ActivateWindow(nint hwnd)
            {
                var threadId1 = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
                var threadId2 = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

                if (threadId1 != threadId2)
                {
                    AttachThreadInput(threadId1, threadId2, true);
                    SetForegroundWindow(hwnd);
                    AttachThreadInput(threadId1, threadId2, false);
                }
                else
                    SetForegroundWindow(hwnd);
            }

            [DllImport("user32.dll")]
            static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

            [DllImport("user32.dll")]
            static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        }
    }
}


