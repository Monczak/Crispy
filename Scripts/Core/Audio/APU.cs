using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Crispy.Scripts.Core
{
    public class APU
    {
        private SignalGenerator signal;
        private WaveOutEvent wo = new WaveOutEvent();

        public bool isPlaying;
        public double volume = 0.05;

        public void Initialize()
        {
            signal = new SignalGenerator()
            {
                Gain = 0.05,
                Frequency = 440,
                Type = SignalGeneratorType.Triangle
            };
            wo.Init(signal);
        }

        public void StartTone()
        {
            if (!isPlaying)
            {
                isPlaying = true;
                wo.Play();
            }
        }

        public void StopTone()
        {
            if (isPlaying)
            {
                isPlaying = false;
                wo.Stop();
            }
        }
    }
}
