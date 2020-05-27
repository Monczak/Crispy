using System;

namespace Crispy.Scripts.Core
{
    [Serializable]
    public class SlotIsEmptyException : Exception
    {
        public SlotIsEmptyException() { }
        public SlotIsEmptyException(string message) : base(message) { }
        public SlotIsEmptyException(string message, Exception inner) : base(message, inner) { }
        protected SlotIsEmptyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
