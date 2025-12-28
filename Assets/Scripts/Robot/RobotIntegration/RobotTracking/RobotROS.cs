using System;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

public delegate void RosNavigationGoalIDReceivedOrReReceived(string goalId);
public delegate void RosNavigationGoalCancelledOrAborted(string goalId);
public delegate void RosNavigationGoalReached(string goalId);
public delegate void RosNavigationGoalFinished();
public delegate void RosJointTrajectoryGoalIDReceivedOrReReceived(string goalId);
public delegate void RosJointTrajectoryGoalCancelledOrAborted(string goalId);
public delegate void RosJointTrajectoryGoalReached(string goalId);
public delegate void RosJointTrajectoryGoalFinished();


public class RobotROS : MonoBehaviour
{
    [Header("WebSocket")]
    WebSocket websocket;
    public List<string> allowedSenderIps = new List<string> { "140.180.236.116", "140.180.237.177", "140.180.236.76", "140.180.234.33" };
    public string targetRobotRosIp = "140.180.234.130";
    public int targetRobotRosPort = 9090;
    private bool open = false;
    [ReadOnly] public string webSocketStatus;

    private (Vector3 translation, Quaternion rotation)? _latestMapToOdom = null;
    private (Vector3 translation, Quaternion rotation)? _latestOdomToBase = null;
    private Vector3 rosTranslation;
    private Quaternion rosRotation;

    [Header("Path Planning")]
    public Transform navigationFeedbackPose;
    [ReadOnly] public LineRenderer pathPlan;

    [Header("ROS Actions")]
    [ReadOnly] public StretchKinematicChain stretchKinematicChain;
    [ReadOnly] public AlignRobotUnity alignRobotUnity;
    public bool triggerSubscribeToPlan = false;
    public bool triggerSubscribeToJointState = false;
    public bool triggerPublishTransform = false;
    public bool triggerSendNavigateAction = false; 
    public Vector3 navigateGoal = new Vector3(0.0f, 0.0f, 0.0f);
    public bool triggerCmdVel = false;
    public bool triggerFollowJointTrajectoryActionToStow = false;
    public bool triggerFollowJointTrajectoryActionToLow = false;
    public bool triggerFollowJointTrajectoryActionToMax = false;

    [Header("Callbacks")]
    [ReadOnly] public RosNavigationGoalIDReceivedOrReReceived rosNavigationGoalIDReceivedOrReReceived;
    [ReadOnly] public RosNavigationGoalCancelledOrAborted rosNavigationGoalCancelledOrAborted;
    [ReadOnly] public RosNavigationGoalReached rosNavigationGoalReached;
    [ReadOnly] public RosNavigationGoalFinished rosNavigationGoalFinished;
    [ReadOnly] public RosJointTrajectoryGoalIDReceivedOrReReceived rosJointTrajectoryGoalIDReceivedOrReReceived;
    [ReadOnly] public RosJointTrajectoryGoalCancelledOrAborted rosJointTrajectoryGoalCancelledOrAborted;
    [ReadOnly] public RosJointTrajectoryGoalReached rosJointTrajectoryGoalReached;
    [ReadOnly] public RosJointTrajectoryGoalFinished rosJointTrajectoryGoalFinished;

    [Header("ROS Transform")]
    [SerializeField] private Vector3 robotRosPos;
    [SerializeField] private Quaternion robotRosRot;

    void Start()
    {
        if (allowedSenderIps.Contains(WebSocketServer.GetLocalIPAddress()))
        {
            ConnectToServer();
        }
        stretchKinematicChain = GetComponent<StretchKinematicChain>();
        alignRobotUnity = GetComponent<AlignRobotUnity>();
        pathPlan = LineRendererFactory.makeLineRenderer(gameObject, "RosNav2Path-plan", Color.white);
        pathPlan.material = new Material(Shader.Find("Sprites/Default"));
    }

    async public void ConnectToServer()
    {
        websocket = new WebSocket($"ws://{targetRobotRosIp}:{targetRobotRosPort}");

        websocket.OnOpen += () =>
        {
            webSocketStatus = "Connection open!";
            Debug.Log($"webSocketStatus: {webSocketStatus}");

            SendWebSocketMessage(GetPlanSubscription());
            SendWebSocketMessage(GetJointStatesSubscription());
            SendWebSocketMessage(GetNavigateToActionStatusArraySubscription());
            SendWebSocketMessage(GetFollowJointTrajectoryActionStatusArraySubscription());
            SendWebSocketMessage(GetTfSubscription());
            open = true;
        };

        websocket.OnError += (e) =>
        {
            webSocketStatus = "Error! " + e;
            Debug.LogError($"webSocketStatus: {webSocketStatus}");

            open = false;
        };

        websocket.OnClose += (e) =>
        {
            webSocketStatus = "Connection closed or failed!";
            Debug.LogError($"webSocketStatus: {webSocketStatus}");

            open = false;
        };



        websocket.OnMessage += (bytes) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(bytes);
                ProcessMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"error / exception: {ex}");
            }
        };

        await websocket.Connect();
    }

    string GetRunstopServiceCall(bool enable)
    {
        return JsonConvert.SerializeObject(new
        {
            op = "call_service",
            service = "/runstop",
            type = "std_srvs/srv/SetBool",
            args = new {
                data = enable
            }
        });
    }

    string GetPlanSubscription()
    {
        return JsonConvert.SerializeObject(new
        {
            op = "subscribe",
            topic = "/plan",
            type = "nav_msgs/msg/Path"
        });
    }

    string GetTfSubscription()
    {
        return JsonConvert.SerializeObject(new
        {
            op = "subscribe",
            topic = "/base_pose",
            type = "geometry_msgs/msg/TransformStamped"
        });
    }

    string GetJointStatesSubscription()
    {
        return JsonConvert.SerializeObject(new
        {
            op = "subscribe",
            topic = "/joint_states",
            type = "sensor_msgs/msg/JointState"
        });
    }

    string GetNavigateToActionStatusArraySubscription()
    {
        return JsonConvert.SerializeObject(new
        {
            op = "subscribe",
            topic = "/navigate_to_pose/_action/status",
            type = "action_msgs/msg/GoalStatusArray"
        });
    }

    string GetFollowJointTrajectoryActionStatusArraySubscription()
    {
        return JsonConvert.SerializeObject(new
        {
            op = "subscribe",
            topic = "/stretch_controller/follow_joint_trajectory/_action/status",
            type = "action_msgs/msg/GoalStatusArray"
        });
    }

    string GetCmdVelPublish(float vx, float vy, float vz, float vrx, float vry, float vrz)
    {
        return JsonConvert.SerializeObject(new
        {
            op = "publish",
            topic = "/stretch/cmd_vel",
            type = "geometry_msgs/msg/Twist",
            msg = new
            {
                linear = new
                {
                    x = vx,
                    y = vy,
                    z = vz
                },
                angular = new
                {
                    x = vrx,
                    y = vry,
                    z = vrz
                }
             }
        });
    }

    string GetTransformPublish(string fromFrame, string toFrame, float x, float y, float z, float qx, float qy, float qz, float qw)
    {
        return JsonConvert.SerializeObject(new
        {
            op = "publish",
            topic = "/tf",
            type = "tf2_msgs/msg/TFMessageStamped",
            msg = new
            {
                transforms = new[]
                {
                    new
                    {
                        header = new
                        {
                            stamp = new
                            {
                                sec = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                nanosec = 0
                            },
                            frame_id = fromFrame
                        },
                        child_frame_id = toFrame,
                        transform = new
                        {
                            translation = new
                            {
                                x = x,
                                y = y,
                                z = z
                            },
                            rotation = new
                            {
                                x = qx,
                                y = qy,
                                z = qz,
                                w = qw
                            }
                        }
                    }
                }
            }
        });
    }

    string GetPosePublish(string fromFrame, string toFrame, float x, float y, float z, float qx, float qy, float qz, float qw)
    {
        return JsonConvert.SerializeObject(new
        {
            op = "publish",
            topic = "/questpose",
            type = "geometry_msgs/msg/Pose",
            msg = new
            {
                position = new
                {
                    x = x,
                    y = y,
                    z = z
                },
                orientation = new
                {
                    x = qx,
                    y = qy,
                    z = qz,
                    w = qw
                }
            }
        });
    }

    public string GetNavigateAction(float x, float y, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0, 0, yaw);
        return JsonConvert.SerializeObject(new
        {
            op = "send_goal",
            action_name = "/navigate_to_pose",
            action_type = "nav2_msgs/action/NavigateToPose",
            goal_msg = new
            {
                pose = new
                {
                    header = new
                    {
                        frame_id = "map",
                        stamp = new
                        {
                            sec = 0,
                            nanosec = 0
                        }
                    },
                    pose = new
                    {
                        position = new
                        {
                            x = x,
                            y = y,
                            z = 0.0f
                        },
                        orientation = new
                        {
                            x = rotation.x,
                            y = rotation.y,
                            z = rotation.z,
                            w = rotation.w
                        }
                    }
                }
            }
        });
    }

    public string GetJointTrajectoryAction(
        float? lift = null,
        float? extension = null,
        float? yaw = null,
        float? pitch = null,
        float? roll = null,
        float? grip = null)
    {
        var positions = new List<float>();
        var jointNames = new List<string>();

        if (lift.HasValue) { positions.Add(lift.Value); jointNames.Add("joint_lift"); }
        if (extension.HasValue) { positions.Add(extension.Value); jointNames.Add("wrist_extension"); }
        if (yaw.HasValue) { positions.Add(yaw.Value); jointNames.Add("joint_wrist_yaw"); }
        if (pitch.HasValue) { positions.Add(pitch.Value); jointNames.Add("joint_wrist_pitch"); }
        if (roll.HasValue) { positions.Add(roll.Value); jointNames.Add("joint_wrist_roll"); }
        if (grip.HasValue) { positions.Add(grip.Value); jointNames.Add("joint_gripper_finger_left"); }

        if (positions.Count == 0)
            throw new ArgumentException("At least one parameter must be set.");

        return JsonConvert.SerializeObject(new
        {
            op = "send_goal",
            action_name = "/stretch_controller/follow_joint_trajectory",
            action_type = "control_msgs/action/FollowJointTrajectory",
            goal_msg = new
            {
                trajectory = new
                {
                    joint_names = jointNames.ToArray(),
                    points = new[]
                    {
                    new
                    {
                        positions = positions.ToArray(),
                        time_from_start = new { sec = 0, nanosec = 0 }
                    }
                }
                }
            }
        });
    }


    public void ProcessMessage(string message)
    {
        ParseMessageAndCallHandler(message);
    }
   
   public void OnTFReceived(JToken tf)
    {
        var t = tf["transform"]?["translation"];
        var q = tf["transform"]?["rotation"];

        if (t == null || q == null)
            return;

        Vector3 translation = new Vector3(
            t.Value<float>("x"),
            t.Value<float>("y"),
            t.Value<float>("z")
        );

        Quaternion rotation = new Quaternion(
            q.Value<float>("x"),
            q.Value<float>("y"),
            q.Value<float>("z"),
            q.Value<float>("w")
        );

        rosTranslation = translation;
        rosRotation = rotation;
    }

    private void ParseMessageAndCallHandler(string message)
    {
        JObject jsonMessage = JObject.Parse(message);
        Debug.Log($"jsonMessage: {message}");

        if (jsonMessage["op"]?.ToString() == "action_response" &&
            jsonMessage["response_type"]?.ToString() == "feedback" &&
            jsonMessage["name"]?.ToString() == "/navigate_to_pose")
        {
            var feedback = jsonMessage["values"]["feedback"];
            Debug.Log($"Nav action feedback jsonMessage: {message}");
            OnNavigateActionFeedbackReceived(feedback);
        }
        else if (
           jsonMessage["op"]?.ToString() == "action_response" &&
           jsonMessage["response_type"]?.ToString() == "result" &&
           jsonMessage["name"]?.ToString() == "/navigate_to_pose")
        {
            rosNavigationGoalFinished();
        }
        else if (
            jsonMessage["op"]?.ToString() == "action_response" &&
            jsonMessage["response_type"]?.ToString() == "feedback" &&
            jsonMessage["name"]?.ToString() == "/stretch_controller/follow_joint_trajectory")
        {
            var feedback = jsonMessage["values"]["feedback"];
            OnJointActionFeedbackReceived(feedback);
        }
        else if (
           jsonMessage["op"]?.ToString() == "action_response" &&
           jsonMessage["response_type"]?.ToString() == "result" &&
           jsonMessage["name"]?.ToString() == "/stretch_controller/follow_joint_trajectory")
        {
            rosJointTrajectoryGoalFinished();
        }
        else if (
           jsonMessage["op"]?.ToString() == "action_response" &&
           jsonMessage["response_type"]?.ToString() == "error" &&
           jsonMessage["name"]?.ToString() == "/stretch_controller/follow_joint_trajectory")
        {
            Debug.Log($"jsonMessage: {message}");
        }
        else if (jsonMessage["op"]?.ToString() == "publish" && jsonMessage["topic"]?.ToString() == "/navigate_to_pose/_action/status")
        {
            var msg = jsonMessage["msg"];
            OnNavigateActionStatusReceived(msg);
        }
        else if (jsonMessage["op"]?.ToString() == "publish" && jsonMessage["topic"]?.ToString() == "/stretch_controller/follow_joint_trajectory/_action/status")
        {
            var msg = jsonMessage["msg"];
            Debug.Log("JointTrajectoryActionStatusReceived message: " + message);
            OnFollowJointTrajectoryActionStatusReceived(msg);
        }
        else if (jsonMessage["op"]?.ToString() == "publish" && jsonMessage["topic"]?.ToString() == "/plan")
        {
            var path = jsonMessage["msg"];
            OnPathReceived(path);
        }
        else if (jsonMessage["op"]?.ToString() == "publish" && jsonMessage["topic"]?.ToString() == "/joint_states")
        {
            var joint_states = jsonMessage["msg"];
            OnJointStatesReceived(joint_states);
        }
        else if (jsonMessage["op"]?.ToString() == "publish" && jsonMessage["topic"]?.ToString() == "/base_pose")
        {
            var tf = jsonMessage["msg"];
            OnTFReceived(tf);
        }
        else
        {
            Debug.Log("Received unknown message: " + message);
        }

    }

    void OnNavigateActionFeedbackReceived(JToken feedback)
    {
        var timestampSec = (float)feedback["current_pose"]["header"]["stamp"]["sec"] +
                           (float)feedback["current_pose"]["header"]["stamp"]["nanosec"] / 1_000_000_000;
        var frameId = feedback["current_pose"]["header"]["frame_id"].ToString();
        var x = (float)feedback["current_pose"]["pose"]["position"]["x"];
        var y = (float)feedback["current_pose"]["pose"]["position"]["y"];
        var z = (float)feedback["current_pose"]["pose"]["position"]["z"];
        var qx = (float)feedback["current_pose"]["pose"]["orientation"]["x"];
        var qy = (float)feedback["current_pose"]["pose"]["orientation"]["y"];
        var qz = (float)feedback["current_pose"]["pose"]["orientation"]["z"];
        var qw = (float)feedback["current_pose"]["pose"]["orientation"]["w"];
    }


    void OnJointActionFeedbackReceived(JToken feedback)
    {

    }

    void OnPathReceived(JToken path)
    {
        var poses = path["poses"] as JArray;
        if (poses == null)
        {
            Debug.LogError("Invalid path data: 'poses' is not an array.");
            return;
        }

        int poseCount = poses.Count;
        pathPlan.positionCount = poseCount;

        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < poseCount; i++)
        {
            var pose = path["poses"][i];
            var frameId = pose["header"]["frame_id"].ToString();
            var timePlanned = (float)pose["header"]["stamp"]["sec"] +
                              (float)pose["header"]["stamp"]["nanosec"] / 1_000_000_000;
            var x = (float)pose["pose"]["position"]["x"];
            var y = (float)pose["pose"]["position"]["y"];
            var z = (float)pose["pose"]["position"]["z"];
            var qx = (float)pose["pose"]["orientation"]["x"];
            var qy = (float)pose["pose"]["orientation"]["y"];
            var qz = (float)pose["pose"]["orientation"]["z"];
            var qw = (float)pose["pose"]["orientation"]["w"];


            var positionRos = FrameConverterRosToUnity.ApplyFrameConventionConversionToPoint(new Vector3(x, y, z));
            Vector3 positionUnity = alignRobotUnity.GetRosToUnityPos(positionRos);

            positions.Add(positionUnity);
        }

        pathPlan.SetPositions(positions.ToArray());

    }

    void OnJointStatesReceived(JToken jointStates)
    {
        var names = jointStates["name"].ToObject<List<string>>();

        int jointRightWheelIndex = names.IndexOf("joint_right_wheel");
        int jointLeftWheelIndex = names.IndexOf("joint_left_wheel");
        int jointLiftIndex = names.IndexOf("joint_lift");
        int jointArmL3Index = names.IndexOf("joint_arm_l3");
        int jointArmL2Index = names.IndexOf("joint_arm_l2");
        int jointArmL1Index = names.IndexOf("joint_arm_l1");
        int jointArmL0Index = names.IndexOf("joint_arm_l0");
        int jointWristYawIndex = names.IndexOf("joint_wrist_yaw");
        int jointHeadPanIndex = names.IndexOf("joint_head_pan");
        int jointHeadTiltIndex = names.IndexOf("joint_head_tilt");
        int jointWristPitchIndex = names.IndexOf("joint_wrist_pitch");
        int jointWristRollIndex = names.IndexOf("joint_wrist_roll");
        int jointGripperFingerRightIndex = names.IndexOf("joint_gripper_finger_right");
        int jointGripperFingerLeftIndex = names.IndexOf("joint_gripper_finger_left");

        float jointRightWheel = (float)jointStates["position"][jointRightWheelIndex];
        float jointLeftWheel = (float)jointStates["position"][jointLeftWheelIndex];
        float jointLift = (float)jointStates["position"][jointLiftIndex];
        float jointArmL3 = (float)jointStates["position"][jointArmL3Index];
        float jointArmL2 = (float)jointStates["position"][jointArmL2Index];
        float jointArmL1 = (float)jointStates["position"][jointArmL1Index];
        float jointArmL0 = (float)jointStates["position"][jointArmL0Index];
        float jointWristYaw = (float)jointStates["position"][jointWristYawIndex];
        float jointHeadPan = (float)jointStates["position"][jointHeadPanIndex];
        float jointHeadTilt = (float)jointStates["position"][jointHeadTiltIndex];
        float jointWristPitch = (float)jointStates["position"][jointWristPitchIndex];
        float jointWristRoll = (float)jointStates["position"][jointWristRollIndex];
        float jointGripperFingerRight = (float)jointStates["position"][jointGripperFingerRightIndex];
        float jointGripperFingerLeft = (float)jointStates["position"][jointGripperFingerLeftIndex];

        Debug.Assert(Mathf.Abs(jointArmL0 - jointArmL1) < 0.0001f
            && Mathf.Abs(jointArmL1 - jointArmL2) < 0.0001f
            && Mathf.Abs(jointArmL2 - jointArmL3) < 0.0001f);

        Debug.Assert(Mathf.Abs(jointGripperFingerRight - jointGripperFingerLeft) < 0.0001f);

        stretchKinematicChain.SetAllJointStatesFromRosValues(jointLift, jointArmL0, jointWristYaw, jointWristPitch, jointWristRoll, jointGripperFingerLeft);

        string usedParams = $"Used: jointLift={jointLift}, wristExtension={jointArmL0}, jointWristYaw={jointWristYaw}, jointWristPitch={jointWristPitch}, jointWristRoll={jointWristRoll}, jointGripperFingerLeft={jointGripperFingerLeft}";
    }

    void OnNavigateActionStatusReceived(JToken json)
    {
        var statusList = json["status_list"].ToObject<List<JObject>>();

        statusList = statusList.OrderBy(x =>
            (long)x["goal_info"]["stamp"]["sec"] + (long)x["goal_info"]["stamp"]["nanosec"] / 1e9).ToList();

        var latestStatus = statusList.Last();
        var latestGoalId = latestStatus["goal_info"]["goal_id"]["uuid"].ToString();
        var latestStatusCode = (int)latestStatus["status"];


        /*
         * https://docs.ros2.org/foxy/api/action_msgs/msg/GoalStatus.html
        int8 STATUS_UNKNOWN = 0
        int8 STATUS_ACCEPTED = 1
        int8 STATUS_EXECUTING = 2
        int8 STATUS_CANCELING = 3
        int8 STATUS_SUCCEEDED = 4
        int8 STATUS_CANCELED = 5
        int8 STATUS_ABORTED = 6
        */

        if (latestStatusCode == 2)
        {
            rosNavigationGoalIDReceivedOrReReceived(latestGoalId);
        } else if (latestStatusCode == 4)
        {
            rosNavigationGoalFinished();
            rosNavigationGoalReached(latestGoalId);
        } else if (latestStatusCode == 5 || latestStatusCode == 6)
        {
            rosNavigationGoalCancelledOrAborted(latestGoalId);
        } else
        {
            throw new ArgumentOutOfRangeException($"navigation status code invalid: {latestStatusCode}");
        }
    }

    void OnFollowJointTrajectoryActionStatusReceived(JToken json)
    {
        var statusList = json["status_list"].ToObject<List<JObject>>();

        statusList = statusList.OrderBy(x =>
            (long)x["goal_info"]["stamp"]["sec"] + (long)x["goal_info"]["stamp"]["nanosec"] / 1e9).ToList();

        var latestStatus = statusList.Last();
        var latestGoalId = latestStatus["goal_info"]["goal_id"]["uuid"].ToString();
        var latestStatusCode = (int)latestStatus["status"];


        /*
         * https://docs.ros2.org/foxy/api/action_msgs/msg/GoalStatus.html
        int8 STATUS_UNKNOWN = 0
        int8 STATUS_ACCEPTED = 1
        int8 STATUS_EXECUTING = 2
        int8 STATUS_CANCELING = 3
        int8 STATUS_SUCCEEDED = 4
        int8 STATUS_CANCELED = 5
        int8 STATUS_ABORTED = 6
        */
        Debug.Log($"OnFollowJointTrajectoryActionStatusReceived: {statusList.ToArray()}");
        Debug.Log($"OnFollowJointTrajectoryActionStatusReceived: {latestGoalId}/{latestStatusCode}");

        if (latestStatusCode == 2)
        {
            rosJointTrajectoryGoalIDReceivedOrReReceived(latestGoalId);
        }
        else if (latestStatusCode == 4)
        {
            rosJointTrajectoryGoalFinished();
            rosJointTrajectoryGoalReached(latestGoalId);
        }
        else if (latestStatusCode == 5 || latestStatusCode == 6)
        {
            //rosJointTrajectoryGoalFinished();
            rosJointTrajectoryGoalCancelledOrAborted(latestGoalId);
        }
        else
        {
            throw new ArgumentOutOfRangeException($"joint trajectory status code invalid: {latestStatusCode}");
        }
    }

    async public void SendWebSocketMessage(string message)
    {
        // Debug.Log($"sending message:  {message}");
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(message);
        }
    }

    public void SendRunstopServiceCall(bool enable)
    {
        SendWebSocketMessage(GetRunstopServiceCall(enable));
    }

    public void NavigateToPose(Vector3 position, float yaw)
    {
        var positionRos = FrameConverterUnityToRos.ApplyFrameConventionConversionToPoint(position);
        SendWebSocketMessage(GetNavigateAction(positionRos.x, positionRos.y, 360 - yaw));
    }

    public void FollowJointTrajectoryToStow()
    {
        SendWebSocketMessage(GetJointTrajectoryAction(0.3f, 0.0f, 3.0f, -0.62f, 0.0f, 0.0f));
    }

    public void FollowJointTrajectoryToLowGrip()
    {
        SendWebSocketMessage(GetJointTrajectoryAction(0.3f, 0f, 0f, 0f, 0f, 0.0f));
    }

    public void FollowJointTrajectoryToMaxExtent()
    {
        SendWebSocketMessage(GetJointTrajectoryAction(1.04f, 0.48f, 0f, 0f, 0f, 0.0f));
    }

    public void FollowJointTrajectoryToCustom(float? lift, float? extension, float? yaw, float? pitch, float? roll, float? grip)
    {
        SendWebSocketMessage(GetJointTrajectoryAction(lift, extension, yaw, pitch, roll, grip));
    }

    public void SendCmdVelOneOff(float vx, float vy, float vz, float vrx, float vry, float vrz)
    {
        SendWebSocketMessage(GetCmdVelPublish(vx, vy, vz, vrx, vry, vrz));
    }

    public Pose GetRobotRosPose()
    {
        return new Pose(robotRosPos, robotRosRot);
    }


    void Update()
    {
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }

        if (triggerSubscribeToPlan)
        {
            SendWebSocketMessage(GetPlanSubscription());
            triggerSubscribeToPlan = false;
        }

        if (triggerSubscribeToJointState)
        {
            SendWebSocketMessage(GetJointStatesSubscription());
            triggerSubscribeToJointState = false;
        }

        if (triggerSendNavigateAction)
        {
            NavigateToPose(new Vector3(navigateGoal.x, navigateGoal.y, 0.0f), navigateGoal.z);
            triggerSendNavigateAction = false;
        }
        
        if (triggerCmdVel)
        {
            SendCmdVelOneOff(0f, 0f, 0f, 0f, 0f, navigateGoal.y);
            triggerCmdVel = false;
        }

        if (triggerFollowJointTrajectoryActionToStow)
        {
            FollowJointTrajectoryToStow();
            triggerFollowJointTrajectoryActionToStow = false;
        }

        if (triggerFollowJointTrajectoryActionToLow)
        {
            FollowJointTrajectoryToLowGrip();
            triggerFollowJointTrajectoryActionToLow = false;
        }

        if (triggerFollowJointTrajectoryActionToMax)
        {
            FollowJointTrajectoryToMaxExtent();
            triggerFollowJointTrajectoryActionToMax = false;
        }

        // Remaps ROS to Unity
        robotRosPos = FrameConverterRosToUnity.ApplyFrameConventionConversionToPoint(rosTranslation);
        robotRosRot = Quaternion.Euler(0.0f, -rosRotation.eulerAngles.z, 0.0f);
    }


    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}
