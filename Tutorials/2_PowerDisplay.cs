using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

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
public void Main(string args)
{
    //Variables
    var reactors = new List<IMyTerminalBlock>();
    var solarPanels = new List<IMyTerminalBlock>();
    var batteries = new List<IMyTerminalBlock>();
    var display = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Wide LCD test");

    string reactorCurrent = "";
    string solarCurrent = "";
    string batCurrent = "";

    string reactorOutput = "";
    string solarOutput = "";
    string batOutput = "";

    //instantiate Varibables
    GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);
    GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarPanels);
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);

    //power loop
    for(int i = 0; i < reactors.Count; i++)
    {
        //Current Output
        if (reactors[i].DetailedInfo.Contains("Current Output"))
            reactorCurrent += reactors[i].DetailedInfo.Substring(reactors[i].DetailedInfo.IndexOf("Current Output")) + "\n";

        //max output
        if (reactors[i].DetailedInfo.Contains("Max Output") && reactors[i].IsWorking)
            reactorOutput += reactors[i].DetailedInfo.Substring(reactors[i].DetailedInfo.IndexOf("Max Output")) + "\n";
    }

    for (int i = 0; i < solarPanels.Count; i++)
    {
        //Current Output
        if (solarPanels[i].DetailedInfo.Contains("Current Output"))
            solarCurrent += solarPanels[i].DetailedInfo.Substring(solarPanels[i].DetailedInfo.IndexOf("Current Output")) + "\n";

        //max output
        if (solarPanels[i].DetailedInfo.Contains("Max Output") && solarPanels[i].IsWorking)
            solarOutput += solarPanels[i].DetailedInfo.Substring(solarPanels[i].DetailedInfo.IndexOf("Max Output")) + "\n";
    }

    for (int i = 0; i < batteries.Count; i++)
    {
        //Current Output
        if (batteries[i].DetailedInfo.Contains("Current Output"))
            batCurrent += batteries[i].DetailedInfo.Substring(batteries[i].DetailedInfo.IndexOf("Current Output")) + "\n";

        //max output
        if (batteries[i].DetailedInfo.Contains("Max Output") && batteries[i].IsWorking && !batteries[i].DetailedInfo.Contains("Fully recharged in:"))
            batOutput += batteries[i].DetailedInfo.Substring(batteries[i].DetailedInfo.IndexOf("Max Output")) + "\n";
    }

    //DisplayText
    display.WritePublicText("Current Power Use: " + PowerUse(reactorCurrent, solarCurrent, batCurrent), false);
    display.WritePublicText("\nMaximum Power Use: " + PowerUse(reactorOutput, solarOutput, batOutput), true);
    display.WritePublicText("\nPower Usage: " + PercentageUse(PowerUse(reactorCurrent, solarCurrent, batCurrent), PowerUse(reactorOutput, solarOutput, batOutput)), true);
    display.WritePublicText("\nTotal Remaining Uranium: " + TotalUranium(reactors), true);

    //Attempted Display Update
    display.ShowPublicTextOnScreen();
    display.UpdateVisual();
}

//Finds current Power Use
public string PowerUse(string reactorCurrent, string solarCurrent, string batCurrent)
{
    var reactorStr = reactorCurrent.Split(' ');
    var solarStr = solarCurrent.Split(' ');
    var batStr = batCurrent.Split(' ');

    string powerRawTotal = "";

    for (int i = 0; i < reactorStr.Length; i++)
    {
        if (reactorStr[i].Contains("Output:"))
            powerRawTotal += reactorStr[i + 1] + " " + reactorStr[i + 2].Substring(0, 2) + " ";
    }

    for (int i = 0; i < solarStr.Length; i++)
    {
        if (solarStr[i].Contains("Output:"))
            powerRawTotal += solarStr[i + 1] + " " + solarStr[i + 2].Substring(0, 2) + " ";
    }

    for (int i = 0; i < batStr.Length; i++)
    {
        if (batStr[i].Contains("Output:"))
            powerRawTotal += batStr[i + 1] + " " + batStr[i + 2].Substring(0, 2) + " ";
    }

    //math
    var tempVals = powerRawTotal.Split(' ');
    float values = 0.0f;

    for(int i = 0; i < tempVals.Length - 1; i++)
    {
        if (tempVals[i + 1] == "GW")
            values += float.Parse(tempVals[i]) * 1000000.0f;
        else if (tempVals[i + 1] == "MW")
            values += float.Parse(tempVals[i]) * 1000.0f;
        else if (tempVals[i + 1] == "kW")
            values += float.Parse(tempVals[i]);
        else if (tempVals[i + 1] == "W")
            values += float.Parse(tempVals[i]) * 0.001f;
    }

    //set correct values
    if (values > 1000000.0f)
    {
        values *= 0.000001f;
        powerRawTotal = values.ToString("n2") + " GW";
    }
    else if (values > 1000.0f)
    {
        values *= 0.001f;
        powerRawTotal = values.ToString("n2") + " MW";
    }
    else if (values > 1.0f)
    {
        powerRawTotal = values.ToString("n2") + " kW";
    }
    else if (values < 1.0f)
    {
        values *= 1000.0f;
        powerRawTotal = values.ToString("n2") + " W";
    }

    return powerRawTotal;
}

// find percentage of Total Power Use
public string PercentageUse(string currentPower, string outputPower)
{
    string percentUse = "";

    //Parse data
    var curStr = currentPower.Split(' ');
    var maxStr = outputPower.Split(' ');

    float current = float.Parse(curStr[0]);
    float max = float.Parse(maxStr[0]);

    if (curStr[1] == "GW")
        current *= 1000000.0f;
    else if (curStr[1] == "MW")
        current *= 1000.0f;
    else if (curStr[1] == "kW")
        current *= 1.0f;
    else if (curStr[1] == "W")
        current *= 0.001f;

    if (maxStr[1] == "GW")
        max *= 1000000.0f;
    else if (maxStr[1] == "MW")
        max *= 1000.0f;
    else if (maxStr[1] == "kW")
        max *= 1.0f;
    else if (maxStr[1] == "W")
        max *= 0.001f;

    float total = (current / max) * 100.0f;

    percentUse = total.ToString("n2") + " %";
    return percentUse;
}

//determine total Uranium
public string TotalUranium(List<IMyTerminalBlock> reactors)
{
    //Setup
    float totalUranium = 0.0f;

    //IMyInventoryOwner owner;
    IMyInventory inventory;
    List<IMyInventoryItem> totalItems = new List<IMyInventoryItem>();

    //acquire all reactor inventories
    for (int i = 0; i < reactors.Count; i++)
    {
        //owner = (IMyInventoryOwner)reactors[i];
        inventory = reactors[i].GetInventory(0);
        var items = inventory.GetItems();

        totalItems.AddRange(items);
    }

    for (int i = 0; i < totalItems.Count; i++)
    {
        if (totalItems[i].Content.SubtypeName == "Uranium")
            totalUranium += (float)totalItems[i].Amount;
    }

    return totalUranium.ToString("n2");
}
        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}