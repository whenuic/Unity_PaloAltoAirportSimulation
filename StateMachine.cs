using Crosstales.NAudio.Midi;
using Crosstales.RTVoice.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public delegate void StateTransitionDelegate(string from, string to);
public delegate void LoadingCompleteDelegate();
public delegate void RequestSpeakCompleteCallback();
public delegate void ResponseSpeakCompleteCallback();

public delegate void ExitChoosingDelegate(string exit);
public delegate void RunwayChoosingDelegate(string runway);
public delegate void ApproachChoosingDelegate(string approach);
public delegate void TaxiwayChoosingDelegate(bool is_automatic);

public delegate void SetHoldOnCommandInTaxiToRunwayState(bool value);
public delegate void SetTakeoffApproved(bool value);
public delegate void SetAlign(bool value);

public delegate void TaxiRaycastCollideCallback(float direction, RaycastHit hit);

public class LoadingHandler {
  public LoadingHandler(LoadingCompleteDelegate loading_complete_delegate) {
    loading_complete_delegate_ = loading_complete_delegate;
  }

  public void Update(float dt) {
    if (start_loading_) {
      loading_timer_ += dt;
      if (loading_timer_ >= 5.0f) {
        CompleteLoading();
        loading_complete_delegate_();
      }
    }
  }

  public void StartLoading() {
    loading_timer_ = 0.0f;
    start_loading_ = true;
  }

  public void CompleteLoading() {
    loading_timer_ = 0;
    start_loading_ = false;
  }

  private float loading_timer_ = 0.0f;
  private bool start_loading_ = false;
  private LoadingCompleteDelegate loading_complete_delegate_;
}

public struct StateNameToId {
  public StateNameToId(string state_name, int state_id) {
    name = state_name;
    id = state_id;
  }
  public string name;
  public int id;
}

public class State {
  public State(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport) {
    state_transition_delegate_ = state_transition_delegate;
    aircraft_ = aircraft;
    airport_ = airport;
    flight_control_ = aircraft_.GetComponent<FlightControl>();
    state_machine_ = aircraft_.GetComponent<StateMachine>();
  }
  public virtual string Name { get; } = "Base";
  public virtual int Id { get; } = 0;

  public virtual void Entry(string from) {
  }
  
  public virtual void Update(float dt) {
  }
  public virtual void Exit() {
  }

  public string GetStateInfo() {
    return state_info_;
  } 

  protected GameObject aircraft_;
  protected AirportManager airport_;
  protected FlightControl flight_control_;
  protected StateTransitionDelegate state_transition_delegate_;
  protected StateMachine state_machine_;

  protected string state_info_; // for banner display, maybe debug

}

public class InactiveState : State {
  public InactiveState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "Inactive";
  public override int Id { get; } = 0;

  public override void Entry(string from) {
    if (from == "Climb") {
      state_machine_.DestroyAircraft();
    }
    if (from == "TaxiToGate") {
      state_machine_.DestroyCard();
    }
    if (from == "Inactive") {
      state_machine_.SetGateOccupied();
    }
    state_machine_.SetIndicatorMaterial();
  }

  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    if (state_machine_.IsInbound()) {
      state_transition_delegate_(this.Name, "OnRadar");
      state_machine_.InstantiateCard(/*isIncoming*/true);
    } else {
      state_transition_delegate_(this.Name, "Loading");
      state_machine_.InstantiateCard(/*isIncoming*/false);
    }
    state_machine_.AssignSetCurrentByCardClickToCard();
  }

  public override void Exit() {
  }
}

public class LoadingState : State {
  public LoadingState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
    loading_handler_ = new LoadingHandler(this.CompleteLoading);
  }
  public override string Name { get; } = "Loading";
  public override int Id { get; } = 1;

  private LoadingHandler loading_handler_;

  public override void Entry(string from) {
    GateInfo gate_info = state_machine_.GetGateInfo(state_machine_.GetParkingGateName());
    Vector3 parking_position = gate_info.gate_gameobject.transform.position;
    parking_position.y = 3.283f;
    aircraft_.transform.position = parking_position;

    Vector3 euler_angles = aircraft_.transform.eulerAngles;
    euler_angles.y = gate_info.direction.y;
    Debug.Log("gate_direction = " + euler_angles.ToString());
    aircraft_.transform.eulerAngles = euler_angles;
    flight_control_.ApplyBrake();
    loading_handler_.StartLoading();
  }

  public void CompleteLoading() {
    state_transition_delegate_(Name, "DepartureRunwayRequest");
  }

  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    loading_handler_.Update(dt);
  }

  public override void Exit() {
  }
}

public class DepartureRunwayRequestState : State {
  public DepartureRunwayRequestState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "DepartureRunwayRequest";
  public override int Id { get; } = 2;

  public override void Entry(string from) {
    // Activate Dept Req button.
    state_machine_.GroundChannelDepartureRunwayRequest();
  }
  public override void Exit() {
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
  }
}
public class TaxiwayRequestState : State {
  public TaxiwayRequestState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "TaxiwayRequest";
  public override int Id { get; } = 3;

  public override void Entry(string from) {
    state_machine_.TaxiwayRequest();
  }
  public override void Exit() {
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
  }
}

public class PushbackRequestState : State {
  public PushbackRequestState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "PushbackRequest";
  public override int Id { get; } = 4;

  public override void Entry(string from) {
    //Activate Pushback Request button
    state_machine_.GroundChannelPushbackRequest();
  }
  public override void Exit() {
    // Start the engine
    if (!flight_control_.GetEngineSwitch()) {
      flight_control_.ToggleEngine();
    }
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");

  }
}

public class PushbackState : State {
  public PushbackState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "Pushback";
  public override int Id { get; } = 5;

  // From parking position, push straight back to the entry position, then make a turn. x,z values are used.
  private Vector3 gate_entry_position;
  // The direction of a perfectly parked aircraft heading direction.
  private Vector3 gate_direction;
  // The position where pushback complete. x,z values are used.
  private Vector3 pushback_target_position;
  // The aircraft heading direction after a perfect pushback completes. Can be also from the
  // pushback_target_direction to the first waypoint of the taxi to runway route.
  private Vector3 pushback_target_direction;

  private float distance_to_target;

  private bool straight_backup_completed = false;
  private bool hold_on_traffic_ = false;

  public override void Entry(string from) {
    // TODO: need to dertermine the four positions and directions
    // For now, just use hard coded value
    GateInfo gate_info = state_machine_.GetGateInfo(state_machine_.GetParkingGateName());
    gate_entry_position = gate_info.gate_entry_gameobject.transform.position;
    if (state_machine_.GetParkingGateName() == "P1") {
      pushback_target_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/W2").transform.position;
      pushback_target_direction = GameObject.Find("PaloAltoAirport/GroundWaypoints/W3").transform.position
                                  - GameObject.Find("PaloAltoAirport/GroundWaypoints/W2").transform.position;
    } else if (state_machine_.GetParkingGateName() == "Q1") {
      pushback_target_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/W4").transform.position;
      pushback_target_direction = GameObject.Find("PaloAltoAirport/GroundWaypoints/W5").transform.position
                                  - GameObject.Find("PaloAltoAirport/GroundWaypoints/W4").transform.position;
    } else if (state_machine_.GetParkingGateName() == "S1") {
      pushback_target_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/W6").transform.position;
      pushback_target_direction = GameObject.Find("PaloAltoAirport/GroundWaypoints/W7").transform.position
                                  - GameObject.Find("PaloAltoAirport/GroundWaypoints/W6").transform.position;
    } else if (state_machine_.GetParkingGateName() == "U1") {
      pushback_target_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/W8").transform.position;
      pushback_target_direction = GameObject.Find("PaloAltoAirport/GroundWaypoints/W9").transform.position
                                  - GameObject.Find("PaloAltoAirport/GroundWaypoints/W8").transform.position;
    } else if (state_machine_.GetParkingGateName() == "X1") {
      pushback_target_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/W10").transform.position;
      pushback_target_direction = GameObject.Find("PaloAltoAirport/GroundWaypoints/W11").transform.position
                                  - GameObject.Find("PaloAltoAirport/GroundWaypoints/W10").transform.position;
    } else if (state_machine_.GetParkingGateName() == "V1") {
      pushback_target_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/W12").transform.position;
      pushback_target_direction = GameObject.Find("PaloAltoAirport/GroundWaypoints/W13").transform.position
                                  - GameObject.Find("PaloAltoAirport/GroundWaypoints/W12").transform.position;
    }
    gate_direction = gate_info.gate_gameobject.transform.position - gate_info.gate_entry_gameobject.transform.position;
    distance_to_target = 0;

    state_machine_.SetGroundRadar(true, false);
    if (from == "HoldOnTraffic") {
      hold_on_traffic_ = false;
    }
  }
  public override void Exit() {
    state_machine_.SetGroundRadar(false, true);
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");

    List<GameObject> collision_objects = state_machine_.GetCollisionTrackingObjects();
    if (collision_objects != null && collision_objects.Count > 0) {
      Debug.Log("Checking collision objects.");
      foreach (GameObject aircraft in collision_objects) {
        float distance = (aircraft.transform.position - aircraft_.transform.position).magnitude;
        Debug.Log("That aircraft is " + distance + " away.");
        string current_state_name = aircraft.GetComponent<StateMachine>().GetCurrentStateName();
        Debug.Log("That aircraft is in " + current_state_name + " state.");
        if (current_state_name == "Inactive" ||
             current_state_name == "Loading" ||
             current_state_name == "DepartureRunwayRequest" ||
             current_state_name == "PushbackRequest") {
          continue;
        }
        hold_on_traffic_ = true;
        Debug.Log(state_machine_.GetTailName() + ": hold on traffic" +
                  aircraft.GetComponent<StateMachine>().GetTailName() + "@" + current_state_name);
        state_machine_.SetHoldOnTrafficObject(aircraft);
        break;
      }
    }

    if (hold_on_traffic_) {
      state_transition_delegate_.Invoke(this.Name, "HoldOnTraffic");
    }

    if (!straight_backup_completed) {
      aircraft_.transform.position += (-gate_direction.normalized) * dt;
      Vector3 diff = gate_entry_position - aircraft_.transform.position;
      diff.y = 0;
      if (diff.magnitude < 0.2f) {
        straight_backup_completed = true;
        Vector3 diff_to_target = aircraft_.transform.position - pushback_target_position;
        diff_to_target.y = 0;
        distance_to_target = diff_to_target.magnitude;
      }
    } else {
      aircraft_.transform.position += (-aircraft_.transform.forward.normalized * dt);
      aircraft_.transform.Rotate(0, -dt * 4.0f, 0);
      Vector3 diff = aircraft_.transform.position - pushback_target_position;
      diff.y = 0;
      if (diff.magnitude < 1.0f || diff.magnitude >= distance_to_target) {
        state_machine_.SetGateEmpty();
        state_transition_delegate_.Invoke(this.Name, "TaxiToRunway");
      }
      distance_to_target = diff.magnitude;
    }
  }
}

public class TaxiToRunwayState : State {
  public TaxiToRunwayState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "TaxiToRunway";
  public override int Id { get; } = 6;

  private List<string> departure_route_ = new List<string>();
  private Vector3 next_waypoint_position = Vector3.zero;

  private float target_taxi_speed_ = 0.0f;

  private bool hold_on_command_ = false;
  private bool hold_on_enter_runway_ = false;
  private bool hold_on_traffic_ = false;
  private bool takeoff_approved_ = false;
  private bool align_ = false;

  private float steer_input_ = 0.0f;
  private float steer_input_last_ = 0.0f;
  private float steer_input_last_last_ = 0.0f;
  private float angle_diff_ = 0.0f;
  private float angle_diff_last_ = 0.0f;
  private float angle_diff_last_last_ = 0.0f;

  public void SetHoldOnCommand(bool value) {
    hold_on_command_ = value;
  }

  public void SetTakeoffApproved(bool approved) {
    takeoff_approved_= approved;
  }

  public void SetAlign(bool align) {
    align_ = align;
  }

  public override void Entry(string from) {
    if (from == "Pushback") {
      aircraft_.transform.Translate(new Vector3(0, 0.1f, 0));
      departure_route_ = state_machine_.GetDepartureRoute();
    }

    // No matter which state is transitted from, hold_on_command_ should be reset to false.
    hold_on_command_ = false;
    hold_on_enter_runway_ = false;

    if (from == "HoldOnTraffic") {
      hold_on_traffic_ = false;
    }

    next_waypoint_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/" + departure_route_[0]).transform.position;
    next_waypoint_position.y = 0;

    // Pop up hold on command button.
    state_machine_.SetHoldOnCommandButtonActive(true, this.SetHoldOnCommand, this.Name);

    state_machine_.SetGroundRadar(true, true);
  }
  public override void Exit() {
    state_machine_.SetGroundRadar(false, true);
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    List<GameObject> collision_objects = state_machine_.GetCollisionTrackingObjects();
    if (collision_objects != null && collision_objects.Count > 0) {
      Debug.Log("Checking collision objects.");
      foreach (GameObject aircraft in collision_objects) {
        float distance = (aircraft.transform.position - aircraft_.transform.position).magnitude;
        Debug.Log("That aircraft is " + distance + " away.");

        string current_state_name = aircraft.GetComponent<StateMachine>().GetCurrentStateName();
        Debug.Log("That aircraft is in " + current_state_name + " state.");
        if (current_state_name == "Inactive" ||
             current_state_name == "Loading" ||
             current_state_name == "DepartureRunwayRequest" ||
             current_state_name == "PushbackRequest") {
          continue;
        }
        hold_on_traffic_ = true;
        Debug.Log(state_machine_.GetTailName() + ": hold on traffic" +
                  aircraft.GetComponent<StateMachine>().GetTailName() + "@" + current_state_name);
        state_machine_.SetHoldOnTrafficObject(aircraft);
        break;
      }
    }


    Vector3 current_rotation = aircraft_.transform.forward;
    current_rotation.y = 0;
    current_rotation.Normalize();
    Vector3 target_rotation = next_waypoint_position - aircraft_.transform.position;
    target_rotation.y = 0;
    target_rotation.Normalize();
    angle_diff_ = Vector3.Angle(current_rotation, target_rotation);
    
    float cross_product_dot_y = Vector3.Cross(current_rotation, target_rotation).y;
    if (cross_product_dot_y > 0) {
      angle_diff_ *= -1.0f;
    }

    float kp = 40.0f;
    float ki = 0.0f;
    float kd = 0.0f;

    steer_input_ = steer_input_ + (kp + ki * dt + kd / dt) * (angle_diff_) +
                                  (-kp - 2.0f * kd / dt) * (angle_diff_last_) +
                                  kd / dt * (angle_diff_last_last_);


    angle_diff_last_last_ = angle_diff_last_;
    angle_diff_last_ = angle_diff_;

    flight_control_.SetSteerInput(Mathf.Clamp(steer_input_, -1.0f, 1.0f));
    

    Vector3 position_diff = aircraft_.transform.position - next_waypoint_position;
    position_diff.y = 0;

    if (position_diff.magnitude < 10.0f) {
      if (Mathf.Abs(angle_diff_) > 2.0f) {
        target_taxi_speed_ = 2.0f;
      } else {
        target_taxi_speed_ = 4.0f;
      }
    } else {
      if (Mathf.Abs(angle_diff_) > 2.0f ) {
        target_taxi_speed_ = 3.0f;
      } else {
        target_taxi_speed_ = 15.0f;
      }
    }
    
    if (position_diff.magnitude < 5.0f) {
      if (departure_route_[0] == "EnterRunway31Sign") {
        hold_on_enter_runway_ = true;
      }
      if (departure_route_[0] == "Runway31TakeoffSign") {
        if (takeoff_approved_) {
          state_transition_delegate_.Invoke(this.Name, "Takeoff");
        } else {
          state_transition_delegate_.Invoke(this.Name, "HoldOnTakeoff");
        }
        departure_route_.Clear();
        state_machine_.SetHoldOnCommandButtonActive(false, null, this.Name);
        return;
      }
      departure_route_.RemoveAt(0);
      next_waypoint_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/" + departure_route_[0]).transform.position;

    }

    // When hold button is pressed, the aircraft will not stop immediately. If a waypoint is passed before the aircraft
    // stops, we still need to track the removal of the waypoint. So after the aircraft completely stops, we transit
    // the state to HoldOnCommand.
    if (hold_on_command_ || hold_on_enter_runway_ || hold_on_traffic_) {
      Debug.Log("Speed is: " + flight_control_.GetSpeed().ToString());
      target_taxi_speed_ = 0.0f;
      if (flight_control_.GetSpeed() < 0.001f) {
        if (hold_on_command_) {
          state_transition_delegate_.Invoke(this.Name, "HoldOnCommand");
        }
        if (hold_on_traffic_) {
          state_transition_delegate_.Invoke(this.Name, "HoldOnTraffic");
        }
        if (hold_on_enter_runway_) {
          state_machine_.SetTakeoffButtonActive(true, this.SetTakeoffApproved);
          state_machine_.SetAlignButtonActive(true, this.SetAlign);
          state_transition_delegate_.Invoke(this.Name, "HoldOnEnterRunway");
        }
      }
    }

    if (flight_control_.GetSpeed() < target_taxi_speed_) {
      flight_control_.ThrottleUp(dt / 3.0f);
      flight_control_.ReleaseBrake();
    } else {
      flight_control_.ThrottleDown(dt);
      flight_control_.ApplyBrake();
    }


    // Debug.Log("Angle_diff = " + angle_diff_ + " Steer_input = " + steer_input_ + " Target_speed = " + target_taxi_speed_);

    int flap_degree_table_pointer;
    if (flight_control_.GetFlapDegreeTablePointer() < 2) {
      flap_degree_table_pointer = flight_control_.ExtendFlap();
    } else if (flight_control_.GetFlapDegreeTablePointer() > 2) {
      flap_degree_table_pointer = flight_control_.RetreatFlap();
    }
  }
}

public class HoldOnCommandState : State {
  public HoldOnCommandState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "HoldOnCommand";
  public override int Id { get; } = 7;

  private string from_state_;

  public override void Entry(string from) {
    from_state_ = from;
  }
  public override void Exit() {
    flight_control_.ReleaseBrake();
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    if (flight_control_.GetSpeed() > 0.0f) {
      flight_control_.ThrottleDown(dt);
      flight_control_.ApplyBrake();
    }
  }
}

public class HoldOnTrafficState : State {
  public HoldOnTrafficState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "HoldOnTraffic";
  public override int Id { get; } = 8;

  private string from_state_;
  private GameObject hold_on_traffic_object_;

  public override void Entry(string from) {
    from_state_ = from;
    if (from == "ExitRunway" || from == "TaxiToRunway" || from == "Pushback") {
      hold_on_traffic_object_ = state_machine_.GetHoldOnTrafficObject();
    }
  }
  public override void Exit() {
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    string hold_on_traffic_object_state_name = hold_on_traffic_object_.gameObject.GetComponent<StateMachine>().GetCurrentStateName();

    float distance = (aircraft_.transform.position - hold_on_traffic_object_.transform.position).magnitude;
    state_info_ = "Hold on:" + hold_on_traffic_object_.gameObject.GetComponent<StateMachine>().GetTailName();
    // raycast range = 40, +5 for the margin
    if (distance >= 40 + 5 || hold_on_traffic_object_state_name == "Inactive") {
      state_machine_.RemoveObjectFromCollisionTrackingObjects(hold_on_traffic_object_);
      state_machine_.SetHoldOnTrafficObject(null);
      if (from_state_ == "ExitRunway") {
        state_transition_delegate_.Invoke(this.Name, "ExitRunway");
      }
      if (from_state_ == "TaxiToRunway") {
        state_transition_delegate_.Invoke(this.Name, "TaxiToRunway");
      }
      if (from_state_ == "Pushback") {
        state_transition_delegate_.Invoke(this.Name, "Pushback");
      }
    }
  }
}

public class HoldOnCrossingState : State {
  public HoldOnCrossingState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "HoldOnCrossing";
  public override int Id { get; } = 9;

  public override void Entry(string from) {
  }
  public override void Exit() {
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
  }
}

public class HoldOnEnterRunwayState : State {
  public HoldOnEnterRunwayState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "HoldOnEnterRunway";
  public override int Id { get; } = 10;

  public override void Entry(string from) {
    // Pop up Takeoff and Align button.
  }
  public override void Exit() {
  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
  }
}

public class HoldOnTakeoffState : State {
  public HoldOnTakeoffState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "HoldOnTakeoff";
  public override int Id { get; } = 11;

  public override void Entry(string from) {
    state_machine_.SetTakeoffButtonFromHoldOnTakeoffState();
  }
  public override void Exit() {
    flight_control_.ReleaseBrake();

  }
  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    if (flight_control_.GetSpeed() > 0.0f) {
      flight_control_.ThrottleDown(dt);
      flight_control_.ApplyBrake();
    }
  }
}

public class TakeoffState : State {
  public TakeoffState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "Takeoff";

  public override int Id { get; } = 12;

  private float angle_diff_;
  private float angle_diff_last_;
  private float angle_diff_last_last_;
  private float steer_input_;

  public override void Entry(string from) {
    flight_control_.ReleaseBrake();
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    Vector3 current_rotation = aircraft_.transform.forward;
    current_rotation.y = 0;
    current_rotation.Normalize();
    Vector3 target_rotation = GameObject.Find("PaloAltoAirport/GroundWaypoints/R1").gameObject.transform.position
                              - aircraft_.transform.position;
    target_rotation.y = 0;
    target_rotation.Normalize();
    angle_diff_ = Vector3.Angle(current_rotation, target_rotation);

    float cross_product_dot_y = Vector3.Cross(current_rotation, target_rotation).y;
    if (cross_product_dot_y > 0) {
      angle_diff_ *= -1.0f;
    }

    float kp = 40.0f;
    float ki = 0.0f;
    float kd = 0.0f;
    steer_input_ = steer_input_ +
                        (kp + ki * dt + kd / dt) * (angle_diff_) +
                        (-kp - 2.0f * kd / dt) * (angle_diff_last_) +
                        kd / dt * (angle_diff_last_last_);

    angle_diff_last_last_ = angle_diff_last_;
    angle_diff_last_ = angle_diff_;

    flight_control_.SetSteerInput(Mathf.Clamp(steer_input_, -1.0f, 1.0f));

    if (flight_control_.GetThrustInput() < 1.0f) {
      flight_control_.ThrottleUp(dt);
    }

    if (!flight_control_.IsGrounded()) {
      state_transition_delegate_.Invoke(this.Name, "Climb");
    }
  }
}

public class ClimbState : State {
  public ClimbState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "Climb";

  public override int Id { get; } = 13;

  private int climb_altitude_;
  private int climb_vertical_rate_;
  private int auto_heading_value_;

  public override void Entry(string from) {

    // TODO: these three variables should come from airport.
    climb_altitude_ = 1148;
    climb_vertical_rate_ = 1000;
    auto_heading_value_ = 322;

    flight_control_.SetAutoHeadingValue(auto_heading_value_);
    flight_control_.SetAutoAltitudeValue(climb_altitude_);
    flight_control_.SetAutoVerticalRateValue(climb_vertical_rate_);
    flight_control_.SetAutoHeadingSwitch(true);
    flight_control_.SetAutoAltitudeSwitch(true);
    flight_control_.SetAutoVerticalRateSwitch(true);
    flight_control_.SetAutopilotSwitch(true);

    state_machine_.ResetExitDetection(); // Reset exit detection since taking off will also collide these detectors.
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    // Debug.Log(Name + " update.");

    if (flight_control_.GetAltitude() > 750.0f) {
      int flap_degree_table_pointer;
      if (flight_control_.GetFlapDegreeTablePointer() > 0) {
        flap_degree_table_pointer = flight_control_.RetreatFlap();
      }
    }

    if (flight_control_.GetAltitude() >= climb_altitude_) {
      state_transition_delegate_.Invoke(this.Name, "Inactive");
    }
  }

}

public class OnRadarState : State {
  public OnRadarState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "OnRadar";

  public override int Id { get; } = 14;

  public override void Entry(string from) {
    if (!flight_control_.GetEngineSwitch()) {
      flight_control_.ToggleEngine();
    }
    flight_control_.SetThrustInput(1.0f);
    if (state_machine_.IsInbound()) {
      flight_control_.SetAutoHeadingValue(
        (int)state_machine_.GetInitialAutoHeading());
      flight_control_.SetAutoAltitudeValue(
        (int)state_machine_.GetInitialAutoAltitude());
      flight_control_.SetAutoHeadingSwitch(true);
      flight_control_.SetAutoAltitudeSwitch(true);
      flight_control_.SetAutopilotSwitch(true);
    }
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    state_transition_delegate_(this.Name, "ArrivalRunwayRequest");
  }
}

public class ArrivalRunwayRequestState : State {
  public ArrivalRunwayRequestState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "ArrivalRunwayRequest";

  public override int Id { get; } = 15;

  public override void Entry(string from) {
    state_machine_.ArrivalRunwayRequest();
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
  }
}

public class ApproachRequestState : State {
  public ApproachRequestState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "ApproachRequest";

  public override int Id { get; } = 16;

  public override void Entry(string from) {
    state_machine_.ApproachRequest();
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    
  }
}

public class ToFinalState : State {
  public ToFinalState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }

  private List<string> approach_route_ = new List<string>();
  private float target_altitude_;

  private float xz_distance_to_head_ = 0.0f;
  private Vector3 waypoint_head_position_;
  private float check_stuck_timer_ = 0.0f;

  public override string Name { get; } = "ToFinal";

  public override int Id { get; } = 17;

  public override void Entry(string from) {    
    ApproachInfo approach_info = state_machine_.GetApproachInfo();
    approach_route_ = new List<string>(approach_info.approach_waypoint_list);
    Debug.Log("Approach route size = " + approach_route_.Count);
    target_altitude_ = approach_info.target_altitude;
    flight_control_.SetAutoAltitudeValue((int)target_altitude_);

    waypoint_head_position_ = GameObject.Find("PaloAltoAirport/Runway31LandingWaypoints/" + approach_route_[0]).transform.position;
    Vector3 diff = aircraft_.transform.position - waypoint_head_position_;
    diff.y = 0;
    xz_distance_to_head_ = diff.magnitude;
    check_stuck_timer_ = 0.0f;
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    check_stuck_timer_ += dt;

    if (flight_control_.GetFlapDegreeTablePointer() < 3) {
      flight_control_.ExtendFlap();
    }

    if (flight_control_.GetTrim() > -3.18f) {
      flight_control_.TrimDecrease(dt);
    } else if (flight_control_.GetTrim() < -3.22f) {
      flight_control_.TrimIncrease(dt);
    }

    if (flight_control_.GetSpeed() > 57.5f) {
      flight_control_.ThrottleDown(dt/2);
    } else if (flight_control_.GetSpeed() < 56.5f) {
      flight_control_.ThrottleUp(dt/2);
    }

    if (approach_route_.Count > 0) {
      Vector3 diff = waypoint_head_position_ - aircraft_.transform.position;
      diff.y = 0;
      float dist = diff.magnitude;

      // break looping the target point OR too close to the target point
      if ((check_stuck_timer_ > 30.0f && dist >= xz_distance_to_head_) || dist < 300.0f) {
        approach_route_.RemoveAt(0);
        if (approach_route_.Count == 0) {
          state_transition_delegate_(this.Name, "OnFinal");
          return;
        }
        waypoint_head_position_ = GameObject.Find("PaloAltoAirport/Runway31LandingWaypoints/" + approach_route_[0]).transform.position;
        check_stuck_timer_ = 0.0f;

        Vector3 updated_diff = aircraft_.transform.position - waypoint_head_position_;
        updated_diff.y = 0;
        xz_distance_to_head_ = updated_diff.magnitude;
      }

      if (dist < xz_distance_to_head_) {
        xz_distance_to_head_ = dist;
        check_stuck_timer_ = 0.0f;
      }

      float angle = Vector3.SignedAngle(Vector3.forward, diff, Vector3.up);
      if (angle < 0) {
        angle += 360;
      }
      flight_control_.SetAutoHeadingValue((int) angle);
      flight_control_.SetAutoAltitudeValue((int)(waypoint_head_position_.y / 0.3048f));
      return;
    }
    state_transition_delegate_(this.Name, "OnFinal");
  }

}

public class OnFinalState : State {
  public struct DescendingState {
    public float rate; // vertical rate, auxiliary for computing d_rate

    public float speed_diff; // actual - ideal
    public float alt_diff; // actual glide path angle - ideal glide path angle
    public float rate_diff; // actual - ideal
    public float d_rate; // (rate - rate_last) / dt
    public float d_thrust; // delta of thrust at current step
    public float d_elevator; // delta of elevator at current step
  }

  public OnFinalState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "OnFinal";

  public override int Id { get; } = 18;

  private GameObject descending_target_;
  private GameObject runway_end_;

  private DescendingState[] descending_state_ = new DescendingState[2]; // 0 is current, 1 is last step

  private float ideal_descending_rate_;

  private float final_descending_control_update_timer;
  private float final_descending_control_update_interval;

  private float runway_width_;

  private Dictionary<int[], float> throttle_lookup_ = new (new MyEqualityComparer());

  public class MyEqualityComparer : IEqualityComparer<int[]> {
    public bool Equals(int[] x, int[] y) {
      if (x.Length != y.Length) {
        return false;
      }
      for (int i = 0; i < x.Length; i++) {
        if (x[i] != y[i]) {
          return false;
        }
      }
      return true;
    }

    public int GetHashCode(int[] obj) {
      int result = 17;
      for (int i = 0; i < obj.Length; i++) {
        unchecked {
          result = result * 23 + obj[i];
        }
      }
      return result;
    }
  }

  public override void Entry(string from) {
    descending_target_ = GameObject.Find("PaloAltoAirport/Runway31LandingWaypoints/DescendingTarget").gameObject;
    runway_end_ = GameObject.Find("PaloAltoAirport/Runway31LandingWaypoints/RunwayEnd").gameObject;
    final_descending_control_update_interval = 0.5f;
    final_descending_control_update_timer = 0.0f;

    flight_control_.SetAutoAltitudeSwitch(false);
    flight_control_.SetAutoVerticalRateSwitch(false);

    runway_width_ = airport_.GetRunwayWidth(state_machine_.GetApproachChosen());

    // kts to m/minute * sin(3 degrees)
    ideal_descending_rate_ = -57.0f * 60.0f * 0.5144444f * Mathf.Sin(3.0f * Mathf.PI / 180.0f) * 3.28084f;

    // High/low, descend/ascend, path correction fast/ok/slow/never, drate descend speedup/keep/slowdown
    //  1  / 0 ,    1   /   0  ,             1  /0/ -1/  -100 ,                  1   / 0  / -1
    throttle_lookup_.Add(new int[] { 1, 1, 1, 1}, 1.0f);
    throttle_lookup_.Add(new int[] { 1, 1, 1, 0}, 0.5f);
    throttle_lookup_.Add(new int[] { 1, 1, 1, -1}, 0.0f);
    throttle_lookup_.Add(new int[] { 1, 1, 0, 1}, 0.5f);
    throttle_lookup_.Add(new int[] { 1, 1, 0, 0}, 0.0f);
    throttle_lookup_.Add(new int[] { 1, 1, 0, -1}, -0.5f);
    throttle_lookup_.Add(new int[] { 1, 1, -1, 1}, -0.5f); // change from 0 to -0.5
    throttle_lookup_.Add(new int[] { 1, 1, -1, 0}, -1.0f); // change from -0.5 to -1
    throttle_lookup_.Add(new int[] { 1, 1, -1, -1}, -1.0f);
    throttle_lookup_.Add(new int[] { 1, 1, -100, 1}, -0.5f);
    throttle_lookup_.Add(new int[] { 1, 1, -100, 0}, -1.0f);
    throttle_lookup_.Add(new int[] { 1, 1, -100, -1}, -1);
    throttle_lookup_.Add(new int[] { 1, 0, 1, 1}, -100);  // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, 1, 0}, -100);  // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, 1, -1}, -100); // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, 0, 1}, -100);  // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, 0, 0}, -100);  // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, 0, -1}, -100); // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, -1, 1}, -100); // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, -1, 0}, -100); // error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, -1, -1}, -100);// error state, too high, ascend, so can't be corrected
    throttle_lookup_.Add(new int[] { 1, 0, -100, 1}, -0.5f);
    throttle_lookup_.Add(new int[] { 1, 0, -100, 0}, -1);
    throttle_lookup_.Add(new int[] { 1, 0, -100, -1}, -1);
    throttle_lookup_.Add(new int[] { 0, 1, 1, 1}, 0.5f);
    throttle_lookup_.Add(new int[] { 0, 1, 1, 0}, 0);
    throttle_lookup_.Add(new int[] { 0, 1, 1, -1}, -0.5f);
    throttle_lookup_.Add(new int[] { 0, 1, 0, 1}, 0.5f);
    throttle_lookup_.Add(new int[] { 0, 1, 0, 0}, 0);
    throttle_lookup_.Add(new int[] { 0, 1, 0, -1}, -0.5f);
    throttle_lookup_.Add(new int[] { 0, 1, -1, 1}, 1.0f);
    throttle_lookup_.Add(new int[] { 0, 1, -1, 0}, 1.0f); // change from 0.5 to 1
    throttle_lookup_.Add(new int[] { 0, 1, -1, -1}, 0.5f);
    throttle_lookup_.Add(new int[] { 0, 1, -100, 1}, 1);
    throttle_lookup_.Add(new int[] { 0, 1, -100, 0}, 1);
    throttle_lookup_.Add(new int[] { 0, 1, -100, -1}, 2.0f); // change from 0.5 to 2.0
    throttle_lookup_.Add(new int[] { 0, 0, 1, 1}, -0.5f);
    throttle_lookup_.Add(new int[] { 0, 0, 1, 0}, -0.5f);
    throttle_lookup_.Add(new int[] { 0, 0, 1, -1}, -1);
    throttle_lookup_.Add(new int[] { 0, 0, 0, 1}, 0);
    throttle_lookup_.Add(new int[] { 0, 0, 0, 0}, -0.5f);
    throttle_lookup_.Add(new int[] { 0, 0, 0, -1}, -1);
    throttle_lookup_.Add(new int[] { 0, 0, -1, 1}, 0.5f);
    throttle_lookup_.Add(new int[] { 0, 0, -1, 0}, 0.5f);
    throttle_lookup_.Add(new int[] { 0, 0, -1, -1}, 0.0f);
    throttle_lookup_.Add(new int[] { 0, 0, -100, 1}, -100);  // error state, too low, ascend, so can't be never corrected
    throttle_lookup_.Add(new int[] { 0, 0, -100, 0}, -100);  // error state, too low, ascend, so can't be never corrected
    throttle_lookup_.Add(new int[] { 0, 0, -100, -1}, -100); // error state, too low, ascend, so can't be never corrected
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    final_descending_control_update_timer += dt;

    Vector3 position_diff = aircraft_.transform.position - descending_target_.transform.position;
    position_diff.y = 0;
    float distance = position_diff.magnitude;
    float ideal_altitude = 0.05240777928f * distance / 0.3048f + 5;
    float predict_time = position_diff.magnitude / flight_control_.GetSpeed() / 0.5144444f / 60.0f;

    descending_state_[1] = descending_state_[0];
    descending_state_[0].rate = flight_control_.GetVerticalRateDisplay();
    descending_state_[0].speed_diff = flight_control_.GetSpeed() - 57;
    descending_state_[0].alt_diff = flight_control_.GetAltitude() - ideal_altitude;
    descending_state_[0].rate_diff = flight_control_.GetVerticalRateDisplay() - ideal_descending_rate_;
    descending_state_[0].d_rate = (descending_state_[0].rate - descending_state_[1].rate) / dt;

    float correction_time = descending_state_[0].alt_diff / (-descending_state_[0].rate_diff) * 60.0f; // vrate is ft/min, we need to find sec.

    // Strategy 3: an improved version of strategy 2
    string debug_str = state_machine_.GetTailName();
    debug_str += ": Alt=";
    debug_str += flight_control_.GetAltitude().ToString("F0");
    debug_str += " Vrt=";
    debug_str += flight_control_.GetVerticalRateDisplay().ToString("F0");
    debug_str += " Dis=";
    debug_str += distance.ToString("F0");
    debug_str += ". ";
    int high_low = flight_control_.GetAltitude() >= ideal_altitude ? 1 : 0;
    int descend_ascend = flight_control_.GetVerticalRateDisplay() < 0 ? 1 : 0;
    debug_str += (high_low == 1 ? "TooHigh. " : "TooLow. ");
    debug_str += (descend_ascend == 1 ? "Descend. " : "Ascend. ");
    int correction_fast_or_slow = 0;
    if (correction_time > 0 && correction_time <= 10.0f) {
      correction_fast_or_slow = 1;
      debug_str += "Correction too fast=";
    } else if (correction_time > 20.0f) {
      correction_fast_or_slow = -1;
      debug_str += "Correction too slow=";
    } else if (correction_time <= 0) {
      correction_fast_or_slow = -100;
      debug_str += "Correction impossible=";
    } else {
      correction_fast_or_slow = 0;
      debug_str += "Correction OK=";
    }
    debug_str += correction_time.ToString("F1");
    debug_str += ". ";
    int drate_speed_up_or_slow_down = 0;
    if (descending_state_[0].d_rate < -10.0f) {
      drate_speed_up_or_slow_down = 1;
      debug_str += "Descend speed up. ";
    } else if (descending_state_[0].d_rate > 10.0f) {
      drate_speed_up_or_slow_down = -1;
      debug_str += "Descend slow down. ";
    } else {
      drate_speed_up_or_slow_down = 0;
      debug_str += "Descend rate keep. ";
    }

    if ((high_low == 0 && flight_control_.GetSpeed() > 58) || flight_control_.GetVerticalRateDisplay() < -450.0f) {
      flight_control_.SetPitchInput(-0.05f * Mathf.Clamp(Mathf.Abs(descending_state_[0].alt_diff) / (ideal_altitude * 0.9f), 0, 1));
    }
    if ((high_low == 1 /* && flight_control_.GetSpeed() > 57 */) || flight_control_.GetVerticalRateDisplay() > 150.0f) {
      flight_control_.SetPitchInput(0.05f * Mathf.Clamp(Mathf.Abs(descending_state_[0].alt_diff) / (ideal_altitude * 0.9f), 0, 1));
    }
    if (high_low == 1) {
      if (flight_control_.GetVerticalRateDisplay() > -250) {
        flight_control_.SetPitchInput(0.02f);
      } else if (flight_control_.GetVerticalRateDisplay() > -350) {
        flight_control_.SetPitchInput(0.01f);
      } else if (flight_control_.GetVerticalRateDisplay() > -450) {
        flight_control_.SetPitchInput(0.0f);
      } else {
        flight_control_.SetPitchInput(-0.02f);
      }
    }
    if (high_low == 0) {
      if (flight_control_.GetVerticalRateDisplay() < -330) {
        flight_control_.SetPitchInput(-0.04f);
      } else if (flight_control_.GetVerticalRateDisplay() < -250) {
        flight_control_.SetPitchInput(-0.03f);
      } else if (flight_control_.GetVerticalRateDisplay() < -150) {
        flight_control_.SetPitchInput(-0.02f);
      } else {
        flight_control_.SetPitchInput(-0.01f);
      }
    }

    if (flight_control_.GetSpeed() > 58) {
      flight_control_.ThrottleDown(0.2f * dt);
    }
    if (flight_control_.GetSpeed() < 54) {
      flight_control_.ThrottleUp(0.15f * dt);
    }
    // Debug.Log("Ideal rate = " + ideal_descending_rate_);

    
    if (final_descending_control_update_timer > final_descending_control_update_interval) {
      Debug.Log(debug_str);
      int[] descending_state = new int[4] { high_low, descend_ascend, correction_fast_or_slow, drate_speed_up_or_slow_down };
      if (throttle_lookup_.ContainsKey(descending_state)) {
        float throttle_input = throttle_lookup_[descending_state];
        if (throttle_input == -100) {
          Debug.Log("Error lookup state with key: " + descending_state);
        } else {
          float throttle_multiplier_due_to_time_interval = 5.0f * final_descending_control_update_interval;
          if (throttle_input > 0.0f) {
            flight_control_.ThrottleUp(throttle_multiplier_due_to_time_interval * throttle_input * dt);
          } else if (throttle_input < 0.0f) {
            flight_control_.ThrottleDown(throttle_multiplier_due_to_time_interval * (-throttle_input) * dt);
          }
        }
        debug_str += "Throttle decision: ";
        debug_str += throttle_input.ToString("F1");
      } else {
        debug_str += "Lookup return empty result.";
      }
      Debug.Log(debug_str);
    }
    

    if (final_descending_control_update_timer > final_descending_control_update_interval) {
      final_descending_control_update_timer -= final_descending_control_update_interval;
    }

    // heading control, include runway alignment

    if (distance > 30.0f && !state_machine_.IsEnterRunway()) {
      Vector3 runway_dir = runway_end_.transform.position - descending_target_.transform.position;
      runway_dir.y = 0;
      Vector3 to_runway_dir = descending_target_.transform.position - aircraft_.transform.position;
      to_runway_dir.y = 0;
      // if angle is positive, runway is on right, need to turn right to correct
      float approach_angle = Vector3.SignedAngle(runway_dir, to_runway_dir, Vector3.up);
      // Debug.Log("approach_angle = " + approach_angle);
      
      float angle_descending_target = Vector3.SignedAngle(Vector3.forward, to_runway_dir, Vector3.up);
      if (angle_descending_target < 0) {
        angle_descending_target += 360;
      }
      // Debug.Log("angle_descending_target = " + angle_descending_target);

      float distance_to_middle_line = to_runway_dir.magnitude * Mathf.Abs(Mathf.Sin(approach_angle / 180.0f * Mathf.PI));
      // Debug.Log("distance_to_middle_line = " + distance_to_middle_line);
      // Debug.Log("runway_width = " + runway_width_);

      if (distance_to_middle_line > 0) {
        float extra_angle = Mathf.Clamp(distance_to_middle_line / runway_width_ * 10.0f, 0.0f, 10.0f);
        // Debug.Log("extra_angle = " + extra_angle);
        if (approach_angle > 0) {
          // runway is on the right, turn further right to correct
          // Debug.Log("Runway is on the right.");
          angle_descending_target += extra_angle;
          if (angle_descending_target > 360.0f) {
            angle_descending_target -= 360.0f;
          }
        }
        if (approach_angle < 0) {
          // runway is on the left, turn further left to correct
          // Debug.Log("Runway is on the left.");
          angle_descending_target -= extra_angle;
          if (angle_descending_target < 0.0f) {
            angle_descending_target += 360.0f;
          }
        }
      }
      // Debug.Log("Auto heading: " + (int)angle_descending_target);
      flight_control_.SetAutoHeadingValue((int)angle_descending_target);
    }
    if (state_machine_.IsEnterRunway()) {
      // Aiming runway end
      Vector3 to_runway_end_dir = runway_end_.transform.position - aircraft_.transform.position;
      to_runway_end_dir.y = 0;
      float angle_descending_target = Vector3.SignedAngle(Vector3.forward, to_runway_end_dir, Vector3.up);
      if (angle_descending_target < 0) {
        angle_descending_target += 360;
      }
      flight_control_.SetAutoHeadingValue((int)angle_descending_target);
    }
    
    // airport height is 11 + 10
    if (flight_control_.GetAltitude() < 21.0f) {
      state_transition_delegate_(this.Name, "TenAboveGround");
    }
  }

}


public class TenAboveGroundState : State {
  public TenAboveGroundState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "TenAboveGround";

  private GameObject runway_end_;

  public override int Id { get; } = 19;

  public override void Entry(string from) {
    runway_end_ = GameObject.Find("PaloAltoAirport/Runway31LandingWaypoints/RunwayEnd");
    if (!state_machine_.IsEnterRunway()) {
      flight_control_.SetAutoAltitudeSwitch(true);
      flight_control_.SetAutoAltitudeValue(11 + 10);
    }
  }

  public override void Exit() {
  }

  public override void Update(float dt) {
    if (state_machine_.IsEnterRunway()) {
      flight_control_.SetAutoAltitudeSwitch(false);
      flight_control_.SetThrustInput(0.0f);
      if (flight_control_.GetVerticalRateDisplay() < -100) {
        flight_control_.TrimDecrease(dt);
      }
    }
    
    Vector3 diff = runway_end_.transform.position - aircraft_.transform.position;
    diff.y = 0;
    float angle = Vector3.SignedAngle(Vector3.forward, diff, Vector3.up);
    if (angle < 0) {
      angle += 360;
    }
    flight_control_.SetAutoHeadingValue((int)angle);

    if (flight_control_.IsGrounded()) {
      Debug.Log("TouchDown rate: " + flight_control_.GetPreviousVerticalRateDisplay());
      state_transition_delegate_(this.Name, "TouchDown");
    }
  }

}

public class TouchDownState : State {
  public TouchDownState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "TouchDown";

  private List<string> exit_list_ = new List<string>();

  public override int Id { get; } = 20;

  public override void Entry(string from) {
    flight_control_.SetAutoHeadingSwitch(false);
    flight_control_.SetAutopilotSwitch(false);
    if (!state_machine_.IsPassR5()) {
      exit_list_.Add("R5");
    }
    if (!state_machine_.IsPassR3()) {
      exit_list_.Add("R3");
    }
    if (!state_machine_.IsPassR1()) {
      exit_list_.Add("R1");
    }
    state_machine_.ExitRequest(exit_list_);
  }

  public override void Exit() {
    state_machine_.ResetExitDetection();
  }

  public override void Update(float dt) {
    if (flight_control_.GetSpeed() > 25) {
      flight_control_.ThrottleDown(dt);
      flight_control_.ApplyBrake();
    } else {
      flight_control_.ThrottleUp(dt / 3.0f);
      flight_control_.ReleaseBrake();
    }
    if (exit_list_.Count > 1) {
      Vector3 next_exit_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/" + exit_list_[0]).transform.position;
      Vector3 next_next_exit_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/" + exit_list_[1]).transform.position;
      float to_next_exit_distance = (aircraft_.transform.position - next_exit_position).magnitude;
      float to_next_next_exit_distance = (aircraft_.transform.position - next_next_exit_position).magnitude;
      if (to_next_next_exit_distance <= (next_exit_position - next_next_exit_position).magnitude) {
        state_machine_.DisableExitChoosingButton(exit_list_[0]);
        exit_list_.RemoveAt(0);
      }
    }
    if (exit_list_.Count == 1) {
      state_machine_.SetExitChosen(exit_list_[0]);
      state_machine_.DisableExitChoosingButton(exit_list_[0]);
      state_transition_delegate_(this.Name, "ExitRunway");
    }
  }

}

public class ExitRunwayState : State {
  public ExitRunwayState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base (state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "ExitRunway";

  public void SetHoldOnCommand(bool value) {
    hold_on_command_ = value;
  }

  private Vector3 next_waypoint_position;
  private float target_taxi_speed_;
  private List<string> exit_route_;

  private bool hold_on_command_;
  private bool hold_on_traffic_;

  private float angle_diff_;
  private float angle_diff_last_;
  private float angle_diff_last_last_;
  private float steer_input_;

  private float kp_ = 40.0f;
  private float ki_ = 0.0f;
  private float kd_ = 0.0f;


  public override int Id { get; } = 21;

  public override void Entry(string from) {
    if (from == "TouchDown") {
      // If from hold on resume, don't re-compute
      state_machine_.ComputeExitRoute();
      state_machine_.SetGateAssignDropdownActive(true);
    }
    if (from == "HoldOnParkingSelection") {
      
    }
    exit_route_ = state_machine_.GetExitRoute();
    foreach (string route in exit_route_) {
      Debug.Log(route);
    }

    next_waypoint_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/" + exit_route_[0]).transform.position;

    if (from == "HoldOnCommand") {
      hold_on_command_ = false;
    }
    if (from == "HoldOnTraffic") {
      hold_on_traffic_ = false;
    }
    state_machine_.SetHoldOnCommandButtonActive(true, this.SetHoldOnCommand, this.Name);

    // turn on taxi radar
    state_machine_.SetGroundRadar(true, true);
  }

  public override void Exit() {
    // turn off taxi radar
    state_machine_.SetGroundRadar(false, true);
  }

  public override void Update(float dt) {
    // Debug.Log(Name + " update.");
    List<GameObject> collision_objects = state_machine_.GetCollisionTrackingObjects();
    if (collision_objects != null && collision_objects.Count > 0) {
      Debug.Log("Checking collision objects.");
      foreach (GameObject aircraft in collision_objects) {
        float distance = (aircraft.transform.position - aircraft_.transform.position).magnitude;
        Debug.Log("That aircraft is " + distance + " away.");
        string current_state_name = aircraft.GetComponent<StateMachine>().GetCurrentStateName();
        Debug.Log("That aircraft is in " + current_state_name + " state.");
        if ( current_state_name == "Inactive" ||
             current_state_name == "Loading" ||
             current_state_name == "DepartureRunwayRequest" ||
             current_state_name == "PushbackRequest") {
          continue;
        }
        hold_on_traffic_ = true;
        Debug.Log(state_machine_.GetTailName() + ": hold on traffic" +
                  aircraft.GetComponent<StateMachine>().GetTailName() + "@" + current_state_name);
        state_machine_.SetHoldOnTrafficObject(aircraft);
        break;
      }
    }

    Vector3 current_rotation = aircraft_.transform.forward;
    current_rotation.y = 0;
    current_rotation.Normalize();
    Vector3 target_rotation = next_waypoint_position - aircraft_.transform.position;
    target_rotation.y = 0;
    target_rotation.Normalize();
    angle_diff_ = Vector3.Angle(current_rotation, target_rotation);

    float cross_product_dot_y = Vector3.Cross(current_rotation, target_rotation).y;
    if (cross_product_dot_y > 0) {
      angle_diff_ *= -1.0f;
    }

    steer_input_ = steer_input_ +
                        (kp_ + ki_ * dt + kd_ / dt) * (angle_diff_) +
                        (-kp_ - 2.0f * kd_ / dt) * (angle_diff_last_) +
                        kd_ / dt * (angle_diff_last_last_);

    flight_control_.SetSteerInput(Mathf.Clamp(steer_input_, -1.0f, 1.0f));

    angle_diff_last_last_ = angle_diff_last_;
    angle_diff_last_ = angle_diff_;


    Vector3 position_diff = aircraft_.transform.position - next_waypoint_position;
    position_diff.y = 0;

    float angle_diff_abs = Mathf.Abs(angle_diff_);
    if (position_diff.magnitude < 10.0f) {
      target_taxi_speed_ = angle_diff_abs > 2.0f ? 2.0f : 4.0f;
    } else {
      target_taxi_speed_ = angle_diff_abs > 2.0f ? 3.0f : 15.0f;
    }

    if (position_diff.magnitude < 5.0f) {
      exit_route_.RemoveAt(0);
      state_machine_.SetExitRoute(exit_route_);
      if (exit_route_.Count == 0) {
        if (string.IsNullOrEmpty(state_machine_.GetParkingGateName())) {
          state_transition_delegate_(this.Name, "HoldOnParkingSelection");
        } else {
          state_transition_delegate_(this.Name, "TaxiToGate");
        }
        return;
      }
      next_waypoint_position = GameObject.Find("PaloAltoAirport/GroundWaypoints/" + exit_route_[0]).transform.position;
    }

    // When hold button is pressed, the aircraft will not stop immediately. If a waypoint is passed before the aircraft
    // stops, we still need to track the removal of the waypoint. So after the aircraft completely stops, we transit
    // the state to HoldOnCommand.
    if (hold_on_command_ || hold_on_traffic_) {
      Debug.Log("Speed is: " + flight_control_.GetSpeed().ToString());
      target_taxi_speed_ = 0.0f;
      if (flight_control_.GetSpeed() < 0.001f) {
        // hold on command should take precedence
        if (hold_on_command_) {
          state_transition_delegate_.Invoke(this.Name, "HoldOnCommand");
        }
        if (hold_on_traffic_) {
          state_transition_delegate_.Invoke(this.Name, "HoldOnTraffic");
        }
      }
    }

    if (flight_control_.GetSpeed() < target_taxi_speed_) {
      flight_control_.ThrottleUp(dt / 3.0f);
      flight_control_.ReleaseBrake();
    } else {
      flight_control_.ThrottleDown(dt);
      flight_control_.ApplyBrake();
    }

    // Reset flap
    int flap_degree_table_pointer;
    if (flight_control_.GetFlapDegreeTablePointer() > 0) {
      flap_degree_table_pointer = flight_control_.RetreatFlap();
    }

    // Reset trim
    if (flight_control_.GetTrim() > 0.05f) {
      flight_control_.TrimDecrease(dt);
    } else if (flight_control_.GetTrim() < -0.05f) {
      flight_control_.TrimIncrease(dt);
    }
  }

}

public class HoldOnParkingSelectionState : State {
  public HoldOnParkingSelectionState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport) :
    base(state_transition_delegate, aircraft, airport) {
  }

  public override string Name { get; } = "HoldOnParkingSelection";

  public override int Id { get; } = 22;

  public override void Entry(string from) {
    flight_control_.ApplyBrake();
  }

  public override void Exit() {
    flight_control_.ReleaseBrake();
  }

  public override void Update(float dt) {
    
  }
}

public class TaxiToGateState : State {
  public TaxiToGateState(StateTransitionDelegate state_transition_delegate, GameObject aircraft, AirportManager airport)
    : base(state_transition_delegate, aircraft, airport) {
  }
  public override string Name { get; } = "TaxiToGate";

  // Start position
  private Vector3 start_position;
  private Vector3 gate_entry_position;

  private Vector3 start_direction;
  private Vector3 gate_direction;

  private float radius;
  private Vector3 center;

  private float t;

  // The gate position. x,z values are used.
  private Vector3 gate_position;

  private bool turn_right_to_gate;

  private bool to_entry_completed = false;

  public override int Id { get; } = 23;

  public override void Entry(string from) {
    // TODO: need to dertermine the four positions and directions
    // For now, just use hard coded value
    GateInfo gate_info = state_machine_.GetGateInfo(state_machine_.GetParkingGateName());
    gate_position = gate_info.gate_gameobject.transform.position;
    gate_position.y = 0;
    start_direction = aircraft_.transform.forward;
    start_direction.y = 0;
    start_position = aircraft_.transform.position;
    start_position.y = 0;
    gate_entry_position = state_machine_.GetGateInfo(state_machine_.GetParkingGateName()).gate_entry_gameobject.transform.position;
    gate_entry_position.y = 0;
    gate_direction = gate_position - gate_entry_position;
    to_entry_completed = false;

    // Check if turn right or left to the gate.
    Vector3 diff = gate_entry_position - start_position;
    Vector3 right = aircraft_.transform.right;
    right.y = 0;
    float dot_product = Vector3.Dot(diff, right);
    turn_right_to_gate = (dot_product > 0);
    if (turn_right_to_gate) {
      Debug.Log("Turn right to gate.");
    } else {
      Debug.Log("Turn left to gate.");
    }

    float angle_diff = Vector3.Angle(start_direction, gate_direction);
    Debug.Log("Angle diff = " + angle_diff);
    float angle_diff_rad = angle_diff * Mathf.PI / 180.0f;
    Debug.Log("Angle diff rad = " + angle_diff_rad);
    float dist = (gate_entry_position - start_position).magnitude;
    Debug.Log("dist = " + dist);
    radius = dist / 2.0f / Mathf.Sin(angle_diff_rad/2.0f);
    Debug.Log("Radius = " + radius);

    center = start_position + (Quaternion.Euler(0, 90, 0) * start_direction).normalized * radius;
    // center.y = aircraft_.transform.position.y;
    flight_control_.SetThrustInput(0);

    // turn on taxi radar
    state_machine_.SetGroundRadar(true, true);
  }

  public override void Exit() {
    state_machine_.SetIsInbound(false);
    // turn off radar
    state_machine_.SetGroundRadar(false, true);
  }

  public override void Update(float dt) {
    if (!to_entry_completed) {
      dt /= 3.0f;
      t += dt;

      Vector3 center_to_position = start_position - center;
      // center_to_position.y = aircraft_.transform.position.y;
      Vector3 new_position = center + Quaternion.AngleAxis(t / Mathf.PI * 180.0f / Mathf.PI, Vector3.up) * center_to_position;
      new_position.y = aircraft_.transform.position.y;
      aircraft_.transform.position = new_position;

      aircraft_.transform.RotateAround(aircraft_.transform.position, Vector3.up, dt / Mathf.PI * 180.0f / Mathf.PI);

      Vector3 diff = gate_entry_position - aircraft_.transform.position;
      diff.y = 0;
      Debug.Log("Dist to entry: " + diff.magnitude);
      if (diff.magnitude < 2.8f) {
        to_entry_completed = true;
        aircraft_.transform.position += diff;
        Vector3 euler_angles = aircraft_.transform.eulerAngles;
        GateInfo gate_info = state_machine_.GetGateInfo(state_machine_.GetParkingGateName());
        euler_angles.y = gate_info.direction.y;
        aircraft_.transform.eulerAngles = euler_angles;
        flight_control_.SetVelocity(new Vector3(0, 0, 0));
      }
    } else {
      Debug.Log("To Entry Completed.");
      Vector3 diff = gate_position - aircraft_.transform.position;
      diff.y = 0;
      aircraft_.transform.position += (diff.normalized * dt * 0.5f);

      if (diff.magnitude < 1.5f) {
        GateInfo gate_info = state_machine_.GetGateInfo(state_machine_.GetParkingGateName());
        flight_control_.ToggleEngine();
        Vector3 euler_angles = aircraft_.transform.eulerAngles;
        euler_angles.y = gate_info.direction.y;
        aircraft_.transform.position += diff;
        aircraft_.transform.eulerAngles = euler_angles;
        flight_control_.SetVelocity(new Vector3(0, 0, 0));
        flight_control_.ApplyBrake();
        state_machine_.SetGateOccupied();
        state_transition_delegate_(this.Name, "Inactive");
      }
    }
  }

}

public class StateMachine : MonoBehaviour {
  public StateMachine() { }
  public void SetInitialCondition(AircraftInstantiateInitialConditions cond) {
    is_inbound_ = cond.is_inbound;
    parking_gate_name_ = cond.parking_gate_name;
    outgoing_content_ = cond.outbound_content;
    incoming_content_ = cond.inbound_content;
    card_prefab_ = cond.card_prefab;

    initial_auto_heading_ = cond.heading;
    initial_auto_speed_ = cond.speed;
    initial_auto_altitude_ = cond.altitude;

    tail_name_ = cond.tail_name;
  }

  GroundChannelManager ground_;

  [SerializeField] GameObject aircraft_;
  FlightControl flight_control_;

  AirportManager airport_;

  [SerializeField] Material red_material;
  [SerializeField] Material blue_material;

  private string tail_name_;

  private Transform outgoing_content_;
  private Transform incoming_content_;

  private GameObject card_prefab_;
  private GameObject card_;

  // States object container
  private State[] states_ = new State[24];

  // State name to id container
  private StateNameToId[] state_name_to_id_ = new StateNameToId[24];

  // current and previous state id
  private int current_state_id_;
  private int previous_state_id_;

  private bool is_inbound_;
  private string parking_gate_name_; // uninitialized private string will have null value. uninitialized public string is empty.
  private string exit_chosen_;
  private string departure_runway_chosen_;
  private string arrival_runway_chosen_;
  private string approach_chosen_;
  private ApproachInfo landing_approach_info_;
  private List<string> departure_route_ = new List<string>();
  private List<string> exit_route_ = new List<string>();
  private List<string> parking_route_ = new List<string>();
  private string pushback_target_;

  private float initial_auto_heading_;
  private float initial_auto_speed_;
  private float initial_auto_altitude_;

  private AddIndexToDestroyList flights_manager_destroy_callback_;
  private SetCurrentByCardClick set_current_by_card_click_callback_;
  private int flights_manager_pool_index_;

  private List<GameObject> collision_tracking_objects_ = new();

  private GameObject hold_on_traffic_object_ = null;

  private List<string> ground_states_group_ = new();
  private List<string> in_gate_states_group_ = new();

  // Todo add landing airport and pull data from there.
  private bool enter_runway_ = false;
  private bool pass_r5_exit_ = false;
  private bool pass_r3_exit_ = false;
  private bool pass_r1_exit_ = false;

  private HumanVoice pilot_voice_;
  private int departure_runway_request_id_;
  private int pushback_request_id_;

  // Start is called before the first frame update
  void Start() {
    airport_ = GameObject.Find("PaloAltoAirport").GetComponent<AirportManager>();

    pilot_voice_ = new HumanVoice("Microsoft David Desktop");

    ground_ = GameObject.Find("GroundChannelManager").GetComponent<GroundChannelManager>();

    flight_control_ = transform.GetComponent<FlightControl>();

    states_[0] = new InactiveState(this.StateTransition, aircraft_, airport_);
    states_[1] = new LoadingState(this.StateTransition, aircraft_, airport_);
    states_[2] = new DepartureRunwayRequestState(this.StateTransition, aircraft_, airport_);
    states_[3] = new TaxiwayRequestState(this.StateTransition, aircraft_, airport_);
    states_[4] = new PushbackRequestState(this.StateTransition, aircraft_, airport_);
    states_[5] = new PushbackState(this.StateTransition, aircraft_, airport_);
    states_[6] = new TaxiToRunwayState(this.StateTransition, aircraft_, airport_);
    states_[7] = new HoldOnCommandState(this.StateTransition, aircraft_, airport_);
    states_[8] = new HoldOnTrafficState(this.StateTransition, aircraft_, airport_);
    states_[9] = new HoldOnCrossingState(this.StateTransition, aircraft_, airport_);
    states_[10] = new HoldOnEnterRunwayState(this.StateTransition, aircraft_, airport_);
    states_[11] = new HoldOnTakeoffState(this.StateTransition, aircraft_, airport_);
    states_[12] = new TakeoffState(this.StateTransition, aircraft_, airport_);
    states_[13] = new ClimbState(this.StateTransition, aircraft_, airport_);

    states_[14] = new OnRadarState(this.StateTransition, aircraft_, airport_);
    states_[15] = new ArrivalRunwayRequestState(this.StateTransition, aircraft_, airport_);
    states_[16] = new ApproachRequestState(this.StateTransition, aircraft_, airport_);
    states_[17] = new ToFinalState(this.StateTransition, aircraft_, airport_);
    states_[18] = new OnFinalState(this.StateTransition, aircraft_, airport_);
    states_[19] = new TenAboveGroundState(this.StateTransition, aircraft_, airport_);
    states_[20] = new TouchDownState(this.StateTransition, aircraft_, airport_);
    states_[21] = new ExitRunwayState(this.StateTransition, aircraft_, airport_);
    states_[22] = new HoldOnParkingSelectionState(this.StateTransition, aircraft_, airport_);
    states_[23] = new TaxiToGateState(this.StateTransition, aircraft_, airport_);

    for (int i = 0; i < states_.Length; i++) {
      state_name_to_id_[i] = new StateNameToId(states_[i].Name, states_[i].Id);
    }

    current_state_id_ = 0;
    previous_state_id_ = 0;
    // parking_loading_complete_event_ = new ParkingLoadingCompleteEvent();

    // Call Inactive state's entry function to initialize the aircraft.
    states_[0].Entry("Inactive");

    // Assign taxi collide callback to flight_control
    flight_control_.AssignTaxiRaycastCollideCallback(this.TaxiRaycastCollideCallback);

    for (int i = 0; i < 13; i++) {
      ground_states_group_.Add(states_[i].Name);
    }
    ground_states_group_.Add(states_[21].Name);
    ground_states_group_.Add(states_[22].Name);

    for (int i = 1; i < 5; i++) {
      in_gate_states_group_.Add(states_[i].Name);
    }
  }

  // Update is called once per frame
  void Update() {
    float dt = Time.deltaTime;
    states_[current_state_id_].Update(dt);

    if (card_ != null) {
      card_.GetComponent<CardHandler>().PrintAltitudeHeadingSpeed(flight_control_.GetAltitude(),
        flight_control_.GetHeading(), flight_control_.GetSpeed());
      card_.GetComponent<CardHandler>().PrintStateInfo(states_[current_state_id_].GetStateInfo());
    }
  }

  void StateTransition(string from, string to) {
    Debug.Log(GetTailName() + ": " + states_[current_state_id_].Name + " Exit.");
    states_[current_state_id_].Exit();
    current_state_id_ = GetStateIdFromName(to);
    previous_state_id_ = GetStateIdFromName(from);
    Debug.Log(GetTailName() + ": " + states_[current_state_id_].Name + " Entry.");
    states_[current_state_id_].Entry(from);

    // Print new state name on card
    if (card_ != null) {
      card_.GetComponent<CardHandler>().PrintStateName(to);
    }
  }

  public int GetStateIdFromName(string name) {
    for (int i = 0; i < state_name_to_id_.Length; i++) {
      if (state_name_to_id_[i].name == name) {
        return state_name_to_id_[i].id;
      }
    }
    return -1;
  }

  public void SetCardPrefab(GameObject card_prefab) {
    card_prefab_ = card_prefab;
  }

  public void InstantiateCard(bool is_incoming) {
    card_ = Instantiate(card_prefab_, is_incoming ? incoming_content_ : outgoing_content_);
    card_.GetComponent<CardHandler>().PrintTailName(tail_name_);
  }

  public void DestroyCard() {
    card_.GetComponent<CardHandler>().DestroyAllChildrenGameObject(card_.transform);
    Destroy(card_);
    card_ = null;
  }

  public void ArrivalRunwayChosen(string runway) {
    Debug.Log("Arrival runway: " + runway + " is chosen.");
    // TODO: handle chosen result.
    arrival_runway_chosen_ = runway;

    card_.GetComponent<CardHandler>().DeactivateRunwayChoosingButtons();
    StateTransition(states_[current_state_id_].Name, "ApproachRequest");
  }

  public void DepartureRunwayChosen(string runway) {
    Debug.Log("Departure runway: " + runway + " is chosen.");
    // TODO: handle chosen result.
    departure_runway_chosen_ = runway;

    card_.GetComponent<CardHandler>().DeactivateRunwayChoosingButtons();

    ground_.GenerateResponse(departure_runway_request_id_,
                             runway,
                             this.StateTransition,
                             states_[current_state_id_].Name,
                             "TaxiwayRequest");
  }

  public void ArrivalRunwayRequest() {
    // Let CardHandler pop up the runway buttons for choice, and send the callback function to handle the user choice
    List<string> arrival_runway_available = airport_.GetRunwayInUseList();
    card_.GetComponent<CardHandler>().RunwayRequest(arrival_runway_available, this.ArrivalRunwayChosen);
  }

  public void GroundChannelDepartureRunwayRequest() {
    departure_runway_request_id_ = ground_.GetRequestId();

    AircraftToGroundRequest request = new();
    request.type = AircraftToGroundRequestType.DepartureRunway;
    request.to_whom = airport_.GetAirportInfo().name + " ground";
    request.from_whom = GetTailName();
    request.for_what = "departure runway request";
    request.callback = this.DepartureRunwayRequest;
    request.voice = pilot_voice_;
    request.request_id = departure_runway_request_id_;

    ground_.Request(request);
  }
  
  public void DepartureRunwayRequest() {
    // Let CardHandler pop up the runway buttons for choice, and send the callback function to handle the user choice
    List<string> departure_runway_available = airport_.GetRunwayInUseList();
    card_.GetComponent<CardHandler>().RunwayRequest(departure_runway_available, this.DepartureRunwayChosen);
  }

  public void ApproachChosen(string approach) {
    Debug.Log("Approach: " + approach + " is chosen.");
    approach_chosen_ = approach;
    landing_approach_info_ = airport_.GetApproachInfo(approach);
    card_.GetComponent<CardHandler>().DeactivateApproachChoosingButtons();
    StateTransition(states_[current_state_id_].Name, "ToFinal");
  }

  public void ApproachRequest() {
    List<string> approach_available = new List<string> { "A", "B", "C", "D" };
    card_.GetComponent<CardHandler>().ApproachRequest(approach_available, this.ApproachChosen);
  }

  public List<string> GetDepartureRoute() {
    return departure_route_;
  }

  public void TaxiwayChosen(bool is_automatic) {
    if (is_automatic) {
      // TODO:
      // Call airport to get the route
    } else {
      // TODO:
      // Develop manual choosing mechanism
    }

    // should be the one that is farther to the first waypoint
    departure_route_.Clear();
    if (parking_gate_name_ == "P1") {
      pushback_target_ = "W2";
      departure_route_.Add("W3");
    } else if (parking_gate_name_ == "Q1") {
      pushback_target_ = "W4";
      departure_route_.Add("W5");
    } else if (parking_gate_name_ == "S1") {
      pushback_target_ = "W6";
      departure_route_.Add("W7");
    } else if (parking_gate_name_ == "U1") {
      pushback_target_ = "W8";
      departure_route_.Add("W9");
    } else if (parking_gate_name_ == "V1") {
      pushback_target_ = "W10";
      departure_route_.Add("W11");
    } else if (parking_gate_name_ == "X1") {
      pushback_target_ = "W12";
      departure_route_.Add("W13");
    }
    departure_route_.Add("W14");
    departure_route_.Add("W15");
    departure_route_.Add("W16");
    departure_route_.Add("W17");
    departure_route_.Add("W18");
    departure_route_.Add("T1");
    departure_route_.Add("T2");
    departure_route_.Add("Y9");
    departure_route_.Add("Y10");
    departure_route_.Add("EnterRunway31Sign");
    departure_route_.Add("A1");
    departure_route_.Add("A2");
    departure_route_.Add("R6");
    departure_route_.Add("Runway31TakeoffSign");

    card_.GetComponent<CardHandler>().DeactivateTaxiwayChoosingButtons();
    StateTransition(states_[current_state_id_].Name, "PushbackRequest");
  }

  public void ExitChosen(string exit_chosen) {
    Debug.Log("Exit: " + exit_chosen + " is chosen.");
    exit_chosen_ = exit_chosen;
    card_.GetComponent<CardHandler>().DeactivateExitChoosingButtons();
    StateTransition(states_[current_state_id_].Name, "ExitRunway");
  }

  public void TaxiwayRequest() {
    // Step 1: pop up Manual vs Automatic button
    card_.GetComponent<CardHandler>().TaxiwayRequest(this.TaxiwayChosen);
  }

  public void ExitRequest(List<string> exit_list) {
    card_.GetComponent<CardHandler>().ExitRequest(exit_list, this.ExitChosen);
  }

  void PushbackApproveButtonCallback() {
    Debug.Log("PushbackRequest Allowed.");
    // Deactivate the pushback request button
    SetPushbackApproveButtonDisable();
    ground_.GenerateResponse(pushback_request_id_,
                             departure_runway_chosen_,
                             this.StateTransition,
                             states_[current_state_id_].Name,
                             "Pushback");
  }

  public void GroundChannelPushbackRequest() {
    pushback_request_id_ = ground_.GetRequestId();

    AircraftToGroundRequest request = new();
    request.type = AircraftToGroundRequestType.Pushback;
    request.to_whom = airport_.GetAirportInfo().name + " ground";
    request.from_whom = GetTailName();
    request.for_what = "request pushback to runway " + departure_runway_chosen_;
    request.callback = this.SetPushbackApproveButtonActive;
    request.voice = pilot_voice_;
    request.request_id = pushback_request_id_;

    ground_.Request(request);
  }

  public void SetPushbackApproveButtonActive() {
    SetPushbackApproveButton(true);
  }

  public void SetPushbackApproveButtonDisable() {
    SetPushbackApproveButton(false);
  }

  public void SetPushbackApproveButton(bool is_active) {
    GameObject button = card_.transform.Find("Panel/PushbackApproveButton").gameObject;
    button.SetActive(is_active);
    if (is_active) {
      button.GetComponent<Button>().onClick.AddListener(PushbackApproveButtonCallback);
    } else {
      button.GetComponent<Button>().onClick.RemoveAllListeners();
    }
  }

  void HoldOnCommandButtonCallback(SetHoldOnCommandInTaxiToRunwayState set_hold_on_command, string original_state) {
    set_hold_on_command(true);
    GameObject button = card_.transform.Find("Panel/HoldOnCommandButton").gameObject;
    button.transform.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Resume";
    button.GetComponent<Button>().onClick.RemoveAllListeners();
    button.GetComponent<Button>().onClick.AddListener(() => {
      ResumeButtonCallback(set_hold_on_command, original_state);
    });
  }

  void ResumeButtonCallback(SetHoldOnCommandInTaxiToRunwayState set_hold_on_command, string original_state) {
    set_hold_on_command(false);
    GameObject button = card_.transform.Find("Panel/HoldOnCommandButton").gameObject;
    button.transform.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Hold";
    button.GetComponent<Button>().onClick.RemoveAllListeners();
    button.GetComponent<Button>().onClick.AddListener(() => { HoldOnCommandButtonCallback(set_hold_on_command, original_state); });
    if (states_[current_state_id_].Name == "HoldOnCommand") {
      StateTransition(states_[current_state_id_].Name, original_state);
    }
  }

  public void SetHoldOnCommandButtonActive(bool is_active, SetHoldOnCommandInTaxiToRunwayState set_hold_on_command, string original_state) {
    GameObject button = card_.transform.Find("Panel/HoldOnCommandButton").gameObject;
    button.SetActive(is_active);
    if (is_active) {
      button.GetComponent<Button>().onClick.AddListener(() => {
        set_hold_on_command(true);
        GameObject button = card_.transform.Find("Panel/HoldOnCommandButton").gameObject;
        button.transform.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Resume";
        button.GetComponent<Button>().onClick.RemoveAllListeners();
        button.GetComponent<Button>().onClick.AddListener(() => { ResumeButtonCallback(set_hold_on_command, original_state); });
      });
    } else {
      button.GetComponent<Button>().onClick.RemoveAllListeners();
    }
  }

  public void TakeoffApprovedButtonCallback(SetTakeoffApproved set_takeoff_approved) {
    set_takeoff_approved(true);
    GameObject takeoff_button = card_.transform.Find("Panel/TakeoffButton").gameObject;
    takeoff_button.GetComponent<Button>().onClick.RemoveAllListeners();
    takeoff_button.SetActive(false);

    GameObject align_button = card_.transform.Find("Panel/AlignButton").gameObject;
    align_button.GetComponent<Button>().onClick.RemoveAllListeners();
    align_button.SetActive(false);

    if (states_[current_state_id_].Name == "HoldOnEnterRunway") {
      StateTransition("HoldOnEnterRunway", "TaxiToRunway");
    }
  }

  public void SetTakeoffButtonActive(bool is_active, SetTakeoffApproved set_takeoff_approved) {
    GameObject button = card_.transform.Find("Panel/TakeoffButton").gameObject;
    button.SetActive(is_active);
    if (is_active) {
      button.GetComponent<Button>().onClick.AddListener(() => {
        TakeoffApprovedButtonCallback(set_takeoff_approved);
      });
    }
  }

  public void SetTakeoffButtonFromHoldOnTakeoffState() {
    GameObject button = card_.transform.Find("Panel/TakeoffButton").gameObject;
    button.SetActive(true);
    button.GetComponent<Button>().onClick.RemoveAllListeners();
    button.GetComponent<Button>().onClick.AddListener(() => {
      StateTransition("HoldOnTakeoff", "Takeoff");
      card_.GetComponent<CardHandler>().DeactivateAllDepartureButtons();
    });
  }

  public void AlignButtonCallback(SetAlign set_align) {
    set_align(true);
    GameObject align_button = card_.transform.Find("Panel/AlignButton").gameObject;
    align_button.GetComponent<Button>().onClick.RemoveAllListeners();
    align_button.SetActive(false);
    StateTransition(states_[current_state_id_].Name, "TaxiToRunway");
  }

  public void SetAlignButtonActive(bool is_active, SetAlign set_align) {
    GameObject button = card_.transform.Find("Panel/AlignButton").gameObject;
    button.SetActive(is_active);
    if (is_active) {
      button.GetComponent<Button>().onClick.AddListener(() => {
        AlignButtonCallback(set_align);
      });
    }
  }

  public void SetGateAssignDropdownActive(bool is_active) {
    GameObject gate_assign_dropdown = card_.transform.Find("Panel/GateAssignDropdown").gameObject;
    gate_assign_dropdown.GetComponent<GateAssignDropdown>().SetAirportManager(airport_);
    Debug.Log(gate_assign_dropdown.name);
    gate_assign_dropdown.SetActive(is_active);

    var dropdown = gate_assign_dropdown.GetComponent<Dropdown>();

    // gate_assign_dropdown.GetComponent<GateAssignDropdown>().RefreshAvailableGates();

    if (is_active) {
      dropdown.onValueChanged.RemoveAllListeners();
      dropdown.onValueChanged.AddListener(delegate {
        ParkingGateNameSetCallback(dropdown.options[dropdown.value].text);
      });
    }
  }

  public string GetParkingGateName() {
    return parking_gate_name_;
  }

  public void ParkingGateNameSetCallback(string name) {
    parking_gate_name_ = name;
    Debug.Log("Parking gate set: " + name);
    airport_.SetGateStatus(name, GateInfo.GateStatus.RESERVED);

    GameObject gate_assign_dropdown = card_.transform.Find("Panel/GateAssignDropdown").gameObject;
    gate_assign_dropdown.SetActive(false);
    ComputeToGateRoute();
    if (states_[current_state_id_].Name == "HoldOnParkingSelection") {
      StateTransition(states_[current_state_id_].Name, "ExitRunway");
    }
  }

  public bool IsInbound() {
    return is_inbound_;
  }

  public void SetIsInbound(bool is_inbound) {
    is_inbound_ = is_inbound;
  }

  public float GetInitialAutoHeading() {
    return initial_auto_heading_;
  }

  public float GetInitialAutoSpeed() {
    return initial_auto_speed_;
  }

  public float GetInitialAutoAltitude() {
    return initial_auto_altitude_;
  }

  public string GetApproachChosen() {
    return approach_chosen_;
  }

  public void SetExitChosen(string exit) {
    exit_chosen_ = exit;
  }

  public void DisableExitChoosingButton(string exit) {
    card_.GetComponent<CardHandler>().DisableExitChoosingButton(exit);
  }

  public void ComputeExitRoute() {
    exit_route_.Clear();
    if (exit_chosen_ == "R5") {
      exit_route_ = new List<string> { "R5", "B2", "B1" };
      return;
    }
    if (exit_chosen_ == "R3") {
      exit_route_ = new List<string> { "R3", "C2", "C1" };
      return;
    }
    // exit_chosen_ == R1
    exit_route_ = new List<string> { "R1", "E2", "E1" };
  }

  public List<string> GetExitRoute() {
    return exit_route_;
  }

  public void SetExitRoute(List<string> exit_route) {
    exit_route_ = exit_route;
  }

  public void ComputeToGateRoute() {
    parking_route_ = exit_route_;
    if (exit_chosen_ == "R5") {
      if (parking_gate_name_ == "P1") {
        parking_route_.AddRange(new List<string> { "Y5", "Y6", "Z2", "Z1", "W1", "W2" });
      }
      if (parking_gate_name_ == "Q1") {
        parking_route_.AddRange(new List<string> { "Y5", "Y6", "Z2", "Z1", "W1", "W2", "W4" });
      }
      if (parking_gate_name_ == "S1") {
        parking_route_.AddRange(new List<string> { "Y5", "Y6", "Z2", "Z1", "W1", "W2", "W6" });
      }
      if (parking_gate_name_ == "U1") {
        parking_route_.AddRange(new List<string> { "Y5", "Y6", "Z2", "Z1", "W1", "W2", "W8" });
      }
      if (parking_gate_name_ == "V1") {
        parking_route_.AddRange(new List<string> { "Y5", "Y6", "Z2", "Z1", "W1", "W2", "W10" });
      }
      if (parking_gate_name_ == "X1") {
        parking_route_.AddRange(new List<string> { "Y5", "Y6", "Z2", "Z1", "W1", "W2", "W12" });
      }
    }
    if (exit_chosen_ == "R3") {
      if (parking_gate_name_ == "P1") {
        parking_route_.AddRange(new List<string> { "Y3", "Y6", "Z2", "Z1", "W1", "W2" });
      }
      if (parking_gate_name_ == "Q1") {
        parking_route_.AddRange(new List<string> { "Y3", "Y6", "Z2", "Z1", "W1", "W2", "W4" });
      }
      if (parking_gate_name_ == "S1") {
        parking_route_.AddRange(new List<string> { "Y3", "Y6", "Z2", "Z1", "W1", "W2", "W6" });
      }
      if (parking_gate_name_ == "U1") {
        parking_route_.AddRange(new List<string> { "Y3", "Y6", "Z2", "Z1", "W1", "W2", "W8" });
      }
      if (parking_gate_name_ == "V1") {
        parking_route_.AddRange(new List<string> { "Y3", "Y6", "Z2", "Z1", "W1", "W2", "W10" });
      }
      if (parking_gate_name_ == "X1") {
        parking_route_.AddRange(new List<string> { "Y3", "Y6", "Z2", "Z1", "W1", "W2", "W12" });
      }
    }
    if (exit_chosen_ == "R1") {
      if (parking_gate_name_ == "P1") {
        parking_route_.AddRange(new List<string> { "Y1", "Y6", "Z2", "Z1", "W1", "W2", "W2" });
      }
      if (parking_gate_name_ == "Q1") {
        parking_route_.AddRange(new List<string> { "Y1", "Y6", "Z2", "Z1", "W1", "W2", "W4" });
      }
      if (parking_gate_name_ == "S1") {
        parking_route_.AddRange(new List<string> { "Y1", "Y6", "Z2", "Z1", "W1", "W2", "W6" });
      }
      if (parking_gate_name_ == "U1") {
        parking_route_.AddRange(new List<string> { "Y1", "Y6", "Z2", "Z1", "W1", "W2", "W8" });
      }
      if (parking_gate_name_ == "V1") {
        parking_route_.AddRange(new List<string> { "Y1", "Y6", "Z2", "Z1", "W1", "W2", "W10" });
      }
      if (parking_gate_name_ == "X1") {
        parking_route_.AddRange(new List<string> { "Y1", "Y6", "Z2", "Z1", "W1", "W2", "W12" });
      }
    }
  }

  public void SetFlightsManagerDestroyCallback(AddIndexToDestroyList callback) {
    flights_manager_destroy_callback_ = callback;
  }

  public void SetFlightsManagerPoolIndex(int index) {
    flights_manager_pool_index_ = index;
  }

  public void DestroyAircraft() {
    DestroyCard();
    aircraft_.gameObject.SetActive(false);
    flights_manager_destroy_callback_(flights_manager_pool_index_);
  }

  public void SetCardCurrent(bool value) {
    if (card_ != null) {
      card_.GetComponent<CardHandler>().SetCurrentIndicator(value);
    }
  }

  public void AssignSetCurrentByCardClickDelegate(SetCurrentByCardClick fcn) {
    set_current_by_card_click_callback_ = fcn;
  }

  public void AssignSetCurrentByCardClickToCard() {
    card_.GetComponent<CardHandler>().AssignSetCurrentByCardClick(set_current_by_card_click_callback_, flights_manager_pool_index_);
  }

  public void SetIndicatorMaterial() {
    GameObject sphere = aircraft_.transform.Find("SphereIndicator").gameObject;
    GameObject sphere_1 = aircraft_.transform.Find("AirSphereIndicator").gameObject;
    if (sphere == null) {
      return;
    }
    if (sphere_1 == null) {
      return;
    }
    var materials = sphere.GetComponent<MeshRenderer>().materials;
    materials[0] = is_inbound_ ? red_material : blue_material;

    sphere.GetComponent<MeshRenderer>().materials = materials;

    var materials_1 = sphere_1.GetComponent<MeshRenderer>().materials;
    materials_1[0] = is_inbound_ ? red_material : blue_material;
    sphere_1.GetComponent<MeshRenderer>().materials = materials_1;
  }

  public void SetGateOccupied() {
    airport_.SetGateStatus(parking_gate_name_, GateInfo.GateStatus.OCCUPIED);
  }

  public void SetGateEmpty() {
    airport_.SetGateStatus(parking_gate_name_, GateInfo.GateStatus.EMPTY);
  }

  public string GetCurrentStateName() {
    return states_[current_state_id_].Name;
  }

  public List<GameObject> GetCollisionTrackingObjects() {
    return collision_tracking_objects_;
  }

  public void ClearCollisionTrackingObjects() {
    collision_tracking_objects_.Clear();
    Debug.Log(GetTailName() + " clear collision tracking objects @" + GetCurrentStateName());
  }

  public void RemoveObjectFromCollisionTrackingObjects(GameObject obj) {
    collision_tracking_objects_.Remove(obj);
  }

  public void TaxiRaycastCollideCallback(float direction, RaycastHit hit) {
    Debug.Log(hit.transform.name + ". distance = " + hit.distance);
    GameObject aircraft = hit.transform.gameObject;
    string state_name = aircraft.GetComponent<StateMachine>().GetCurrentStateName();
    if (!ground_states_group_.Contains(state_name)) {
      Debug.Log("Ground states doesn't contain name: " + state_name);
      return;
    }
    if (in_gate_states_group_.Contains(state_name)) {
      // in gate aircrafts don't block other's moving
      return;
    }
    if (collision_tracking_objects_.Contains(aircraft)) {
      Debug.Log("Aircraft already in tracking list.");
      return;
    }
    Debug.Log(GetTailName() + ": " + aircraft.GetComponent<StateMachine>().GetTailName() +
              " added to the tracking list @ " + state_name);
    collision_tracking_objects_.Add(aircraft);
  }

  public void SetHoldOnTrafficObject(GameObject obj) {
    hold_on_traffic_object_ = obj;
  }

  public GameObject GetHoldOnTrafficObject() {
    return hold_on_traffic_object_;
  }

  public GateInfo GetGateInfo(string gate_name) {
    return airport_.GetGateInfo(gate_name);
  }

  private void OnTriggerEnter(Collider other) {
    Debug.Log("Collide happened." + other.gameObject.name);
    if (other.gameObject.name == "EnterRunwayDetection") {
      enter_runway_ = true;
    }
    if (other.gameObject.name == "PassR5Detection") {
      pass_r5_exit_ = true;
    }
    if (other.gameObject.name == "PassR3Detection") {
      pass_r3_exit_ = true;
    }
    if (other.gameObject.name == "PassR1Detection") {
      pass_r1_exit_ = true;
    }
  }

  public bool IsEnterRunway() {
    return enter_runway_;
  }

  public bool IsPassR5() {
    return pass_r5_exit_;
  }

  public bool IsPassR3() {
    return pass_r3_exit_;
  }

  public bool IsPassR1() {
    return pass_r1_exit_;
  }

  public void ResetExitDetection() {
    enter_runway_ = false;
    pass_r5_exit_ = false;
    pass_r3_exit_ = false;
    pass_r1_exit_ = false;
  }

  public string GetTailName() {
    return tail_name_;
  }

  public ApproachInfo GetApproachInfo() {
    return landing_approach_info_;
  }

  public void SetGroundRadar(bool on_off, bool direction) {
    flight_control_.SetGroundRadar(on_off, direction);
    if (!on_off) {
      // clear collision objects list
      ClearCollisionTrackingObjects();
    }
  }
}
