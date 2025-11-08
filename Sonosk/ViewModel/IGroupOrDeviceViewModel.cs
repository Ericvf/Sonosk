namespace Sonosk.ViewModel
{
    public interface IGroupOrDeviceViewModel
    {
        void IncreaseVolume(int delta);


        void DecreaseVolume(int delta);

        int Volume { get; set; }

        string? Name { get; }

        string BaseUri { get;}
    }
}