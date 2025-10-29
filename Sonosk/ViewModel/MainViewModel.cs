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
            await DiscoveryAndUpdate();
        }

        #endregion
        public async Task Initialize()
        {
            await DiscoveryAndUpdate();
        }

        public async Task DiscoveryAndUpdate()
        {
            if (!isLoading)
            {
                IsLoading = true;

                var deviceDiscovery = sonosDiscoverService.Discover(CancellationToken.None);
                var devices = new List<SonosDevice>();
                await foreach (var device in deviceDiscovery)
                {
                    devices.Add(device);
                }

                var coordinatorDevice = devices.FirstOrDefault(d =>
                    d.Services.Any(s => s.ServiceId == "urn:upnp-org:serviceId:ZoneGroupTopology"));

                if (coordinatorDevice != null)
                {
                    var zone = await sonosDiscoverService.GetZone(coordinatorDevice);
                    foreach (var group in zone.ZoneGroups.OrderBy(z => z.ID))
                    {
                        var groupName = string.Join(", ", group.ZoneGroupMember.Select(m => m.ZoneName).ToArray());

                        var existingGroup = Groups.SingleOrDefault(g => g.Id == group.ID);
                        if (existingGroup != null)
                        {
                            existingGroup.GroupName = groupName;
                            existingGroup.Coordinator = devices.FirstOrDefault(d => d.UDN.Substring(5) == group.Coordinator);

                            foreach (var groupMember in group.ZoneGroupMember)
                            {
                                var existingDevice = existingGroup.Devices.SingleOrDefault(d => d.Name == groupMember.ZoneName);
                                if (existingDevice == null)
                                {
                                    // TODO: Refactor to avoid code duplication
                                    var memberViewModel = viewModelFactory.Create<DeviceViewModel>();
                                    memberViewModel.Name = groupMember.ZoneName;
                                    memberViewModel.Device = devices.FirstOrDefault(d => d.UDN.Substring(5) == groupMember.UUID);
                                    if (memberViewModel.Device != null)
                                    {
                                        var volume = await sonosDiscoverService.GetVolume(memberViewModel.Device);
                                        memberViewModel.InitVolume(volume);
                                    }
                                    existingGroup.Devices.Add(memberViewModel);
                                }
                                else
                                {
                                    existingDevice.Name = groupMember.ZoneName;
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
                            groupViewModel.Coordinator = devices.FirstOrDefault(d => d.UDN.Substring(5) == group.Coordinator);

                            if (groupViewModel.Coordinator != null)
                            {
                                var volume = await sonosDiscoverService.GetGroupVolume(groupViewModel.Coordinator);
                                groupViewModel.InitVolume(volume);
                            }

                            foreach (var groupMember in group.ZoneGroupMember)
                            {
                                var memberViewModel = viewModelFactory.Create<DeviceViewModel>();
                                memberViewModel.Name = groupMember.ZoneName;

                                memberViewModel.Device = devices.FirstOrDefault(d => d.UDN.Substring(5) == groupMember.UUID);
                                if (memberViewModel.Device != null)
                                {
                                    var volume = await sonosDiscoverService.GetVolume(memberViewModel.Device);
                                    memberViewModel.InitVolume(volume);
                                }

                                groupViewModel.Devices.Add(memberViewModel);
                            }

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

                IsLoading = false;
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