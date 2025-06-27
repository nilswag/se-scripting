using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorCloser
{
    public class Program : MyGridProgram
    {
        // Start of script
        /*
         * Automatic Door Closer Script
         * 
         * This script automatically closes doors after they have been open for a specified duration.
         * 
         * Configuration:
         * - doorPrefix: The prefix string used to identify which doors to control.
         *   The script only checks the first word of each door's name for an exact match with this prefix (case-insensitive).
         *   For example:
         *     "Door Alpha"  → matches if doorPrefix = "Door"
         *     "DoorAlpha"   → does NOT match if doorPrefix = "Door" because first word is "DoorAlpha"
         *     "Hangar Door" → does NOT match if doorPrefix = "Door"
         * 
         * - maxOpenTime: The maximum amount of time (in seconds) a door is allowed to remain open before the script closes it automatically.
         * 
         * Usage:
         * 1. Set your desired doorPrefix.
         * 2. Set maxOpenTime to your preferred timeout.
         * 3. Name your doors accordingly, making sure the first word matches doorPrefix for doors you want controlled.
         * 4. Place this script in a programmable block and run it with an Update frequency.
         */
        private readonly string doorPrefix = "Door";
        private readonly float maxOpenTime = 5f;
        // End of configuration

        private const float TICK_DURATION = 1f / 60f * 100;
        private Dictionary<long, float> doorTimer;
        private List<IMyDoor> doors;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            doorTimer = new Dictionary<long, float>();
            doors = new List<IMyDoor>();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            doors.Clear();
            GridTerminalSystem.GetBlocksOfType(doors);

            foreach (var door in doors)
            {
                if (door.OpenRatio <= 0) continue;
                if (!door.CustomName.StartsWith(doorPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (!doorTimer.ContainsKey(door.EntityId)) doorTimer[door.EntityId] = 0f;

                doorTimer[door.EntityId] += TICK_DURATION;

                if (doorTimer[door.EntityId] > maxOpenTime)
                {
                    door.CloseDoor();
                    doorTimer.Remove(door.EntityId);
                }
            }
        }
        // End of script
    }
}
