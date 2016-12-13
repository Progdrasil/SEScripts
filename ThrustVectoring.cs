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

        public float speedLimit = 500;//speed limit of your game 
                                     //this is because you cant disable rotor safety lock 
                                     //the best you can do is set the safety lock as max speed of the game. 
                                     //default is 100m/s i recommend subtract 10 and set it as that. 
                                     //make sure all your rotors safety lock is at max speed 

        public bool creative = true;//set this to true if your game is in creative mode 
                                    // this is because in creative mode weight does not count your cargo; whereas in survival it does 
                                    // so i have to calculate thrust to weight differently 

        public const float defaultAccel = 1;//this is the default target acceleration you see on the display 
                                            // if you want to change the default, change this 
                                            // note, values higher than 1 will mean your nacelles will face the ground when you want to go 
                                            // down rather than just lower thrust 
                                            // '1g' is acceleration caused by current gravity (not nessicarily 9.81m/s) although 
                                            // if current gravity is less than 0.1m/s it will ignore this setting and be 9.81m/s anyway 

        // control module gamepad bindings 
        // type "/cm showinputs" into chat 
        // press the desired button 
        // put that text EXACTLY as it is in the quotes for the control you want 
        public const string jetpackButton = "g.ls";
        public const string dampenersButton = "c.damping";
        public const string lowerAccel = "";
        public const string raiseAccel = "";
        public const string resetAccel = "";
        public const string moveUp = "g.a";// normally your jump button 
        public const string moveDown = "g.b";// normally your crouch button 

        int count = 1;
        int maxSeconds = 60;
        StringBuilder profile = new StringBuilder();
        void ProfilerGraph()
        {
            if (count <= maxSeconds * 60)
            {
                double timeToRunCode = Runtime.LastRunTimeMs;

                profile.Append(timeToRunCode.ToString()).Append("\n");
                count++;
            }
            else
            {
                var screen = GridTerminalSystem.GetBlockWithName("DEBUG") as IMyTextPanel;
                screen?.WritePublicText(profile.ToString());
                screen?.ShowPublicTextOnScreen();
                count = 1;
            }
        }//end of Whip's Profiler Graph Code 

        void Main(string arg)
        {
            // ProfilerGraph(); 
            clearLists();

            writeBool = false;

            getController();


            // setup control module 
            Dictionary<string, object> inputs;
            try
            {
                inputs = Me.GetValue<Dictionary<string, object>>("ControlModule.Inputs");
            }
            catch (Exception e)
            {
                Echo("Control Module not installed");
                Echo("Go install the mod linked on this scripts workshop page");
                Echo("Exception: " + e);
                return;
            }

            // get gravity in world space 
            Vector3D worldGrav = controller.GetNaturalGravity();
            // TODO: find out what happens if this is in space 

            // get velocity 
            MyShipVelocities shipVelocities = controller.GetShipVelocities();
            Vector3D shipVelocity = shipVelocities.LinearVelocity;
            // Vector3D shipAngularVelocity = shipVelocities.AngularVelocity; 

            // setup thrust to weight calculations 
            MyShipMass myShipMass = controller.CalculateShipMass();
            int massWithoutCargo = myShipMass.BaseMass;
            int massWithCargo = myShipMass.TotalMass;
            int shipMass = massWithCargo;
            if (creative)
            {
                shipMass = massWithoutCargo;
            }

            float gravLength = (float)worldGrav.Normalize();
            float normalThrust = gravLength * shipMass;// f=ma (acceleration is gravity length) 
            float normalAccel = 9.81f;
            if (gravLength > 0.1)
            {
                normalAccel = gravLength * defaultAccel;// normal accel is proportional to gravity length 
            }
            if (accel == 0)
            {
                accel = normalAccel;
            }

            worldGrav = worldGrav * normalThrust;// worldGrav is now in N for mass of ship 
                                                 /*/depreciated movement controls 
                                                     // controls 
                                                     bool c = inputs.ContainsKey("c.crouch") || inputs.ContainsKey(moveDown); // find a specific key 
                                                     bool space = inputs.ContainsKey("c.jump") || inputs.ContainsKey(moveUp); // find a specific key 
                                                     bool w = inputs.ContainsKey("c.forward"); // find a specific key 
                                                     bool s = inputs.ContainsKey("c.backward"); // find a specific key 
                                                     bool a = inputs.ContainsKey("c.strafeleft"); // find a specific key 
                                                     bool d = inputs.ContainsKey("c.straferight"); // find a specific key 
                                                     Vector2 leftStick = (Vector2)inputs["g.lsanalog"]; // get left analog stick 
                                                     //float leftTrigger = (float)inputs["g.ltanalog"] // t is for trigger, s is for (joy)stick 

                                                 */
            if ((inputs.ContainsKey("c.damping") || inputs.ContainsKey(dampenersButton)) && !dampenersIsPressed)
            {//inertia dampener key 
                dampeners = !dampeners;//toggle 
                dampenersIsPressed = true;
                // this doesn't work when there are no thrusters on the same grid as the cockpit 
                // dampeners = controller.GetValue<bool>("DampenersOverride"); 
            }
            if (!inputs.ContainsKey("c.damping") && !inputs.ContainsKey(dampenersButton))
            {
                dampenersIsPressed = false;
            }
            if ((inputs.ContainsKey("c.thrusts") || inputs.ContainsKey(jetpackButton)) && !jetpackIsPressed)
            {//jetpack key 
                jetpack = !jetpack;//toggle 
                jetpackIsPressed = true;
            }
            if (!inputs.ContainsKey("c.thrusts") && !inputs.ContainsKey(jetpackButton))
            {
                jetpackIsPressed = false;
            }
            if ((inputs.ContainsKey("plus") || inputs.ContainsKey(raiseAccel)) && !plusIsPressed)
            {//throttle up 
                accel *= 1.5f;//exponential growth 
                plusIsPressed = true;
            }
            if (!inputs.ContainsKey("plus") && !inputs.ContainsKey(raiseAccel))
            { //increase target acceleration 
                plusIsPressed = false;
            }

            if ((inputs.ContainsKey("minus") || inputs.ContainsKey(lowerAccel)) && !minusIsPressed)
            {//throttle down 
                accel /= 1.5f;//exponential decay 
                minusIsPressed = true;
            }
            if (!inputs.ContainsKey("minus") && !inputs.ContainsKey(lowerAccel))
            { //lower target acceleration 
                minusIsPressed = false;
            }
            if (inputs.ContainsKey("0") || inputs.ContainsKey(resetAccel)) //default throttle 
                accel = normalAccel;
            float thrustOffset = accel * shipMass;// f=ma 
            MatrixD controllerMatrix = controller.WorldMatrix;
            /*/rest of depreciated movement controls 
                Vector3D desiredVec = Vector3D.Zero; 


                if(arg.Contains("%Vector")) { 
                    // parse the arg to get the vector 
                    // make it desiredVec 
                } else { 
                    //apply the keys 
                    if(w) { desiredVec += controllerMatrix.Forward; } 
                    if(s) { desiredVec += controllerMatrix.Backward; } 
                    if(a) { desiredVec += controllerMatrix.Left; } 
                    if(d) { desiredVec += controllerMatrix.Right; } 
                    if(space) { desiredVec += controllerMatrix.Up; } 
                    if(c) { desiredVec += controllerMatrix.Down; } 
                    // analog input here 
                    if(Math.Abs(leftStick.X) > analogDeadZone || Math.Abs(leftStick.Y) > analogDeadZone) { 
                        desiredVec += controllerMatrix.Right * leftStick.X / analogMaxValue; 
                        desiredVec += controllerMatrix.Backward * leftStick.Y / analogMaxValue; 
                    } 
                } 
            //*/
            // new movement controls 
            var moveVec = (Vector3)inputs["c.movement"];
            controllerMatrix.Translation = Vector3D.Zero;
            Vector3D desiredVec = Vector3D.Transform(moveVec, controllerMatrix);

            // write("desiredVec: " + desiredVec); 

            desiredVec = desiredVec * thrustOffset;// force multiplier 


            //safety, dont go over max speed 
            if (shipVelocity.Length() > speedLimit)
            {
                desiredVec -= shipVelocity * thrustOffset;
            }//dampeners 
            else if (dampeners && desiredVec == Vector3D.Zero)
            {
                desiredVec -= shipVelocity * thrustOffset / 10;
            }

            // point thrust in opposite direction, add gravity 
            Vector3D requiredVec = -desiredVec + worldGrav;

            //setup Nacelles 
            getNacelles();

            // set the vector 
            /* 
	        for(int i = 0; i < nacelles.Count; i++) { 
		        nacelles[i].requiredVec = requiredVec / nacelles.Count; 
	        }*/

            // group similar nacelles (rotor axis is same direction) 
            List<List<Nacelle>> nacelleGroups = new List<List<Nacelle>>();
            for (int i = 0; i < nacelles.Count; i++)
            {
                bool foundGroup = false;
                foreach (List<Nacelle> g in nacelleGroups)
                {// check each group to see if its lined up 
                    if (Math.Abs(Vector3D.Dot(nacelles[i].rotor.axis, g[0].rotor.axis)) > 0.9f)
                    {
                        g.Add(nacelles[i]);
                        foundGroup = true;
                        break;
                    }
                }
                if (!foundGroup)
                {// if it never found a group, add a group 
                    nacelleGroups.Add(new List<Nacelle>());
                    nacelleGroups[nacelleGroups.Count - 1].Add(nacelles[i]);
                }
            }

            // asdf 
            Vector3D asdf = Vector3D.Zero;
            // 1 
            foreach (List<Nacelle> g in nacelleGroups)
            {
                g[0].requiredVec = reject2(requiredVec, g[0].rotor.axis);
                asdf += g[0].requiredVec;
            }
            // 2 
            asdf -= requiredVec;
            // 3 
            foreach (List<Nacelle> g in nacelleGroups)
            {
                g[0].requiredVec -= asdf;
            }
            // 4 
            asdf /= nacelleGroups.Count;
            // 5 
            foreach (List<Nacelle> g in nacelleGroups)
            {
                g[0].requiredVec += asdf;
            }
            // apply first nacelle settings to rest in each group 
            foreach (List<Nacelle> g in nacelleGroups)
            {
                Vector3D req = g[0].requiredVec / g.Count;
                for (int i = 0; i < g.Count; i++)
                {
                    g[i].requiredVec = req;
                    g[i].go();
                }
            }

            // write("groups: " + nacelleGroups.Count); 
            Vector3D totalThrust = Vector3D.Zero;
            foreach (List<Nacelle> g in nacelleGroups)
            {
                foreach (Nacelle n in g)
                {
                    // double check, make sure its on a normal to the axis 
                    totalThrust += reject2(n.requiredVec, n.rotor.axis);
                }
            }
            /*/debugging data 
	        for(int i = history.Length - 1; i >= 0; i--) { 
		        if(i > 0) { 
			        history[i] = history[i - 1]/2; 
		        } else { 
			        history[i] = (requiredVec - totalThrust)*513.0/1024.0; 
		        } 
	        } 
	        Vector3D asdfasdf = Vector3D.Zero; 
	        for(int i = 0; i < history.Length; i++) { 
		        asdfasdf += history[i]; 
	        } 
 
	        float max = nacelles[0].thrusters[0].thruster.MaxThrust; 
	        float cur = nacelles[0].thrusters[0].thruster.CurrentThrust; 
	        float thr = nacelles[0].thrusters[0].thruster.ThrustOverride; 
	        write("Max: " + max); 
	        write("Cur: " + cur); 
	        write("Thr: " + thr); 
	        write("max thrust: " + nacelles[0].thrusters[0].maxThrust); 
	        write("efficiency: " + Math.Round(100 * nacelles[0].thrusters[0].maxThrust/nacelles[0].thrusters[0].thruster.MaxThrust, 0) + "%"); 
	        write("requiredVec: " + Vector3D.Round(requiredVec)); 
	        write("total Vec:      " + Vector3D.Round(totalThrust)); 
	        write("diff: " + Vector3D.Round(asdfasdf)+"\n" + asdfasdf.X+"\n"+asdfasdf.Y+"\n"+asdfasdf.Z); 
	        //*/

            write("Target Accel: " + Math.Round(accel / 9.81f, 2) + "g");
            write("Thrusters: " + jetpack);
            write("Dampeners: " + dampeners);
            write("Active Nacelles: " + nacelles.Count);
            write("Flight Mode: " + (creative ? "Creative" : "Survival"));
            // write(nacelles[0].rotor.theString); 

            double totalMaxThrust = 0;
            double totalCurrMaxThrust = 0;
            foreach (Nacelle n in nacelles)
            {
                foreach (Thruster t in n.thrusters)
                {
                    totalMaxThrust += t.thruster.MaxThrust;
                    totalCurrMaxThrust += t.maxThrust;
                }
            }
            write("Avg Efficiency: " + Math.Round(100 * totalCurrMaxThrust / totalMaxThrust, 0) + "%");

        }
        public static T Clamp<T>(T value, T max, T min) where T : System.IComparable<T>
        {
            T result = value;
            if (value.CompareTo(max) > 0)
                result = max;
            if (value.CompareTo(min) < 0)
                result = min;
            return result;
        }

        public static Vector3D[] history = new Vector3D[10];

        public static float maxThrust = 0;
        public static float minThrust = 999999999;
        public List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        public List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
        public List<Nacelle> nacelles = new List<Nacelle>();
        // public float minMaxThrust = 0; 
        public static bool jetpack = false;
        public static bool jetpackIsPressed = false;
        public static bool dampeners = false;
        public static bool dampenersIsPressed = false;
        public static bool plusIsPressed = false;
        public static bool minusIsPressed = false;
        public static float accel = 0;
        public IMyShipController controller;

        void clearLists()
        {
            blocks.Clear();
            blockGroups.Clear();
            nacelles.Clear();
        }

        // TODO: put data invalidation and keep values... dont initialize everything EVERY SINGLE FRAME 
        // TODO: fix thrust values T.T 
        void getNacelles()
        {
            GridTerminalSystem.GetBlockGroups(blockGroups);
            // go through groups 
            for (int i = 0; i < blockGroups.Count; i++)
            {
                // if current group is marked to be a nacelle 
                if (blockGroups[i].Name.IndexOf("%Nacelle") != -1)
                {
                    bool addGroup = true;

                    // setup lists 
                    List<Thruster> thrusters = new List<Thruster>();
                    Rotor rotor = new Rotor(controller);

                    // list of blocks in current group 
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                    blockGroups[i].GetBlocks(groupBlocks);// get blocks from current group and put them in the list 

                    // setup go through blocks in current group 
                    for (int j = 0; j < groupBlocks.Count; j++)
                    {
                        // thruster 
                        try
                        {
                            //TODO: put support for multiple rotors in the same group 
                            if (rotor.rotor == null)
                            {
                                rotor.rotor = (IMyMotorStator)groupBlocks[j];
                            }
                        }
                        catch (Exception exc)
                        {
                            //its not a rotor 
                        }

                        // thruster 
                        try
                        {
                            thrusters.Add(new Thruster((IMyThrust)groupBlocks[j]));
                        }
                        catch (Exception exc)
                        {
                            //its not a thruster 
                        }
                    }
                    rotor.checkRotName();
                    if (addGroup && rotor.rotor.IsAttached && thrusters.Count > 0)
                    {
                        // nacelles.Add(new Nacelle(blockGroups[i].Name, rotor, controller, engines, engineThrust)); 
                        nacelles.Add(new Nacelle(blockGroups[i].Name, rotor, controller, thrusters));
                        // minMaxThrust = (minMaxThrust == 0 ? nacelles[nacelles.Count - 1].totalThrust : Math.Min(nacelles[nacelles.Count - 1].totalThrust, minMaxThrust)); 
                    }
                }
            }
        }

        public double angleBetweenCos(Vector3D a, Vector3D b)
        {
            double dot = Vector3D.Dot(a, b);
            double Length = a.Length() * b.Length();
            return dot / Length;
        }

        public Vector3D project2(Vector3D a, Vector3D b)
        {
            double aDotB = Vector3D.Dot(a, b);
            double bDotB = Vector3D.Dot(b, b);
            return b * aDotB / bDotB;
        }

        public Vector3D reject2(Vector3D a, Vector3D b)
        {
            return a - project2(a, b);
        }

        void getController()
        {
            blocks.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(blocks);
            if (blocks.Count < 1)
            {
                Echo("ERROR: no ship controller found");
                return;
            }

            controller = (IMyShipController)blocks[0];
            int lvl = 0;
            bool allCockpitsAreFree = true;
            int prevLvl = 0;
            IMyShipController prevController = controller;
            bool hasReverted = false;
            for (int i = 0; i < blocks.Count; i++)
            {
                // only one of them is being controlled 
                if (((IMyShipController)blocks[i]).IsUnderControl && allCockpitsAreFree)
                {
                    prevController = controller;
                    prevLvl = lvl;
                    controller = ((IMyShipController)blocks[i]);
                    lvl = 5;
                }//more than one is being controlled, it reverts to previous setting 
                else if (((IMyShipController)blocks[i]).IsUnderControl && !allCockpitsAreFree && !hasReverted)
                {
                    lvl = prevLvl;
                    controller = prevController;
                    hasReverted = true;
                }//has %Main in the name 
                else if (((IMyShipController)blocks[i]).CustomName.IndexOf("%Main") != -1 && lvl < 4)
                {
                    controller = ((IMyShipController)blocks[i]);
                    lvl = 4;
                }//is ticked as a main cockpit 
                else if (((IMyShipController)blocks[i]).GetValue<bool>("MainCockpit") && lvl < 3)
                {
                    controller = ((IMyShipController)blocks[i]);
                    lvl = 3;
                }//is set to control thrusters 
                else if (((IMyShipController)blocks[i]).ControlThrusters && lvl < 2)
                {
                    controller = ((IMyShipController)blocks[i]);
                    lvl = 2;
                }
                else
                {
                    controller = ((IMyShipController)blocks[i]);
                    lvl = 1;
                }
            }
        }

        private IMyTextPanel screen;
        private bool writeBool;
        public void write(string str)
        {
            str += "\n";
            if (writeBool)
            {
                screen.WritePublicText(str, true);
            }
            else
            {
                blocks.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks);
                if (blocks.Count == 0)
                {
                    return;
                }
                screen = (IMyTextPanel)blocks[0];
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].CustomName.IndexOf("%VectorLCD") != -1)
                    {
                        screen = (IMyTextPanel)blocks[i];
                    }
                }
                screen.WritePublicText(str);
                writeBool = true;
            }
        }

        public class Nacelle
        {
            // misc data 
            public string name;

            // physical parts 
            public Rotor rotor;
            public List<Thruster> thrusters = new List<Thruster>();
            public IMyShipController controller;

            // vector data 
            public static Vector3D resultantVec;//total resultant vec caused by all nacelles 
            public Vector3D requiredVec;

            // non-vector data 
            public float totalThrust = 0;
            public static double sumDotRes;
            public double dotOfRes;

            //Quaternion rotation; - for use later 
            //Defines a four-dimensional vector (x,y,z,w), which is used to efficiently rotate an 
            //object about the (x, y, z) vector by the angle theta, where w = cos(theta/2). 

            public Nacelle(string name, Rotor rotor, IMyShipController controller, List<Thruster> thrusters)
            {
                this.name = name;
                this.controller = controller;
                this.rotor = rotor;
                this.thrusters = thrusters;

                resultantVec = Vector3D.Zero;
                Nacelle.sumDotRes = 0;

                rotor.getAxis();

                for (int i = 0; i < thrusters.Count; i++)
                {
                    totalThrust += (float)thrusters[i].maxThrust;
                    if (Program.jetpack)
                    {
                        thrusters[i].setPower(true);
                    }
                    else
                    {
                        thrusters[i].setPower(false);
                    }
                }
            }

            // final calculations and setting physical components 
            public void go()
            {
                rotor.desiredVec = requiredVec;
                rotor.doTrig2();

                //set the thrust for each engine 
                for (int i = 0; i < thrusters.Count; i++)
                {
                    thrusters[i].setThrust(requiredVec / thrusters.Count);
                }
            }

            // calculating what percentage of resultantVec to counter, should be done after resultantVec and before go 
            public void calcDotRes()
            {
                dotOfRes = Vector3D.Dot(Vector3D.Normalize(resultantVec), rotor.axis);
                dotOfRes = Math.Abs(dotOfRes);
                dotOfRes--;
                dotOfRes *= -1;
                sumDotRes += dotOfRes;
            }
            /* 
                public string testProject() { 
                    Vector3D result = Program.project2(new Vector3D(1, 1, 1), new Vector3D(1, 0, 0)); 
                    return "testProject: \nshould be: {1, 0, 0}\n" + result; 
                }*/
        }

        public class Thruster
        {
            public IMyThrust thruster;
            public double maxThrust;
            public Vector3D down;
            public string asdf = "";

            public Thruster(IMyThrust thruster)
            {
                this.thruster = thruster;
                this.down = thruster.WorldMatrix.Down;
                if (thruster.CurrentThrust > 0 || thruster.ThrustOverride > 0)
                {
                    maxThrust = thruster.MaxThrust * thruster.CurrentThrust / thruster.ThrustOverride;
                }
                else
                {
                    maxThrust = thruster.MaxThrust;
                }
            }

            public void setPower(bool powerOn)
            {
                if (powerOn)
                {
                    thruster.ApplyAction("OnOff_On");
                    return;
                }
                thruster.ApplyAction("OnOff_Off");
            }

            public void setThrust(Vector3D thrustVec)
            {
                // thrustVec is in newtons 
                // double thrust = Vector3D.Dot(thrustVec, down); 
                double thrust = thrustVec.Length();
                thrust *= 100;// convert to percentage 
                thrust /= maxThrust;
                Program.Clamp(thrust, 100, 0);
                thruster.SetValue<float>("Override", (float)thrust);// apply the thrust 
            }
        }

        public class Rotor
        {
            public IMyMotorStator rotor;
            // world space 
            public Vector3D desiredVec;
            public float angle;
            public string theString;
            public Vector3D axis;
            public float finalAngle;
            public IMyShipController controller;
            public int offset;

            public Rotor(IMyShipController controller)
            {
                this.controller = controller;
            }

            // sets the rotor offset from the name 
            public void checkRotName()
            {
                int nameA = rotor.CustomName.IndexOf("%(");
                int nameB = rotor.CustomName.IndexOf(")");
                if (nameA != -1 && nameB > nameA)
                {
                    string offsetStr = rotor.CustomName.Substring(nameA + 2, (nameB - nameA) - 2);
                    if (Int32.TryParse(offsetStr, out offset))
                    {
                        // theString = "\noffset for " + rotor.CustomName + " set to " + offset + " degrees"; 
                    }
                    else
                    {
                        // theString = "\nERROR: rotor offset could not be parsed"; 
                    }
                }
            }

            // gets the rotor 
            public void getAxis()
            {
                theString = "\n" + rotor.CustomName + ": ";
                MatrixD rotorMatrix = rotor.WorldMatrix;

                axis = rotorMatrix.Up;// non used dimensions is in world space 
                if (axis.Length() != 1)
                {// TODO: remove this and check if it still works 
                    axis.Normalize();
                }
            }

            // sets the angle to be in the direction of the vector 
            public void doTrig()
            {
                // transform by matrix to go to world space 
                // transform by matrixI to go to local space 
                MatrixD rotorMatrix = rotor.WorldMatrix;
                MatrixD rotorMatrixI = MatrixD.Invert(rotorMatrix);

                // transpose is cheaper inverse, but only use it with orientation. no translation 
                // Matrix.Transpose(ref rotorMatrix, out rotorMatrix); 

                // this.desiredVec.Normalize(); 

                // turn desiredVec from world space to rotor local space 
                // desiredVec = Vector3D.Transform(desiredVec, rotorMatrixI); 
                // desiredVec.Normalize(); 
                // theString += "\ndesiredVec: \n" + desiredVec.ToString("0.0"); 

                theString += "\ndesiredVec: \n" + desiredVec.ToString("0.0");
                desiredVec = Vector3D.Transform(desiredVec, rotorMatrixI);
                theString += "\ndesiredVec: \n" + desiredVec.ToString("0.0");
                desiredVec.Normalize();
                theString += "\ndesiredVec: \n" + desiredVec.ToString("0.0");
                // theString += "\nlength: " + desiredVec.Length(); 

                this.angle = (float)Math.Atan(desiredVec.Z / desiredVec.X);
                if (desiredVec.X > 0)
                {
                    if (desiredVec.Z > 0)
                    {
                        // x+ z+ 
                        theString += "\nx+ z+";
                        this.angle = -(float)(2 * Math.PI - this.angle);
                    }
                    else
                    {
                        // x+ z- 
                        theString += "\nx+ z-";
                    }
                }
                else
                {
                    if (desiredVec.Z > 0)
                    {
                        // x- z+ 
                        theString += "\nx- z+";
                        this.angle = (float)(Math.PI + this.angle);
                    }
                    else
                    {
                        // x- z- 
                        theString += "\nx- z-";
                        this.angle = (float)(Math.PI + this.angle);
                    }
                }
                theString += "\nangle: " + Math.Round(180 * this.angle / Math.PI, 1);

                setPos(angle + (float)(offset * Math.PI / 180));
            }

            public double angleBetweenCos(Vector3D a, Vector3D b)
            {
                double dot = Vector3D.Dot(a, b);
                double Length = a.Length() * b.Length();
                return dot / Length;
            }

            public void doTrig2()
            {
                desiredVec = Vector3D.Reject(desiredVec, axis);

                // angle between vectors 
                float angle = -(float)Math.Acos(angleBetweenCos(rotor.WorldMatrix.Forward, desiredVec));

                if (Math.Acos(angleBetweenCos(rotor.WorldMatrix.Left, desiredVec)) > Math.PI / 2)
                {
                    angle = (float)(2 * Math.PI - angle);
                }

                setPos(angle + (float)(offset * Math.PI / 180));
            }

            float cutAngle(float angle)
            {
                while (angle > Math.PI)
                {
                    angle -= 2 * (float)Math.PI;
                }
                while (angle < -Math.PI)
                {
                    angle += 2 * (float)Math.PI;
                }
                // theString += "\nnew Angle: " + Math.Round(180*angle/Math.PI, 1); 
                return angle;
            }

            void setPos(float x)//no rotor limits here 
            {
                theString += "\nbefore cut: " + (float)Math.Round(180 * x / Math.PI);
                x = cutAngle(x);
                float velocity = 60;
                // theString += "\nSetting angle to:\n" + (x * 180/Math.PI); 
                float x2 = cutAngle(rotor.Angle);
                theString += "\nFinal Angle: " + (float)Math.Round(180 * x / Math.PI);
                if (Math.Abs(x - x2) < Math.PI)
                {
                    if (x2 < x)
                    {//dont cross origin 
                        rotor.SetValue<float>("Velocity", velocity * Math.Abs(x - x2));
                    }
                    else
                    {
                        rotor.SetValue<float>("Velocity", -velocity * Math.Abs(x - x2));
                    }
                }
                else
                {
                    //cross origin 
                    if (x2 < x)
                    {
                        rotor.SetValue<float>("Velocity", -velocity * Math.Abs(x - x2));
                    }
                    else
                    {
                        rotor.SetValue<float>("Velocity", velocity * Math.Abs(x - x2));
                    }
                }
            }
        }
        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}