using AnimationExtensions;
using Sonosk.ViewModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Sonosk
{

    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        private readonly SingleEventTimer singleEventTimer;
        private bool isVisiblityToggle = true;
        private Animation hideAnimation, showAnimation;

        public MainWindow(MainViewModel mainViewModel, SingleEventTimer singleEventTimer)
        {
            this.mainViewModel = mainViewModel;
            this.singleEventTimer = singleEventTimer;
            DataContext = mainViewModel;

            InitializeComponent();
            LayoutRoot.Hide();
            refresh.Hide();

            var loadingStarted = refresh
                .Fade(0).Fade(1, 500)
                .Rotate(0).Rotate(270, 1500, Eq.OutCubic);

            var loadingEnded = refresh.Fade(1).Fade(0, 500);
            mainViewModel.LoadingStarted += () => Dispatcher.BeginInvoke(() => loadingStarted.Play());
            mainViewModel.LoadingEnded += () => Dispatcher.BeginInvoke(() => loadingEnded.Play());

            this.Loaded += Window_Loaded;
            Activated += (s, e) => IsActivated();
            Deactivated += (s, e) => IsDeactivated();
        }

        protected async override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PositionWindowBottomRight();
        }

        private void PositionWindowBottomRight()
        {
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            Left = screenWidth - Width;
            Top = screenHeight - Height;
        }

        private void IsDeactivated()
        {
            if (!isVisiblityToggle)
            {
                isVisiblityToggle = true;
                showAnimation?.Stop();
                hideAnimation?.Stop();
                hideAnimation = LayoutRoot
                    .Fade(0, 300, Eq.OutSine)
                    .Move(0, 100, 200, Eq.InBack)
                    .ThenDo(d =>
                    {
                        Hide();
                        mainViewModel.IsActivated = false;
                    })
                .Play();
            }
        }

        private void IsActivated()
        {
            if (isVisiblityToggle)
            {
                PositionWindowBottomRight();
                Activate();

                isVisiblityToggle = false;
                hideAnimation?.Stop();
                showAnimation?.Stop();
                singleEventTimer.Cancel();
                showAnimation = LayoutRoot.Fade(0).Fade(1, 200, Eq.OutSine)
                    .Move(0, 100)
                    .Move(0, 0, 200, Eq.OutBack)
                    .ThenDo(d =>
                    {
                        mainViewModel.IsActivated = true;
                        if (mainViewModel.IsSmallView)
                        {
                            singleEventTimer.Queue(1000, () =>
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    IsDeactivated();
                                });
                                return Task.CompletedTask;
                            });
                        }
                        
                    })
                    .Play();

                if (!mainViewModel.IsSmallView)
                {
                    mainViewModel.Refresh(2);
                }
            }
        }

        public static void ActivateWindow(Window window)
        {
            var hwnd = new WindowInteropHelper(window).EnsureHandle();

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
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    }
}