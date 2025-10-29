namespace Sonosk.Sonos
{
    public class SonosDevice
    {
        public string BaseUri { get; set; }
        public string DeviceType { get; set; }
        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerURL { get; set; }
        public string ModelNumber { get; set; }
        public string ModelDescription { get; set; }
        public string ModelName { get; set; }
        public string ModelURL { get; set; }
        public string SoftwareVersion { get; set; }
        public string SwGen { get; set; }
        public string HardwareVersion { get; set; }
        public string SerialNum { get; set; }
        public string MACAddress { get; set; }
        public string UDN { get; set; }
        public string RoomName { get; set; }
        public string DisplayName { get; set; }
        public Dictionary<string, string> Versions { get; set; }
        public List<Service> Services { get; set; }
        public List<SonosDevice> SubDevices { get; set; }

        public override string ToString()
        {
            return $"{FriendlyName} ({RoomName}) - {ModelName} [{SerialNum}]";
        }

        public class Service
        {
            public string ServiceType { get; set; }
            public string ServiceId { get; set; }
            public string ControlURL { get; set; }
            public string EventSubURL { get; set; }
            public string SCPDURL { get; set; }

            public override string ToString()
            {
                return $"{ServiceType} ({ServiceId})";
            }
        }


    }
}