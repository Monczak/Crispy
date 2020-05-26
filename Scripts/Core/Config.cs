namespace Crispy.Scripts.Core
{
    public class Config
    {
        public uint CyclesPerSecond { get; set; }
        public uint TimerUpdatesPerSecond { get; set; }
        public uint RewindFrequency { get; set; }

        public int RewindBufferSize { get; set; }

        public int SavestateSlots { get; set; }

        public Config()
        {
            CyclesPerSecond = 500;
            TimerUpdatesPerSecond = 60;
            RewindFrequency = 60;
            RewindBufferSize = 600;

            SavestateSlots = 6;
        }
    }
}
