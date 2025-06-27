using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace AirlockScript
{
    public partial class Program : MyGridProgram
    {
        // Start of script
        /*
         * Airlock control script with configurable naming convention
         * 
         * This script automatically detects and controls one or multiple airlocks on your ship or station.
         * It groups blocks by <AirlockPrefix> + <Number>, assigns doors to inner or outer lists, vents and lights are assigned accordingly.
         * It locks doors to prevent inner and outer doors opening simultaneously, and changes light color based on vent pressurization status.
         * 
         * 
         * Naming convention (configurable and case-insensitive):
         * 
         * Blocks are grouped based on names using this pattern:
         *      <AirlockPrefix> <Identifier> <ComponentType> [<DoorSide>]
         * Where:     
         * - <AirlockPrefix> : Configurable prefix set by the user (e.g. "Airlock", "AL", "Zone") (case-insensitive)
         * - <Identifier> : A custom identifier for each door (e.g. "1", "2", "Main"). (case-insensitive)
         *              The exact naming does not matter for the script (do make sure each identifier is unique, otherwise airlocks will be grouped which might not be intentional).
         * - <ComponentType> : "Door", "Vent" or "Light" (case-insensitive).
         * - <DoorSide> : (Doors only) "Inner" or "Outer" (case-insensitive).
         * 
         * Examples:
         * - Airlock 1 Door Inner
         * - Airlock 1 Door Outer
         * - Airlock 1 Vent
         * - Airlock 1 Light
         * 
         * - AL 2 Door Inner
         * - AL 2 Door Outer
         * - AL 2 Vent
         * - AL 2 Light
         * 
         * - AL SomeIdentifier Door Inner
         * ...
         * 
         * 
         * Usage:
         * 1. Set your desired airlock prefix below in `airlockPrefix`.
         * 2. Name your doors, vents, and lights following the pattern mentioned above.
         * 3. Put this script in a programmable block on your grid.
         * 4. The script updates automatically every 10 ticks (~166 ms).
         * 
         */
        private readonly string airlockPrefix = "Airlock";
        // End of configuration

        private readonly List<Airlock> airlocks;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            var doors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(doors);
            var vents = new List<IMyAirVent>();
            GridTerminalSystem.GetBlocksOfType(vents);
            var lights = new List<IMyLightingBlock>();
            GridTerminalSystem.GetBlocksOfType(lights);

            var dict = new Dictionary<string, Airlock>();

            // Group doors
            foreach (var door in doors)
            {
                string[] tokens = door.CustomName.Split(' ');
                if (tokens.Length < 4) continue;
                if (!tokens[0].Equals(airlockPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                string identifier = tokens[1];
                string side = tokens[3];
                string key = $"{tokens[0]} {identifier}";

                if (!dict.ContainsKey(key))
                    dict[key] = new Airlock(key);
                
                if (side.Equals("Inner", StringComparison.OrdinalIgnoreCase)) dict[key].InnerDoors.Add(door);
                if (side.Equals("Outer", StringComparison.OrdinalIgnoreCase)) dict[key].OuterDoors.Add(door);
            }

            // Group vents
            foreach (var vent in vents)
            {
                string[] tokens = vent.CustomName.Split(' ');
                if (tokens.Length < 3) continue;
                if (!tokens[0].Equals(airlockPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                string identifier = tokens[1];
                string key = $"{tokens[0]} {identifier}";

                if (!dict.ContainsKey(key))
                    dict[key] = new Airlock(key);

                dict[key].Vent = vent;
            }

            // Group lights
            foreach (var light in lights)
            {
                string[] tokens = light.CustomName.Split(' ');
                if (tokens.Length < 3) continue;
                if (!tokens[0].Equals(airlockPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                string identifier = tokens[1];
                string key = $"{tokens[0]} {identifier}";

                if (!dict.ContainsKey(key))
                    dict[key] = new Airlock(key);

                dict[key].Lights.Add(light);
            }

            airlocks = dict.Values.ToList();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            airlocks.ForEach(al => al.Update());
        }

        class Airlock
        {
            public string Name;
            public List<IMyDoor> InnerDoors, OuterDoors;
            public IMyAirVent Vent;
            public List<IMyLightingBlock> Lights;

            public Airlock(string name)
            {
                Name = name;
                InnerDoors = new List<IMyDoor>();
                OuterDoors = new List<IMyDoor>();
                Lights = new List<IMyLightingBlock>();
            }

            public void Update()
            {
                bool innerOpen = InnerDoors.Any(d => d.Status == DoorStatus.Open || d.Status == DoorStatus.Opening);
                bool outerOpen = OuterDoors.Any(d => d.Status == DoorStatus.Open || d.Status == DoorStatus.Opening);

                if (Vent != null)
                {
                    Vent.Depressurize = !innerOpen;
                    float o2 = Vent.GetOxygenLevel();

                    InnerDoors.ForEach(d => d.Enabled = !outerOpen);
                    OuterDoors.ForEach(d => d.Enabled = !innerOpen && o2 < 0.05f);

                    var color = Vent.Status == VentStatus.Pressurized ? Color.Green : Color.Red;
                    Lights.ForEach(l => l.Color = color);
                }
                else
                {
                    InnerDoors.ForEach(d => d.Enabled = !outerOpen);
                    OuterDoors.ForEach(d => d.Enabled = !innerOpen);
                }
            }
        };
        // End of script
    }
}
