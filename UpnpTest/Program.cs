using Microsoft.Extensions.DependencyInjection;
using UpnpTest;

var serviceProvider = BuildServiceProvider();
var app = serviceProvider.GetRequiredService<App>();
await app.RunAsync();

static ServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();
    services.AddSingleton<App>();
    services.AddSingleton<SonosDiscoverService>();
    services.AddHttpClient();

    return services.BuildServiceProvider();
}

public class App
{
    private readonly SonosDiscoverService sonosDiscoverService;

    public App(SonosDiscoverService sonosDiscoverService)
    {
        this.sonosDiscoverService = sonosDiscoverService;
    }

    public async Task<int> RunAsync()
    {
        Console.WriteLine("Started Sonos.Cli v1.0.0.0");
        Console.WriteLine("Discovering devices...");

        var deviceDiscovery = sonosDiscoverService.Discover(CancellationToken.None);
        var devices = new List<SonosDevice>();
        await foreach (var device in deviceDiscovery)
        {
            Console.WriteLine($"{device.FriendlyName} {device.RoomName}");
            devices.Add(device);
        }

        Console.WriteLine();

        var office = devices.FirstOrDefault(d => d.RoomName == "Office");
        var vol = await sonosDiscoverService.GetVolume(office);

        Console.WriteLine($"Room: {office.RoomName} - Volume: {vol}");

        //await sonosDiscoverService.SetVolume(office, vol + 10);

        var zone = await sonosDiscoverService.GetZone(office);
        //var officeGroup = zone.ZoneGroups.FirstOrDefault(z => z.ZoneGroupMember.Any(m => m.UUID == office.UDN.Substring(5)));

        //var coordinator = devices.FirstOrDefault(d => d.UDN.Substring(5) == officeGroup.Coordinator);
        //var groupvol = await sonosDiscoverService.GetGroupVolume(coordinator);
        //Console.WriteLine($"Group: {coordinator.DisplayName} - Volume: {groupvol}");

        //await sonosDiscoverService.SetGroupVolume(coordinator, groupvol + 10);

        foreach (var group in zone.ZoneGroups.OrderBy(z => z.ID))
        {
            Console.WriteLine($" Zone Group: {group.ID} (Coordinator: {group.Coordinator})");
            foreach (var member in group.ZoneGroupMember.OrderBy(z => z.UUID))
            {
                Console.WriteLine($"  Member: {member.UUID} - {member.ZoneName} ({member.Location})");

                if (member.Satellite != null)
                {
                    foreach (var sat in member.Satellite)
                    {
                        Console.WriteLine($"   Satellite: {sat.UUID} - {sat.ZoneName} ({sat.Location})");
                    }
                }
            }
        }

        return 0;
    }
}
