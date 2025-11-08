using Sonosk.Sonos;
using Sonosk.Wpf;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Sonosk.ViewModel
{

    public class GroupViewModel : BaseViewModel, IGroupOrDeviceViewModel
    {
        private readonly SingleEventTimer singleEventTimer;

        public ObservableCollection<DeviceViewModel> Devices { get; } = new ObservableCollection<DeviceViewModel>();

        //public GroupViewModel()
        //{

        //}

        public GroupViewModel(SingleEventTimer singleEventTimer)
        {
            this.singleEventTimer = singleEventTimer;
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
                    OnPropertyChanged(nameof(Volume));
                    SetVolume(volume);
                }
            }
        }

        public string Name => GroupName ?? "Unknown";

        public string BaseUri => Devices.FirstOrDefault()?.Device?.BaseUri ?? string.Empty;

        //     public SonosDevice? Coordinator { get; set; }

        public override string ToString()
        {
            return $"{GroupName}";
        }

        private void SetVolume(int volume)
        {
          //  if (Coordinator != null)
            {
                singleEventTimer.Queue(100, () =>
                {
                    //await sonosDiscoverService.SetGroupVolume(Coordinator, volume);

                    var devices = Devices.ToArray();

                    double avg = devices.Average(d => d.Volume);
                    if (avg == 0)
                    {
                        foreach (var d in devices)
                            d.SetVolumeFromGroup(volume);
                    }
                    else
                    {
                        double scale = volume / avg;

                        foreach (var d in devices)
                        {
                            int newVol = (int)Math.Round(d.Volume * scale);
                            newVol = Math.Clamp(newVol, 0, 100);
                            d.SetVolumeFromGroup(newVol);
                        }
                    }
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

        public void SetVolumeUI(int volume)
        {
            this.volume = volume;
            OnPropertyChanged(nameof(Volume));
        }

        public void CalculateVolumeUI()
        {
            var devices = Devices.ToArray();
            if (devices.Length == 0)
            {
                Volume = 0;
                return;
            }

            double avg = devices.Average(d => d.Volume);
            var volume = Math.Clamp((int)Math.Round(avg), 0, 100);
            SetVolumeUI(volume);
        }

        public ICommand MouseScrollCommand => new RelayCommand<int>(delta =>
        {
            if (delta > 0) IncreaseVolume(1);
            else if (delta < 0) DecreaseVolume(1);
        });
    }
}