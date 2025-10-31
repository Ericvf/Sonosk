using Sonosk.Sonos;
using Sonosk.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Sonosk.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private readonly SonosDiscoverService sonosDiscoverService;
        private readonly ViewModelFactory viewModelFactory;

        public event Action? LoadingStarted;
        public event Action? LoadingEnded;

        private bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                if (isLoading != value)
                {
                    isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));

                    if (isLoading)
                    {
                        LoadingStarted?.Invoke();
                    }
                    else
                    {
                        LoadingEnded?.Invoke();
                    }
                }
            }
        }

        public ObservableCollection<GroupViewModel> Groups { get; } = new ObservableCollection<GroupViewModel>();

        public MainViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Groups = new ObservableCollection<GroupViewModel>
                {
                    new GroupViewModel
                    {
                        GroupName = "Living Room",
                        Devices =
                        {
                             new DeviceViewModel { Name = "Living Room" , Volume=20 },
                        },
                    },
                    new GroupViewModel
                    {
                        GroupName = "Office, Kitchen",
                        Devices =
                        {
                             new DeviceViewModel { Name = "Office" , Volume=20 },
                             new DeviceViewModel { Name = "Kitchen" , Volume=15 },
                        },
                    },
                };
            }
        }

        public MainViewModel(SonosDiscoverService sonosDiscoverService, ViewModelFactory viewModelFactory)
        {
            this.sonosDiscoverService = sonosDiscoverService;
            this.viewModelFactory = viewModelFactory;
        }

        #region Commands

        private ICommand refreshCommand;

        public ICommand RefreshCommand
        {
            get
            {
                if (refreshCommand == null)
                {
                    refreshCommand = new RelayCommand(RefreshHandler);
                }

                return refreshCommand;
            }
        }

        private async void RefreshHandler(object obj)
        {
            await Refresh(5);
        }

        #endregion

        public async Task Refresh(int timeout = 5)
        {
            await DiscoveryAndUpdate(timeout);
        }

        private async Task DiscoveryAndUpdate(int timeout = 5)
        {
            if (!isLoading)
            {
                IsLoading = true;

                await UpdateAllVolumes();

                var deviceDiscovery = sonosDiscoverService.Discover(timeout, CancellationToken.None);
                var devices = new List<SonosDevice>();
                var hasZoneTopology = false;

                await foreach (var device in deviceDiscovery)
                {
                    if (!hasZoneTopology)
                    {
                        var isCoordinator = device.Services.Any(s => s.ServiceId == "urn:upnp-org:serviceId:ZoneGroupTopology");
                        if (isCoordinator)
                        {
                            await UpdateZone(device);
                            hasZoneTopology = true;
                        }
                    }

                    devices.Add(device);
                }

                foreach (var groupViewModel in Groups)
                {
                    foreach (var deviceViewModel in groupViewModel.Devices)
                    {
                        if (deviceViewModel.Device == null && deviceViewModel.DeviceId != null)
                        {
                            deviceViewModel.Device = devices.FirstOrDefault(d => d.UDN.Substring(5) == deviceViewModel.DeviceId);
                        }
                    }
                }

                await UpdateAllVolumes();

                IsLoading = false;
            }
        }

        private async Task UpdateZone(SonosDevice device)
        {
            var zone = await sonosDiscoverService.GetZone(device);
            foreach (var group in zone.ZoneGroups.OrderBy(z => z.ID))
            {
                var groupName = string.Join(", ", group.ZoneGroupMember.Select(m => m.ZoneName).OrderBy(_ => _).ToArray());

                var existingGroup = Groups.SingleOrDefault(g => g.Id == group.ID);
                if (existingGroup != null)
                {
                    existingGroup.GroupName = groupName;

                    foreach (var zoneGroupDevice in group.ZoneGroupMember)
                    {
                        var existingDevice = existingGroup.Devices.SingleOrDefault(d => d.DeviceId == zoneGroupDevice.UUID);
                        if (existingDevice == null)
                        {
                            var memberViewModel = viewModelFactory.Create<DeviceViewModel>();
                            memberViewModel.Name = zoneGroupDevice.ZoneName;
                            memberViewModel.DeviceId = zoneGroupDevice.UUID;
                            memberViewModel.Group = existingGroup;

                            var volume = await GetZoneGroupMemberVolume(zoneGroupDevice);
                            memberViewModel.SetVolumeUI(volume);

                            existingGroup.Devices.Add(memberViewModel);
                        }
                        else
                        {
                            existingDevice.Name = zoneGroupDevice.ZoneName;
                            existingDevice.Group = existingGroup;
                        }
                    }

                    var membersToRemove = existingGroup.Devices
                        .Where(d => !group.ZoneGroupMember.Any(m => m.ZoneName == d.Name))
                        .ToArray();

                    foreach (var memberToRemove in membersToRemove)
                    {
                        existingGroup.Devices.Remove(memberToRemove);
                    }
                }
                else
                {
                    var groupViewModel = viewModelFactory.Create<GroupViewModel>();
                    groupViewModel.Id = group.ID;
                    groupViewModel.GroupName = groupName;

                    foreach (var zoneGroupDevice in group.ZoneGroupMember)
                    {
                        var memberViewModel = viewModelFactory.Create<DeviceViewModel>();
                        memberViewModel.Name = zoneGroupDevice.ZoneName;
                        memberViewModel.DeviceId = zoneGroupDevice.UUID;
                        memberViewModel.Group = groupViewModel;

                        var volume = await GetZoneGroupMemberVolume(zoneGroupDevice);
                        memberViewModel.SetVolumeUI(volume);

                        groupViewModel.Devices.Add(memberViewModel);
                    }

                    groupViewModel.CalculateVolumeUI();
                    Groups.Add(groupViewModel);
                }
            }

            var groupsToRemove = Groups
                .Where(g => !zone.ZoneGroups.Any(z => z.ID == g.Id))
                .ToArray();

            foreach (var groupToRemove in groupsToRemove)
            {
                Groups.Remove(groupToRemove);
            }
        }

        private async Task<int> GetZoneGroupMemberVolume(ZoneGroupStateZoneGroupZoneGroupMember groupMember)
        {
            var uri = new Uri(groupMember.Location);
            var device2 = new SonosDevice()
            {
                BaseUri = $"{uri.Scheme}://{uri.Host}:{uri.Port}",
            };

            var volume = await sonosDiscoverService.GetVolume(device2);
            return volume;
        }

        private async Task UpdateAllVolumes()
        {
            var deviceTasks = Groups
                .SelectMany(g => g.Devices)
                .Where(d => d.Device != null)
                .Select(async d =>
                {
                    var volume = await sonosDiscoverService.GetVolume(d.Device);
                    d.SetVolumeUI(volume);
                });

            await Task.WhenAll(deviceTasks);

            foreach (var group in Groups)
            {
                group.CalculateVolumeUI();
            }
        }

        public void IncreaseVolume(int v)
        {
            Groups.Where(g => g.GroupName.Contains("Office")).SingleOrDefault()?.IncreaseVolume(v);
        }

        public void DecreaseVolume(int v)
        {

            Groups.Where(g => g.GroupName.Contains("Office")).SingleOrDefault()?.DecreaseVolume(v);

        }
    }
}