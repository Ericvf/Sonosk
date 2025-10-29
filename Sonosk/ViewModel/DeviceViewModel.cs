using Sonosk.Sonos;

namespace Sonosk.ViewModel
{
    public class DeviceViewModel : BaseViewModel
    {
        private SingleEventTimer singleEventTimer = new SingleEventTimer();
        private readonly SonosDiscoverService sonosDiscoverService;

        public DeviceViewModel()
        {
        }

        public DeviceViewModel(SonosDiscoverService sonosDiscoverService)
        {
            this.sonosDiscoverService = sonosDiscoverService;
        }

        public SonosDevice? Device { get; set; }

        private string? name;
        public string? Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private int volume = -1;
        public int Volume
        {
            get { return volume; }
            set
            {
                if (volume != value)
                {
                    volume = value;
                    SetVolume(volume);
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        public void InitVolume(int volume)
        {
            this.volume = volume;
        }

        private void SetVolume(int volume)
        {
            if (Device != null)
            {
                singleEventTimer.Queue(100, async () =>
                {
                    await sonosDiscoverService.SetVolume(Device, volume);
                });
            }
        }
    }
}