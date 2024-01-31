using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineers;

partial class Program : MyGridProgram
{

    enum State
    {
        Increase,
        Decrease,
    }

    IMyMotorAdvancedStator stator;
    State state = State.Increase;
    float targetVelocity = 10;
    float accel = 60;
    float angleThreshold = 1;

    float secondsPerUpdate;

    public Program()
    {
        stator = GridTerminalSystem.GetBlockWithName("test rotor") as IMyMotorAdvancedStator;

        if (stator == null)
        {
            Echo("no stator");
            return;
        }

        Runtime.UpdateFrequency = UpdateFrequency.Update10;

        float ticksPerSecond = 60;

        secondsPerUpdate = 10 / ticksPerSecond;
    }

    public void Main(string argument, UpdateType updateType)
    {

        if (argument != "")
        {
            Echo($"current angle: {stator.Angle}");

            float velocity;
            if (!float.TryParse(argument, out velocity))
            {
                Echo($"{argument} is not a float");
                targetVelocity = 0;
            }
            else
            {
                if (stator.TargetVelocityRPM != velocity)
                {
                    Echo($"setting velocity to {velocity}");
                    targetVelocity = velocity;
                    state = velocity < 0 ? State.Decrease : State.Increase;

                }
            }
        }

        if (state == State.Increase && stator.Angle + angleThreshold >= stator.UpperLimitRad)
        {
            state = State.Decrease;
            targetVelocity = -targetVelocity;
        }
        else if (state == State.Decrease && stator.Angle - angleThreshold <= stator.LowerLimitRad)
        {
            state = State.Increase;
            targetVelocity = -targetVelocity;
        }

        if (updateType == UpdateType.Update10)
        {
            if (stator.TargetVelocityRPM != targetVelocity)
            {
                switch (state)
                {
                    case State.Increase:
                        stator.TargetVelocityRPM += accel * secondsPerUpdate;
                        break;
                    case State.Decrease:
                        stator.TargetVelocityRPM -= accel * secondsPerUpdate;
                        break;
                }
            }
            else
            {
                Echo($"At target speed: {stator.TargetVelocityRPM}");
            }

            if (Math.Abs(stator.TargetVelocityRPM) > Math.Abs(targetVelocity) + 0.05)
            {
                Echo("Correcting velocity");
                stator.TargetVelocityRPM = targetVelocity;
            }
        }
    }
}
