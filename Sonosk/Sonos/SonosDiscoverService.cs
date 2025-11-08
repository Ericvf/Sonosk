using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Sonosk.Sonos
{
    public class SonosDiscoverService
    {
        private readonly IPEndPoint multicastEndPoint;
        private readonly IPEndPoint localEndPoint;
        private readonly HttpClient httpClient;

        public SonosDiscoverService(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient();
            localEndPoint = new IPEndPoint(GetLocalIPAddress(), 1901);
            multicastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
        }

        static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            var lanIP = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

            if (lanIP == null)
                throw new Exception("No LAN IPv4 address found.");

            return lanIP;
        }

        public async IAsyncEnumerable<SonosDevice> Discover(int timeout = 5, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(localEndPoint);

            string searchString =
                "M-SEARCH * HTTP/1.1\r\n" +
                "HOST:239.255.255.250:1900\r\n" +
                "MAN:\"ssdp:discover\"\r\n" +
                "MX:3\r\n" +
                "ST:urn:schemas-upnp-org:device:ZonePlayer:1\r\n\r\n";

            udpSocket.SendTo(Encoding.UTF8.GetBytes(searchString), SocketFlags.None, multicastEndPoint);

            byte[] receiveBuffer = new byte[64000];

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(timeout))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (udpSocket.Available > 0)
                {
                    int receivedBytes = udpSocket.Receive(receiveBuffer, SocketFlags.None);
                    if (receivedBytes > 0)
                    {
                        string response = Encoding.UTF8.GetString(receiveBuffer, 0, receivedBytes);

                        var headers = ParseHeaders(response);
                        if (headers.TryGetValue("LOCATION", out var location))
                        {
                            var baseUri = GetBaseUri(location);

                            var sonosDevice = await GetSonosDevice(location);
                            if (sonosDevice != null)
                            {
                                sonosDevice.BaseUri = baseUri;
                                yield return sonosDevice;
                            }
                        }
                    }
                }
                else
                {
                    await Task.Delay(50, cancellationToken);
                }
            }

            stopwatch.Stop();
        }

        private string GetBaseUri(string location)
        {
            var uri = new Uri(location);
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }

        public async Task<int> GetVolume(SonosDevice device)
        {
            string soap = @"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><u:GetVolume xmlns:u=""urn:schemas-upnp-org:service:RenderingControl:1""><InstanceID>0</InstanceID><Channel>Master</Channel></u:GetVolume></s:Body></s:Envelope>";

            var uri = $"{device.BaseUri}/MediaRenderer/RenderingControl/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:RenderingControl:1#GetVolume""");

            var response = await httpClient.PostAsync(uri, content);
            string xml = await response.Content.ReadAsStringAsync();

            XDocument doc = XDocument.Parse(xml);

            XNamespace s = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace u = "urn:schemas-upnp-org:service:RenderingControl:1";

            // Grab the value
            var currentVolume = doc.Descendants(u + "GetVolumeResponse")
                                      .Elements("CurrentVolume")
                                      .FirstOrDefault()?.Value;

            return int.TryParse(currentVolume, out int volume) ? volume : -1;
        }

        public async Task<int> GetGroupVolume(SonosDevice device)
        {
            string soap = @"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><u:GetGroupVolume xmlns:u=""urn:schemas-upnp-org:service:GroupRenderingControl:1""><InstanceID>0</InstanceID></u:GetGroupVolume></s:Body></s:Envelope>";

            var uri = $"{device.BaseUri}/MediaRenderer/GroupRenderingControl/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:GroupRenderingControl:1#GetGroupVolume""");

            var response = await httpClient.PostAsync(uri, content);
            string xml = await response.Content.ReadAsStringAsync();

            XDocument doc = XDocument.Parse(xml);

            XNamespace s = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace u = "urn:schemas-upnp-org:service:GroupRenderingControl:1";

            // Grab the value
            var currentVolume = doc.Descendants(u + "GetGroupVolumeResponse")
                                      .Elements("CurrentVolume")
                                      .FirstOrDefault()?.Value;

            return int.TryParse(currentVolume, out int volume) ? volume : -1;
        }

        public async Task<bool> SetVolume(SonosDevice device, int volume)
        {
            string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <u:SetVolume xmlns:u=""urn:schemas-upnp-org:service:RenderingControl:1"">
      <InstanceID>0</InstanceID>
      <Channel>Master</Channel>
      <DesiredVolume>{volume}</DesiredVolume>
    </u:SetVolume>
  </s:Body>
</s:Envelope>";

            var uri = $"{device.BaseUri}/MediaRenderer/RenderingControl/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:RenderingControl:1#SetVolume""");

            var response = await httpClient.PostAsync(uri, content);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SetGroupVolume(SonosDevice device, int volume)
        {
            string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><u:SetGroupVolume xmlns:u=""urn:schemas-upnp-org:service:GroupRenderingControl:1""><InstanceID>0</InstanceID><DesiredVolume>{volume}</DesiredVolume></u:SetGroupVolume></s:Body></s:Envelope>";

            var uri = $"{device.BaseUri}/MediaRenderer/GroupRenderingControl/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:GroupRenderingControl:1#SetGroupVolume""");

            var response = await httpClient.PostAsync(uri, content);

            return response.IsSuccessStatusCode;
        }

        public async Task<ZoneGroupState?> GetZone(SonosDevice device)
        {
            string soap = @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" 
                                    s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                          <s:Body>
                            <u:GetZoneGroupState xmlns:u=""urn:schemas-upnp-org:service:ZoneGroupTopology:1"" />
                          </s:Body>
                        </s:Envelope>";

            var uri = $"{device.BaseUri}/ZoneGroupTopology/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:ZoneGroupTopology:1#GetZoneGroupState""");

            var response = await httpClient.PostAsync(uri, content);
            string xml = await response.Content.ReadAsStringAsync();


            XDocument soapDoc = XDocument.Parse(xml);
            XNamespace s = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace u = "urn:schemas-upnp-org:service:ZoneGroupTopology:1";

            var zoneGroupStateEscaped = soapDoc
                .Descendants(u + "GetZoneGroupStateResponse")
                .Elements("ZoneGroupState")
                .FirstOrDefault()?.Value;

            if (zoneGroupStateEscaped != null)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ZoneGroupState));
                using (TextReader reader = new StringReader(zoneGroupStateEscaped))
                {
                    var deserialized = serializer.Deserialize(reader);
                    if (deserialized is ZoneGroupState zoneGroupState)
                        return zoneGroupState;
                }
            }

            return default;
        }

        private async Task<SonosDevice?> GetSonosDevice(string location)
        {
            var response = await httpClient.GetStringAsync(location);
            var doc = XDocument.Parse(response);
            if (doc.Root != null)
            {
                XNamespace ns = "urn:schemas-upnp-org:device-1-0";
                var rootDevice = doc.Root.Element(ns + "device");

                if (rootDevice != null)
                {
                    var device = new SonosDevice
                    {
                        DeviceType = rootDevice.Element(ns + "deviceType")?.Value,
                        FriendlyName = rootDevice.Element(ns + "friendlyName")?.Value,
                        Manufacturer = rootDevice.Element(ns + "manufacturer")?.Value,
                        ManufacturerURL = rootDevice.Element(ns + "manufacturerURL")?.Value,
                        ModelNumber = rootDevice.Element(ns + "modelNumber")?.Value,
                        ModelDescription = rootDevice.Element(ns + "modelDescription")?.Value,
                        ModelName = rootDevice.Element(ns + "modelName")?.Value,
                        ModelURL = rootDevice.Element(ns + "modelURL")?.Value,
                        SoftwareVersion = rootDevice.Element(ns + "softwareVersion")?.Value,
                        SwGen = rootDevice.Element(ns + "swGen")?.Value,
                        HardwareVersion = rootDevice.Element(ns + "hardwareVersion")?.Value,
                        SerialNum = rootDevice.Element(ns + "serialNum")?.Value,
                        MACAddress = rootDevice.Element(ns + "MACAddress")?.Value,
                        UDN = rootDevice.Element(ns + "UDN")?.Value,
                        RoomName = rootDevice.Element(ns + "roomName")?.Value,
                        DisplayName = rootDevice.Element(ns + "displayName")?.Value,
                        Versions = new Dictionary<string, string>(),
                        Services = new List<SonosDevice.Service>(),
                        SubDevices = new List<SonosDevice>()
                    };

                    var versions = rootDevice.Element(ns + "versions");
                    if (versions != null)
                    {
                        foreach (var v in versions.Elements())
                        {
                            var value = v.Element(ns + "version")?.Value;
                            if (value != null)
                            {
                                device.Versions[v.Name.LocalName] = value;
                            }
                        }
                    }

                    var serviceList = rootDevice.Element(ns + "serviceList");
                    if (serviceList != null)
                    {
                        foreach (var s in serviceList.Elements(ns + "service"))
                        {
                            device.Services.Add(new SonosDevice.Service
                            {
                                ServiceType = s.Element(ns + "serviceType")?.Value,
                                ServiceId = s.Element(ns + "serviceId")?.Value,
                                ControlURL = s.Element(ns + "controlURL")?.Value,
                                EventSubURL = s.Element(ns + "eventSubURL")?.Value,
                                SCPDURL = s.Element(ns + "SCPDURL")?.Value
                            });
                        }
                    }

                    var deviceList = rootDevice.Element(ns + "deviceList");
                    if (deviceList != null)
                    {
                        foreach (var d in deviceList.Elements(ns + "device"))
                        {
                            var subDevice = new SonosDevice
                            {
                                DeviceType = d.Element(ns + "deviceType")?.Value,
                                FriendlyName = d.Element(ns + "friendlyName")?.Value,
                                Manufacturer = d.Element(ns + "manufacturer")?.Value,
                                ManufacturerURL = d.Element(ns + "manufacturerURL")?.Value,
                                ModelNumber = d.Element(ns + "modelNumber")?.Value,
                                ModelDescription = d.Element(ns + "modelDescription")?.Value,
                                ModelName = d.Element(ns + "modelName")?.Value,
                                ModelURL = d.Element(ns + "modelURL")?.Value,
                                UDN = d.Element(ns + "UDN")?.Value,
                                Services = new List<SonosDevice.Service>()
                            };

                            var subServiceList = d.Element(ns + "serviceList");
                            if (subServiceList != null)
                            {
                                foreach (var s in subServiceList.Elements(ns + "service"))
                                {
                                    subDevice.Services.Add(new SonosDevice.Service
                                    {
                                        ServiceType = s.Element(ns + "serviceType")?.Value,
                                        ServiceId = s.Element(ns + "serviceId")?.Value,
                                        ControlURL = s.Element(ns + "controlURL")?.Value,
                                        EventSubURL = s.Element(ns + "eventSubURL")?.Value,
                                        SCPDURL = s.Element(ns + "SCPDURL")?.Value
                                    });
                                }
                            }

                            device.SubDevices.Add(subDevice);
                        }
                    }

                    return device;
                }
            }

            return null;
        }

        private Dictionary<string, string> ParseHeaders(string response)
        {
            return response
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(line =>
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    return new KeyValuePair<string, string>(
                        parts[0].Trim(),
                        parts.Length > 1 ? parts[1].Trim() : string.Empty
                    );
                })
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<string> GetCurrentTrack(string baseUri, uint instanceId = 0)
        {
            string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <u:GetPositionInfo xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1"">
      <InstanceID>{instanceId}</InstanceID>
    </u:GetPositionInfo>
  </s:Body>
</s:Envelope>";

            var uri = $"{baseUri}/MediaRenderer/AVTransport/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo""");

            var response = await httpClient.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode)
                return "Unknown";

            string xml = await response.Content.ReadAsStringAsync();
            XDocument doc = XDocument.Parse(xml);
            XNamespace u = "urn:schemas-upnp-org:service:AVTransport:1";

            var positionInfo = doc.Descendants(u + "GetPositionInfoResponse").FirstOrDefault();
            if (positionInfo == null)
                return "Unknown";

            string uriTrack = positionInfo.Element("TrackURI")?.Value ?? "";
            string metadata = positionInfo.Element("TrackMetaData")?.Value ?? "";

            if (!string.IsNullOrEmpty(metadata))
            {
                try
                {
                    var metaDoc = XDocument.Parse(metadata);
                    if (metaDoc != null)
                    {
                        XNamespace dc = "http://purl.org/dc/elements/1.1/";
                        var title = metaDoc.Descendants(dc + "title").FirstOrDefault()?.Value;
                        var artist = metaDoc.Descendants(dc + "creator").FirstOrDefault()?.Value;
                        if (!string.IsNullOrEmpty(title))
                            return !string.IsNullOrEmpty(artist) ? $"{title} — {artist}" : title;
                    }
                }
                catch { /* ignore parsing errors */ }
            }

            return uriTrack; // fallback
        }

        public async Task<MediaInfo?> GetMediaInfo(string deviceBaseUri, uint instanceId = 0)
        {
            string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/""><s:Body><u:GetMediaInfo xmlns:u=""urn:schemas-upnp-org:service:AVTransport:1""><InstanceID>{instanceId}</InstanceID></u:GetMediaInfo></s:Body></s:Envelope>";
            var uri = $"{deviceBaseUri}/MediaRenderer/AVTransport/Control";

            var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", @"""urn:schemas-upnp-org:service:AVTransport:1#GetMediaInfo""");

            var response = await httpClient.PostAsync(uri, content);
            if (!response.IsSuccessStatusCode)
                return null;

            string xml = await response.Content.ReadAsStringAsync();
            XDocument doc = XDocument.Parse(xml);

            XNamespace s = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace u = "urn:schemas-upnp-org:service:AVTransport:1";

            var mediaInfoElement = doc.Descendants(u + "GetMediaInfoResponse").FirstOrDefault();
            if (mediaInfoElement == null)
                return null;

            return new MediaInfo
            {
                NrTracks = int.TryParse(mediaInfoElement.Element("NrTracks")?.Value, out int nr) ? nr : 0,
                MediaDuration = mediaInfoElement.Element("MediaDuration")?.Value ?? "",
                CurrentURI = mediaInfoElement.Element("CurrentURI")?.Value ?? "",
                CurrentURIMetaData = mediaInfoElement.Element("CurrentURIMetaData")?.Value ?? "",
                NextURI = mediaInfoElement.Element("NextURI")?.Value ?? "",
                NextURIMetaData = mediaInfoElement.Element("NextURIMetaData")?.Value ?? "",
                PlayMedium = mediaInfoElement.Element("PlayMedium")?.Value ?? "",
                RecordMedium = mediaInfoElement.Element("RecordMedium")?.Value ?? "",
                WriteStatus = mediaInfoElement.Element("WriteStatus")?.Value ?? ""
            };
        }

        // Example class to hold the media info
        public class MediaInfo
        {
            public int? NrTracks { get; set; }
            public string? MediaDuration { get; set; }
            public string? CurrentURI { get; set; }
            public string? CurrentURIMetaData { get; set; }
            public string? NextURI { get; set; }
            public string? NextURIMetaData { get; set; }
            public string? PlayMedium { get; set; }
            public string? RecordMedium { get; set; }
            public string? WriteStatus { get; set; }
        }

    }

}
