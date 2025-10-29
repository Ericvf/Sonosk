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


    //public class TrayIcon
    //{
    //    private readonly MainViewModel mainViewModel;
    //    public event EventHandler<EventArgs> Clicked;
    //    public event EventHandler<EventArgs> RightClicked;

    //    public TrayIcon(MainViewModel mainViewModel)
    //    {
    //        this.mainViewModel = mainViewModel;
    //    }

    //    public void Show(IntPtr handle)
    //    {
    //        TrayIconHelper.AddTrayIcon(handle, "QuickSonos - appbyfex");
    //        HwndSource source = HwndSource.FromHwnd(handle);
    //        source.AddHook(WndProc);
    //    }


    //    private const int WM_LBUTTONDOWN = 0x0201;
    //    private const int WM_RBUTTONDOWN = 0x0204;
    //    private const int WM_MOUSEWHEEL = 0x020A;

    //    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    //    {
    //        if (msg == TrayIconHelper.WM_USER)
    //        {
    //            switch ((int)lParam)
    //            {
    //                case WM_LBUTTONDOWN:
    //                    //IconClicked?.Invoke(this, new ButtonEventArgs(false));
    //                    handled = true;
    //                    break;
    //                case WM_RBUTTONDOWN:
    //                    //Debug.WiteLine("right click");
    //                    //IconClicked?.Invoke(this, new ButtonEventArgs(true));
    //                    handled = true;
    //                    break;
    //                case WM_MOUSEWHEEL:
    //                    int delta = (short)((wParam.ToInt32() >> 16) & 0xFFFF);
    //                    if (delta > 0)
    //                    {
    //                        mainViewModel.IncreaseVolume(delta);
    //                    }
    //                    else if (delta < 0)
    //                    {
    //                        mainViewModel.DecreaseVolume(delta * -1);
    //                    }
    //                    handled = true;
    //                    break;
    //            }

    //            handled = true;
    //            return 1;
    //        }

    //        return IntPtr.Zero;
    //    }
    //}
}
