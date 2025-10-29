using AnimationExtensions;
using Sonosk.ViewModel;
using System.Windows;

namespace Sonosk
{

    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        private bool isVisiblityToggle = true;
        private Animation showAnimation, hideAnimation;

        public MainWindow(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
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
            Activated += (s, e) => this.IsActivated();
            Deactivated += (s, e) => this.IsDeactivated();
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
                hideAnimation?.Stop();
                showAnimation?.Stop();
                showAnimation = LayoutRoot
                    .Fade(0, 300, Eq.OutSine)
                    .Move(0, 100, 200, Eq.InBack)
                    .ThenDo(d => Hide())
                .Play();
            }
        }

        private void IsActivated()
        {
            if (isVisiblityToggle)
            {
                isVisiblityToggle = false;
                Show();
                Activate();

                showAnimation?.Stop();
                hideAnimation?.Stop();

                hideAnimation = LayoutRoot.Fade(0).Fade(1, 200, Eq.OutSine)
                    .Move(0, 100)
                    .Move(0, 0, 200, Eq.OutBack)
                    .Play();

                mainViewModel.Initialize();
            }
        }
    }
}