namespace Crispy.Scripts.Core
{
    public class RewindManager
    {
        public static CPUState[] ringBuffer;

        public static int bufferPos;
        private static int rewindBufferStartPos;

        private static int recordedStates = 0;

        public static void Initialize(int bufferSize)
        {
            ringBuffer = new CPUState[bufferSize];
            bufferPos = 0;
            recordedStates = 0;
        }

        public static void Record(CPUState state)
        {
            rewindBufferStartPos = -1;
            ringBuffer[bufferPos] = state;
            bufferPos++;
            recordedStates++;
            if (bufferPos >= ringBuffer.Length) bufferPos = 0;
            if (recordedStates > ringBuffer.Length) recordedStates = ringBuffer.Length;
        }

        public static CPUState Rewind()
        {
            if (rewindBufferStartPos == -1) rewindBufferStartPos = bufferPos;

            bufferPos--;
            recordedStates--;
            if (recordedStates == -1) recordedStates = 0;

            if (bufferPos == -1) bufferPos = ringBuffer.Length - 1;
            if (bufferPos == rewindBufferStartPos || recordedStates == 0) bufferPos++;
            if (bufferPos == ringBuffer.Length) bufferPos--;

            return ringBuffer[bufferPos];
        }
    }
}
