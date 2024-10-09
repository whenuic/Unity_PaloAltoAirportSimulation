using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AirportInfo {
  public string name;
  public float altitude; // in meter
}

public struct ApproachInfo {
  public string approach_name;
  public List<string> approach_waypoint_list;
  public float target_altitude;
  public string runway;
}

public struct RunwayInfo {
  public string name;
  public float width;
  public float length;
}

public struct PatternInfo {
  public float wind_direction; // where does the wind come from?
  public List<string> runway_in_use; // The set of runway name that can be used to land or take off
}

public struct GateInfo {
  public enum GateStatus {
    OCCUPIED = 0,
    RESERVED = 1,
    EMPTY = 2,
  }

  public GateStatus status;
  public string name;
  public GameObject gate_gameobject;
  public Vector3 direction; // the euler angles, the y component is of interest.

  public GameObject gate_entry_gameobject;
}

struct GroundWaypointConnection {
  // From the current point, it can connect to this named waypoint
  public string name;
  // At the current point, the direction of the tangent on the curve to the above named waypoint
  public int direction;
}

struct GroundWaypointInfo {
  public GroundWaypointInfo(string name, int id, List<GroundWaypointConnection> connection) {
    name_ = name;
    id_ = id;
    connection_ = connection;
  }

  public GroundWaypointInfo(string name, int id,
                            string next_name_1 = "", int to_next_direction_1 = -1,
                            string next_name_2 = "", int to_next_direction_2 = -1,
                            string next_name_3 = "", int to_next_direction_3 = -1,
                            string next_name_4 = "", int to_next_direction_4 = -1) {
    name_ = name;
    id_ = id;
    connection_ = new List<GroundWaypointConnection>();
    GroundWaypointConnection connection;
    if (to_next_direction_1 > 0) {
      connection.name = next_name_1;
      connection.direction = to_next_direction_1;
      connection_.Add(connection);
    }
    if (to_next_direction_2 > 0) {
      connection.name = next_name_2;
      connection.direction = to_next_direction_2;
      connection_.Add(connection);
    }
    if (to_next_direction_3 > 0) {
      connection.name = next_name_3;
      connection.direction = to_next_direction_3;
      connection_.Add(connection);
    }
    if (to_next_direction_4 > 0) {
      connection.name = next_name_4;
      connection.direction = to_next_direction_4;
      connection_.Add(connection);
    }
  }

  public string name_;
  public int id_;
  public List<GroundWaypointConnection> connection_;
}


public class AirportManager : MonoBehaviour {

  private List<RunwayInfo> runway_ = new();
  private AirportInfo airport_ = new();
  private GateInfo[] gates_ = new GateInfo[30];

  private PatternInfo pattern_ = new();

  private List<ApproachInfo> approaches_ = new();

  // Start is called before the first frame update
  void Start() {
    List<GroundWaypointInfo> ground_waypoint_info = new List<GroundWaypointInfo>();
    ground_waypoint_info.Add(new GroundWaypointInfo("Q1", 0, "Q2", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("Q2", 1, "Q1", 90 , "W4", 270, "W5", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("P1", 2, "P2", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("P2", 3, "P1", 90,  "W2", 270, "W3", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("R1", 4, "E2", 180, "R2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("R2", 5, "R1", 180, "R3", 0,   "C2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("R3", 6, "R2", 180, "R4", 0,   "C2", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("R4", 7, "R3", 180, "R5", 0,   "B2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("R5", 8, "R4", 180, "Runway31TakeoffSign", 0, "B2", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("R6", 9, "A2", 0,   "Runway31TakeoffSign", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("E1", 10, "E2", 270, "Y1", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("E2", 11, "R1", 270, "E1", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("C1", 12, "C2", 270, "Y2", 90, "Y3", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("C2", 13, "C1", 90, "R2", 270, "R3", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("B1", 14, "B2", 270, "Y4", 90, "Y5", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("B2", 15, "B1", 90, "R4", 270, "R5", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("A1", 16, "A2", 270, "EnterRunway31Sign", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("A2", 17, "A1", 90, "R6", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y1", 18, "E1", 180, "Y2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y2", 19, "Y1", 180, "Y3", 0, "C1", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y3", 20, "Y2", 180, "Y4", 0, "C1", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y4", 21, "Y3", 180, "Y5", 0, "B1", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y5", 22, "Y4", 180, "Y6", 0, "B1", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y6", 23, "Y5", 180, "Y7", 0, "Z2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y7", 24, "Y6", 180, "Y8", 0, "Z2", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y8", 25, "Y7", 180, "Y9", 0, "T2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y9", 26, "Y8", 180, "Y10", 0, "T2", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("Y10", 27, "Y9", 180, "EnterRunway31Sign", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("Z1", 28, "Z2", 270, "W1", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("Z2", 29, "Z1", 90, "Y6", 270, "Y7", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("W1", 30, "W2", 0, "Z1", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("W2", 31, "W1", 180, "W3", 0, "P2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("W3", 32, "W2", 180, "W4", 0, "P2", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("W4", 33, "W3", 180, "W5", 0, "Q2", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("W5", 34, "W4", 180, "W6", 0, "Q2", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("W6", 35, "W5", 180, "T1", 0));
    ground_waypoint_info.Add(new GroundWaypointInfo("T1", 36, "T2", 270, "W6", 90));
    ground_waypoint_info.Add(new GroundWaypointInfo("T2", 37, "T1", 90, "Y8", 270, "Y9", 270));
    ground_waypoint_info.Add(new GroundWaypointInfo("Runway31TakeoffSign", 38, "R6", 0, "R5", 180));
    ground_waypoint_info.Add(new GroundWaypointInfo("EnterRunway31Sign", 39, "Y10", 135, "A1", 315));

    // Initialize gates
    gates_[0] = new GateInfo();
    gates_[0].name = "P1";
    gates_[0].gate_gameobject = transform.Find("GroundWaypoints/P1").gameObject;
    gates_[0].status = GateInfo.GateStatus.EMPTY;
    gates_[0].direction = transform.Find("GroundWaypoints/P1").eulerAngles;
    gates_[0].gate_entry_gameobject = transform.Find("GroundWaypoints/P2").gameObject;

    gates_[1] = new GateInfo();
    gates_[1].name = "Q1";
    gates_[1].gate_gameobject = transform.Find("GroundWaypoints/Q1").gameObject;
    gates_[1].status = GateInfo.GateStatus.EMPTY;
    gates_[1].direction = transform.Find("GroundWaypoints/Q1").eulerAngles;
    gates_[1].gate_entry_gameobject = transform.Find("GroundWaypoints/Q2").gameObject;

    gates_[2] = new GateInfo();
    gates_[2].name = "S1";
    gates_[2].gate_gameobject = transform.Find("GroundWaypoints/S1").gameObject;
    gates_[2].status = GateInfo.GateStatus.EMPTY;
    gates_[2].direction = transform.Find("GroundWaypoints/S1").eulerAngles;
    gates_[2].gate_entry_gameobject = transform.Find("GroundWaypoints/S2").gameObject;

    gates_[3] = new GateInfo();
    gates_[3].name = "U1";
    gates_[3].gate_gameobject = transform.Find("GroundWaypoints/U1").gameObject;
    gates_[3].status = GateInfo.GateStatus.EMPTY;
    gates_[3].direction = transform.Find("GroundWaypoints/U1").eulerAngles;
    gates_[3].gate_entry_gameobject = transform.Find("GroundWaypoints/U2").gameObject;

    gates_[4] = new GateInfo();
    gates_[4].name = "V1";
    gates_[4].gate_gameobject = transform.Find("GroundWaypoints/V1").gameObject;
    gates_[4].status = GateInfo.GateStatus.EMPTY;
    gates_[4].direction = transform.Find("GroundWaypoints/V1").eulerAngles;
    gates_[4].gate_entry_gameobject = transform.Find("GroundWaypoints/V2").gameObject;

    gates_[5] = new GateInfo();
    gates_[5].name = "X1";
    gates_[5].gate_gameobject = transform.Find("GroundWaypoints/X1").gameObject;
    gates_[5].status = GateInfo.GateStatus.EMPTY;
    gates_[5].direction = transform.Find("GroundWaypoints/X1").eulerAngles;
    gates_[5].gate_entry_gameobject = transform.Find("GroundWaypoints/X2").gameObject;

    // Initialize pattern
    pattern_.wind_direction = 310.0f; // Wind coming from NW
    pattern_.runway_in_use = new List<string> { "31" };

    // Initialize approaches
    ApproachInfo approach_info = new();
    approach_info.approach_name = "A";
    approach_info.approach_waypoint_list = new List<string> { "B", "D", "D1", "A1", "A" };
    approach_info.target_altitude = 800.0f;
    approach_info.runway = "31";
    approaches_.Add(approach_info);
    approach_info.approach_name = "B";
    approach_info.approach_waypoint_list = new List<string> { "B", "D", "E", "E1", "F1", "F", "A" };
    approach_info.target_altitude = 800.0f;
    approach_info.runway = "31";
    approaches_.Add(approach_info);
    approach_info.approach_name = "C";
    approach_info.approach_waypoint_list = new List<string> { "A2", "A" };
    approach_info.target_altitude = 630.0f;
    approach_info.runway = "31";
    approaches_.Add(approach_info);
    approach_info.approach_name = "D";
    approach_info.approach_waypoint_list = new List<string> { "C", "G", "G1", "A2", "A" };
    approach_info.target_altitude = 1000.0f;
    approach_info.runway = "31";
    approaches_.Add(approach_info);

    // Initialize AirportInfo
    airport_.name = "PaloAlto";
    airport_.altitude = 11.0f * 0.3048f;

    // Initialize RunwayInfo
    RunwayInfo runway_info = new RunwayInfo();
    runway_info.name = "31";
    runway_info.width = 21.336f;
    runway_info.length = 744.626f;
    runway_.Add(runway_info);

    runway_info.name = "13";
    runway_info.width = 21.336f;
    runway_info.length = 744.626f;
    runway_.Add(runway_info);
  }

  public List<string> GetRunwayInUseList() {
    return pattern_.runway_in_use;
  }

  public float GetRunwayWidth(string approach_name_string) {
    string runway_name = "";
    foreach (var approach in approaches_) {
      if (approach.approach_name == approach_name_string) {
        runway_name = approach.runway;
      }
    }
    float width = 0;
    foreach (var runway in runway_) {
      if (runway.name == runway_name) {
        width = runway.width;
        break;
      }
    }
    return width;
  }

  public GateInfo.GateStatus GetGateStatus(string name) {
    int target_gate_index = 0;
    for (int i = 0; i < gates_.Length; i++) {
      if (gates_[i].name == name) {
        target_gate_index = i;
        break;
      }
    }
    return gates_[target_gate_index].status;
  }

  public void SetGateStatus(string name, GateInfo.GateStatus status) {
    int target_gate_index = 0;
    for (int i = 0; i < gates_.Length; i++) {
      if (gates_[i].name == name) {
        target_gate_index = i;
        break;
      }
    }
    gates_[target_gate_index].status = status;
  }

  public List<string> GetAvailableGates() {
    List<string> gates_available = new(); 
    foreach (var gate in gates_) {
      if (gate.status == GateInfo.GateStatus.EMPTY) {
        gates_available.Add(gate.name);
      }
    }
    return gates_available;
  }

  public GateInfo GetGateInfo(string name) {
    int target_gate_index = 0;
    for (int i = 0; i < gates_.Length; i++) {
      if (gates_[i].name == name) {
        target_gate_index = i;
        break;
      }
    }
    return gates_[target_gate_index];
  }

  public ApproachInfo GetApproachInfo(string approach_name) {
    for (int i = 0; i < approaches_.Count; i++) {
      if (approaches_[i].approach_name == approach_name) {
        return approaches_[i];
      }
    }
    return approaches_[0];
  }

  public AirportInfo GetAirportInfo() {
    return airport_;
  }

  // Update is called once per frame
  void Update() {
        
  }
}
