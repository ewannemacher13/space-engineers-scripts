// See https://aka.ms/new-console-template for more information
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

Console.WriteLine("Hello, World!");

string groupName = "";

// --------------------
List<IMyMotorAdvancedRotor> rotors;

IMyProgrammableBlock self;
IMyCubeGrid myCubeGrid;

//Public Program
{
    findSelf();
    initBlocks();

    rotors.First().


    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

void findSelf()
{
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks, x => ((IMyProgrammableBlock)x).IsRunning);
    if (blocks.Count != 1)
    {
        throw new Exception("Number of running programmable blocks was not 1!");
    }
    self = blocks.First() as IMyProgrammableBlock;
    myCubeGrid = self.CubeGrid;
}

void initBlocks()
{
    var blockGroup = GridTerminalSystem.GetBlockGroupWithName(groupName);
    if (blockGroup != null)
    {
        rotors = new();
        List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        blockGroup.GetBlocks(blocks);
        foreach (var block in blocks)
        {
            var rotor = block as IMyMotorAdvancedRotor;
            if (rotor!= null)
            {
                rotors.Add(rotor);
            }
        }
        Echo($"Added {rotors.Count} rotors.");
    }
}
