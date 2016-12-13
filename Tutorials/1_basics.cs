using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

//include the space engineers API
using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        //=======================================================================
        //////////////////////////BEGIN//////////////////////////////////////////
        //=======================================================================
        public void Main()
        {
            IMyTextPanel display = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Wide LCD test");

            char letter = 'A';
            string str = "this is a string";
            display.WritePublicText(letter + "\n" + str + "\n" + str[3], false);

            int int1 = 0;
            int int2 = 1;
            int int3 = 2;

            int[] array = new int[3];
            //array = new int[3];
            array[0] = int1;
            array[1] = int2;
            array[2] = int3;

            str = "\n\n\n" + array[0] + "\n" + array[1] + "\n" + array[2];
            //display.WritePublicText(int1 + "\n" + int2 + "\n" + int3, false); 
            display.WritePublicText(str, true);
        }
        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}