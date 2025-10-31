using Sonosk.Sonos;

namespace Sonosk.ViewModel
{
    public class DeviceViewModel : BaseViewModel
    {
        private readonly SingleEventTimer singleEventTimer;
        private readonly SonosDiscoverService sonosDiscoverService;

        public DeviceViewModel()
        {
        }

        public DeviceViewModel(SonosDiscoverService sonosDiscoverService, SingleEventTimer singleEventTimer)
        {
            this.sonosDiscoverService = sonosDiscoverService;
            this.singleEventTimer = singleEventTimer;
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
                    OnPropertyChanged(nameof(Volume));
                    SetVolume(volume, true);
                }
            }
        }

        public GroupViewModel Group { get; set; }

        public string DeviceId { get; set; }

        public void SetVolumeUI(int volume)
        {
            this.volume = volume;
            OnPropertyChanged(nameof(Volume));
        }

        public void SetVolumeFromGroup(int volume)
        {
            this.volume = volume;
            OnPropertyChanged(nameof(Volume));
            SetVolume(volume, false);
        }

        public async void SetVolume(int volume, bool calculateGroup = false)
        {
            if (Device != null)
            {
                singleEventTimer.Queue(100, async () =>
                {
                    await sonosDiscoverService.SetVolume(Device, volume);
                    if (Group != null && calculateGroup)
                    {
                        Group.CalculateVolumeUI();
                    }
                });
            }
        }
    }
}