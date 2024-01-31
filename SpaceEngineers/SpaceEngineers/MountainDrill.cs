using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using VRageRender.Voxels;

namespace SpaceEngineers.MountainDrill;

partial class Program : MyGridProgram
{

    string group = "test";

    public class OperationMode
    {
        public enum Enum { Velocity, Position, Oscilate };
        public Enum Mode = Enum.Position;
        public void SetMode(string mode)
        {
            switch (mode[0])
            {
                case 'v': Mode = Enum.Velocity; break;
                case 'p': Mode = Enum.Position; break;
                case 'o': Mode = Enum.Oscilate; break;
                default: break;
            }
        }
    }
    public abstract class MyController
    {
        public enum State { Increase, Decrease, }
        public State state = State.Increase;

        protected float accel = 0;
        public float GetAccel(UpdateType updateType)
        {
            switch (updateType)
            {
                case UpdateType.Update1: return accel / 60;
                case UpdateType.Update10: return accel * 10 / 60;
                case UpdateType.Update100: return accel * 100 / 60;
                default: return 0;
            }
        }
        protected float targetVelocity = 0;

        public abstract float GetMaxLimit();
        public abstract float GetMinLimit();
        public abstract float GetPosition();
        public abstract float GetVelocity();
        public abstract void SetVelocity(float value);

        public bool AtMax()
        {
            return GetPosition() + positionThreshold >= GetMaxLimit();
        }
        public bool AtMin()
        {
            return GetPosition() - positionThreshold <= GetMinLimit();
        }

        public void SetTargetVelocity(float value)
        {
            targetVelocity = value;
            SetVelocity(value);

            if (targetVelocity < 0) state = State.Decrease;
            else state = State.Increase;

            accel = targetVelocity * 2;
        }

        protected float targetPosition = 0;
        public float positionThreshold = 0.5f;
        public void SetTargetPosition(float value, float threshold = -1)
        {
            targetPosition = value;
            if (threshold >= 0) positionThreshold = threshold;
        }

        public OperationMode Mode = new OperationMode();
        public int GetTargetMoveDirection()
        {
            if (GetPosition() <= targetPosition - positionThreshold) return 1;
            if (GetPosition() >= targetPosition + positionThreshold) return -1;
            return 0;
        }

        public void SetParam(string param, string value)
        {
            switch (param[0])
            {
                case 'v':
                    float velocity;
                    if (float.TryParse(value, out velocity))
                        SetTargetVelocity(velocity);
                    break;
                case 'p':
                    float position;
                    if (float.TryParse(value, out position))
                        SetTargetPosition(position);
                    break;
                case 'm': Mode.SetMode(value); break;
            }
        }

        public void Tick(UpdateType updateType)
        {
            if (Mode.Mode == OperationMode.Enum.Oscilate)
            {
                if (state == State.Increase && AtMax())
                {
                    state = State.Decrease;
                    targetVelocity = -targetVelocity;
                }
                else if (state == State.Decrease && AtMin())
                {
                    state = State.Increase;
                    targetVelocity = -targetVelocity;
                }
            }

            if (Mode.Mode == OperationMode.Enum.Position)
            {
                var direction = GetTargetMoveDirection();
                SetTargetVelocity(Math.Abs(targetVelocity) * direction);
            }
            else if (GetVelocity() != targetVelocity)
            {
                switch (state)
                {
                    case State.Increase:
                        SetVelocity(GetVelocity() + GetAccel(updateType));
                        break;
                    case State.Decrease:
                        SetVelocity(GetVelocity() - GetAccel(updateType));
                        break;
                }
            }

            if (Math.Abs(GetVelocity()) > Math.Abs(targetVelocity) + 0.05)
                SetVelocity(targetVelocity);
        }
    }
    public class MyRotor : MyController
    {
        public IMyMotorAdvancedStator Rotor;
        Program program;
        public new void SetTargetVelocity(float value)
        {
            base.SetTargetVelocity(value);
            Rotor.TargetVelocityRPM = targetVelocity;
        }

        public bool IsNull = false;

        public MyRotor(Program _program, IMyMotorAdvancedStator _rotor)
        {
            if (_rotor == null) IsNull = true;

            Rotor = _rotor;
            program = _program;
        }

        public override float GetMaxLimit() => Rotor.UpperLimitRad;
        public override float GetMinLimit() => Rotor.LowerLimitRad;
        public override float GetPosition() => Rotor.Angle;
        public override float GetVelocity() => Rotor.TargetVelocityRPM;
        public override void SetVelocity(float value) => Rotor.TargetVelocityRPM = value;
    }

    public class MyPiston : MyController
    {
        public IMyPistonBase Piston;
        Program program;

        public bool IsNull = false;

        public MyPiston(Program _program, IMyPistonBase _piston)
        {
            if (_piston == null) IsNull = true;

            Piston = _piston;
            program = _program;
        }
        public new void SetTargetVelocity(float value)
        {
            base.SetTargetVelocity(value);
            Piston.Velocity = targetVelocity;
        }
        public override float GetMaxLimit() => Piston.MaxLimit;
        public override float GetMinLimit() => Piston.MinLimit;
        public override float GetPosition() => Piston.CurrentPosition;
        public override float GetVelocity() => Piston.Velocity;
        public override void SetVelocity(float value) => Piston.Velocity = value;
    }

    public class MyPistonLine
    {
        public List<MyPiston> Pistons;
        Program program;

        public MyPistonLine(Program _program, List<MyPiston> _pistons)
        {
            Pistons = _pistons;
            program = _program;
        }
        public OperationMode mode = new OperationMode();

        public void Tick(UpdateType updateType)
        {

        }
    }

    MyRotor rotorZ;
    List<MyPiston> pistonsZ = new List<MyPiston>();
    List<MyPiston> pistonsP = new List<MyPiston>();
    IMyProgrammableBlock self;
    string CustomDataCache = "";

    public Program()
    {

        var _selfs = new List<IMyTerminalBlock>();
        GridTerminalSystem.SearchBlocksOfName($"{group}", _selfs, p => p is IMyProgrammableBlock);

        if (_selfs.Count > 0)
        {
            self = _selfs[0] as IMyProgrammableBlock;
        }
        else
        {
            Echo("could not find self");
            return;
        }

        rotorZ = new MyRotor(this, GridTerminalSystem.GetBlockWithName($"{group} rotor z") as IMyMotorAdvancedStator);

        if (rotorZ.IsNull)
        {
            Echo("no rotor");
            return;
        }

        var _pistons = new List<IMyTerminalBlock>();
        GridTerminalSystem.SearchBlocksOfName($"{group} piston z", _pistons, p => p is IMyPistonBase);
        foreach (var _piston in _pistons)
        {
            var piston = new MyPiston(this, _piston as IMyPistonBase);
            if (piston.IsNull) continue;

            pistonsZ.Add(piston);
        }
        GridTerminalSystem.SearchBlocksOfName($"{group} piston plunge", _pistons, p => p is IMyPistonBase);
        foreach (var _piston in _pistons)
        {
            var piston = new MyPiston(this, _piston as IMyPistonBase);
            if (piston.IsNull) continue;
            pistonsP.Add(piston);
        }

        Runtime.UpdateFrequency = UpdateFrequency.Update10;
    }

    public void ParseCommands(string commands)
    {
        foreach (var a in commands.Replace("\n", ",").Split(','))
        {
            var args = a.Trim().Split(' ');

            if (args.Length < 3) { Echo("format: pz param value"); return; }
            var type = args[0];
            var param = args[1];
            var value = args[2];

            switch (type)
            {
                case "rz": rotorZ.SetParam(param, value); break;
                case "pz": foreach (var p in pistonsZ) { p.SetParam(param, value); } break;
                case "pp": foreach (var p in pistonsP) { p.SetParam(param, value); } break;
                default: Echo($"{args[0]} not recognized"); break;
            }
        }
    }

    public void Main(string argument, UpdateType updateType)
    {
        if (self.CustomData != CustomDataCache)
        {
            Echo($"Updating custom data");
            ParseCommands(self.CustomData);

            CustomDataCache = self.CustomData;
        }
        if (argument != "")
        {
            ParseCommands(argument);
        }

        rotorZ.Tick(updateType);
        foreach (var p in pistonsZ) p.Tick(updateType);
        foreach (var p in pistonsP) p.Tick(updateType);
    }
}
