/* 
Copyright 2024 Purdue AI Racing

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at:

    http://www.apache.org/licenses/LICENSE-2.0

The software is provided "AS IS", WITHOUT WARRANTY OF ANY KIND, 
express or implied. In no event shall the authors or copyright 
holders be liable for any claim, damages or other liability, 
whether in action of contract, tort or otherwise, arising from, 
out of or in connection with the software or the use of the software.
*/

using UnityEngine;
using geometry_msgs.msg;
using ROS2;

namespace Autonoma
{
    public class InsTwistPublisher : Publisher<TwistWithCovarianceStamped>
    {
        public string modifiedRosNamespace = "/novatel_bottom";
        public string modifiedTopicName = "/ins_twist";
        public float modifiedFrequency = 125f;
        public string modifiedFrameId = "gps_bottom";
        
        public void getPublisherParams()
        {
            // get things from sensor assigned by UI to the sensor
        }

        protected override void Start()
        {
            getPublisherParams();
            this.rosNamespace = modifiedRosNamespace;
            this.topicName = modifiedTopicName;
            this.frequency = modifiedFrequency; // Hz
            this.frameId = modifiedFrameId;
            base.Start();
        }
        public ImuSimulator imuSim;
        public InsSimulator insSim;
        public override void fillMsg()
        {
            msg.Header.Frame_id = modifiedFrameId;

            msg.Twist.Twist.Linear.X = imuSim.imuVelLocal.x; // Forward   
            msg.Twist.Twist.Linear.Y = imuSim.imuVelLocal.y; // Left
            msg.Twist.Twist.Linear.Z = imuSim.imuVelLocal.z; // Up

            msg.Twist.Twist.Angular.X = imuSim.imuGyro.x; 
            msg.Twist.Twist.Angular.Y = imuSim.imuGyro.y; 
            msg.Twist.Twist.Angular.Z = imuSim.imuGyro.z; 

        }


    }
}
