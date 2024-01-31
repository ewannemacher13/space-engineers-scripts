using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace SpaceEngineers;
internal class RedditExample
{
    string jumpDriveGroup = "";
    string indicatorName = "";

    // --------------------
    List<IMyJumpDrive> drives;
    IMyFunctionalBlock indicator;
    bool drivesCharged = true;

    IMyProgrammableBlock self;
    IMyCubeGrid myCubeGrid;

    //public Program()
    public RedditExample()
    {
        findSelf();
        initBlocks();
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
        var blockGroup = GridTerminalSystem.GetBlockGroupWithName(jumpDriveGroup);
        if (blockGroup != null)
        {
            drives = new List<IMyJumpDrive>();
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blockGroup.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                var drive = block as IMyJumpDrive;
                if (drive != null)
                {
                    drives.Add(drive);
                }
            }
            Echo($"Added {drives.Count} jump drives.");
        }
        if (!String.IsNullOrEmpty(indicatorName))
        {
            List<IMyTerminalBlock> indicators = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(indicatorName, indicators, x => x.CubeGrid == myCubeGrid);
            if (indicators.Count > 0)
            {
                var foundIndicator = indicators.First() as IMyFunctionalBlock;
                if (foundIndicator != null)
                {
                    indicator = foundIndicator;
                }
            }
        }
    }

    void checkDriveStatus()
    {
        bool charged = true;
        foreach (var drive in drives)
        {
            charged = charged && (drive.Status == MyJumpDriveStatus.Ready);
        }
        drivesCharged = charged;
        if (indicator != null)
        {
            indicator.Enabled = drivesCharged;
        }
    }

    public void Main(string argument, UpdateType updateSource)
    {
        checkDriveStatus();
    }
}

internal static class Runtime
{
    public static UpdateFrequency UpdateFrequency { get; set; }
}
