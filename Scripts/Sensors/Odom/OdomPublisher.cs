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
using nav_msgs.msg;
using std_msgs.msg;
using geometry_msgs.msg;
using ROS2;

namespace Autonoma
{
    public class OdomPublisher : Publisher<Odometry>
    {
        public string modifiedRosNamespace = "/novatel_bottom";
        public string modifiedTopicName = "/odom";
        public float modifiedFrequency = 125f;
        public string modifiedFrameId = "utm";
        public string modifiedChildFrameId = "gps_top_ant1";
        private LatLngUTMConverter latLngUtmConverter;
        private double utmX;
        private double utmY;
        

        public void getPublisherParams()
        {
            // get things from sensor assigned by UI to the sensor
        }

        protected override void Start()
        {
            Debug.Log("Starting odometry publisher");
            getPublisherParams();
            this.rosNamespace = modifiedRosNamespace;
            this.topicName = modifiedTopicName;
            this.frequency = modifiedFrequency; // Hz
            this.frameId = modifiedFrameId;
            latLngUtmConverter = new LatLngUTMConverter("WGS 84"); //initialize converter
            base.Start();
        }
        public OdomSimulator odomSim;
        public GnssSimulator gnssSim;
        public ImuSimulator imuSim;
        public override void fillMsg()
        {
            // (ushort week, uint ms) weekMs = GnssSimulator.GetGPSWeekAndMS();

            // msg.Nov_header.Gps_week_number = weekMs.week;
            // msg.Nov_header.Gps_week_milliseconds = weekMs.ms;

            msg.Header.Frame_id = modifiedFrameId;
            msg.Child_frame_id = modifiedChildFrameId;

            double latitude = gnssSim.lat;
            double longitude = gnssSim.lon;
            // Convert latitude and longitude to UTM coordinates
            var utmResult = latLngUtmConverter.convertLatLngToUtm(latitude, longitude);
            utmX = utmResult.Easting;
            utmY = utmResult.Northing;

            //Position
            msg.Pose = new PoseWithCovariance();
            msg.Pose.Pose = new geometry_msgs.msg.Pose();
            msg.Pose.Pose.Position = new Point();
            // msg.Pose.Covariance = new double[36];
            for (int i = 0; i < msg.Pose.Covariance.Length; i++)
            {
                msg.Pose.Covariance[i] = 0.0;
            }

            msg.Pose.Pose.Position.X = utmX;
            msg.Pose.Pose.Position.Y = utmY;
            msg.Pose.Pose.Position.Z = gnssSim.height;

            //The Euler input is in ENU
            UnityEngine.Quaternion quat = UnityEngine.Quaternion.Euler((float)(imuSim.imuAngle.y), (float)(imuSim.imuAngle.x), (float)(imuSim.imuAngle.z + 90.0));
            msg.Pose.Pose.Orientation.X = quat.x;
            msg.Pose.Pose.Orientation.Y  = quat.y;
            msg.Pose.Pose.Orientation.Z = quat.z;
            msg.Pose.Pose.Orientation.W = quat.w;

            // Twist (velocity)
            msg.Twist = new TwistWithCovariance();
            msg.Twist.Twist = new Twist();

            msg.Twist.Twist.Linear = new geometry_msgs.msg.Vector3();
            msg.Twist.Twist.Linear.X = imuSim.imuVelLocal.x; // Forward   //change this to be using GPS twist
            msg.Twist.Twist.Linear.Y = imuSim.imuVelLocal.y; // Left
            msg.Twist.Twist.Linear.Z = imuSim.imuVelLocal.z; // Up

            msg.Twist.Twist.Angular = new geometry_msgs.msg.Vector3();
            msg.Twist.Twist.Angular.X = imuSim.imuGyro.x; 
            msg.Twist.Twist.Angular.Y = imuSim.imuGyro.y; 
            msg.Twist.Twist.Angular.Z = imuSim.imuGyro.z; 

            for (int i = 0; i < msg.Twist.Covariance.Length; i++)
            {
                msg.Twist.Covariance[i] = 0.0;
            }
        }

        // private Time GetCurrentTime()
        // {
        //     // Return the current ROS2 time
        //     return new Time
        //     {
        //         sec = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        //         nanosec = (uint)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000 * 1000000)
        //     };
        // }
    }
}
