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
        private Animation hideAnimation, showAnimation;
        private bool isVisibilityToggle = true;

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
            mainViewModel.HideForm += () => Dispatcher.BeginInvoke(() => IsDeactivated());

            this.Loaded += Window_Loaded;
            Activated += (s, e) => IsActivated();
            Deactivated += (s, e) => IsDeactivated();
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
            //if (!isVisibilityToggle)
            {
                showAnimation?.Stop();
                hideAnimation?.Stop();
                hideAnimation = LayoutRoot
                    .Fade(0, 300, Eq.OutSine)
                    .Move(0, 100, 200, Eq.InBack)
                    .ThenDo(d =>
                    {
                        //isVisibilityToggle = true;
                        mainViewModel.IsActivated = false;
                        Hide();
                    })
                .Play();
            }
        }

        private void IsActivated()
        {

            //if (isVisibilityToggle)
            {
                PositionWindowBottomRight();

                //isVisibilityToggle = false;
                mainViewModel.IsActivated = true;
                hideAnimation?.Stop();
                showAnimation?.Stop();
                singleEventTimer.Cancel();
                showAnimation = LayoutRoot.Fade(0).Fade(1, 200, Eq.OutSine)
                    .Move(0, 100)
                    .Move(0, 0, 200, Eq.OutBack)
                    .Play();

                if (!mainViewModel.IsSmallView)
                {
                    mainViewModel.Refresh(2);
                }
            }
        }

    }
}