namespace Sonosk
{
    public class SingleEventTimer
    {
        private Timer? timer;

        public void Queue(int interval, Func<Task> action)
        {
            timer?.Dispose();
            timer = new Timer(_ => action(), null, interval, Timeout.Infinite);
        }

        public void Cancel()
        {
            timer?.Dispose();
            timer = null;
        }
    }
}