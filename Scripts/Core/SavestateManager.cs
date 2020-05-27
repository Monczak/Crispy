using System.IO;

namespace Crispy.Scripts.Core
{
    public class SavestateManager
    {
        public static Savestate[] savestates;
        public static int selectedSlot;
        public static string romName;

        private static readonly string savestatePath = "Savestates";

        public static void Initialize(int savestateSlots)
        {
            savestates = new Savestate[savestateSlots];

            for (int i = 0; i < savestateSlots; i++)
            {
                savestates[i] = new Savestate();
            }

            selectedSlot = 0;
        }

        public static CPUState GetSelectedState()
        {
            return savestates[selectedSlot].state;
        }

        public static void SelectNextSlot()
        {
            selectedSlot++;
            if (selectedSlot == savestates.Length)
                selectedSlot = 0;
        }

        public static void SelectPreviousSlot()
        {
            selectedSlot--;
            if (selectedSlot == -1)
                selectedSlot = savestates.Length - 1;
        }

        public static bool IsSelectedSlotEmpty()
        {
            return !File.Exists(GetSavestatePath(selectedSlot));
        }

        private static string GetSavestatePath(int slot)
        {
            return $"{savestatePath}/{romName}_{slot.ToString()}.sav";
        }

        public static void Savestate(int slot, CPUState state)
        {
            savestates[slot].state = state;

            savestates[slot].Serialize(GetSavestatePath(slot));
        }

        public static void LoadSavestate(int slot)
        {
            if (!savestates[slot].Deserialize(GetSavestatePath(slot)))
            {
                throw new SlotIsEmptyException();
            }
        }
    }
}
