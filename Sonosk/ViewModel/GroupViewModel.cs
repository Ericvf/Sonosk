using Sonosk.Sonos;
using System.Collections.ObjectModel;

namespace Sonosk.ViewModel
{
    public class GroupViewModel : BaseViewModel
    {
        private SingleEventTimer singleEventTimer = new SingleEventTimer();
        private readonly SonosDiscoverService? sonosDiscoverService;

        public ObservableCollection<DeviceViewModel> Devices { get; } = new ObservableCollection<DeviceViewModel>();

        public GroupViewModel()
        {
            
        }

        public GroupViewModel(SonosDiscoverService sonosDiscoverService)
        {
            this.sonosDiscoverService = sonosDiscoverService;
        }

        private string? groupName;
        public string? GroupName
        {
            get
            {
                return groupName;
            }
            set
            {
                if (groupName != value)
                {
                    groupName = value;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        private string? id;
        public string? Id
        {
            get
            {
                return id;
            }
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged(nameof(id));
                }
            }
        }

        private int volume = 0;
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

        public SonosDevice? Coordinator { get; set; }

        public override string ToString()
        {
            return $"{GroupName} (Coordinator: {Coordinator}, ID: {Id})";
        }

        private void SetVolume(int volume)
        {
            if (Coordinator != null)
            {
                singleEventTimer.Queue(100, async () =>
                {
                    await sonosDiscoverService.SetGroupVolume(Coordinator, volume);
                });
            }
        }

        public void IncreaseVolume(int v)
        {
            var newVolume = Math.Clamp(volume + v, 0, 100);
            Volume = newVolume;
        }

        public void DecreaseVolume(int v)
        {
            var newVolume = Math.Clamp(volume - v, 0, 100);
            Volume = newVolume;
        }

        public void InitVolume(int volume)
        {
            this.volume = volume;
        }
    }
}