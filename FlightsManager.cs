using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public delegate void AddIndexToDestroyList(int index);
public delegate void SetCurrentByCardClick(int index);


public struct AircraftData {
  public GameObject aircraft;
  public float final_descending_control_update_timer;
  public float final_descending_control_update_interval;
}

public struct AircraftInstantiateInitialConditions {
  public bool is_inbound;
  public bool is_controlled_by_flights_manager;
  
  // common conditions
  public Vector3 position;
  public Vector3 quaternion_euler_rotation;
  public Vector3 initial_velocity;

  // outbound only
  public string parking_gate_name;

  // inbound only
  public float heading;
  public float speed;
  public float altitude;

  public Transform inbound_content;
  public Transform outbound_content;

  public GameObject card_prefab;

  public string tail_name;
}

public class FlightsManager : MonoBehaviour {
  public FlightsManager() {
    
  }

  [SerializeField] GameObject ui_manager;
  
  // The card banner
  public Transform outgoing_content;
  public Transform incoming_content;
  public GameObject card_prefab;

  public GameObject aircraft_prefab;

  public AircraftData[] aircraft_pool = new AircraftData[20];
  // Current aircraft can be negative. Negative means no seleted aircraft for UI output.
  // UI manager should be able to handle the null of this index.
  private int current_aircraft_index = -1;

  // Used to store the indexes that need to be destroyed in the next update function.
  private List<int> need_to_destroy_index_list = new List<int>();

  private float incoming_traffic_interval_; // how many minutes are two incoming aircrafts apart from
  private float incoming_traffic_timer_;
  private int max_incoming_traffic_allowed_ = 1; // how many incoming traffic is allowed

  private int total_landing_count_;
  private int total_taking_off_count_;
  private int total_incoming_count_;

  public int NextAvailableAircraftPoolIndex() {
    int index = -1;
    for (int i = 0; i < aircraft_pool.Length; i++) {
      if (aircraft_pool[i].aircraft == null) {
        index = i;
        break;
      }
    }
    return index;
  }

  public GameObject CurrentAircraft() {
    if (GetCurrentAircraftIndex() < 0) {
      return null;
    }
    if (aircraft_pool[GetCurrentAircraftIndex()].aircraft == null) {
      NullifyCurrentAircraftIndex();
      return null;
    }
    return aircraft_pool[GetCurrentAircraftIndex()].aircraft;
  }

  public void SetCurrentAircraftIndex(int index) {
    if (GetCurrentAircraftIndex() >= 0 && GetCurrentAircraftIndex() != index) {
      if (aircraft_pool[GetCurrentAircraftIndex()].aircraft != null) {
        aircraft_pool[GetCurrentAircraftIndex()].aircraft.GetComponent<StateMachine>().SetCardCurrent(false);
        aircraft_pool[GetCurrentAircraftIndex()].aircraft.GetComponent<FlightControl>().SetConnectToUi(false);
        ui_manager.GetComponent<UiManager>().enabled = false;
      }
    }
    if (index == -1) {
      NullifyCurrentAircraftIndex();
      ui_manager.GetComponent<UiManager>().enabled = false;
      return;
    }
    current_aircraft_index = index;
    aircraft_pool[GetCurrentAircraftIndex()].aircraft.GetComponent<StateMachine>().SetCardCurrent(true);
    aircraft_pool[GetCurrentAircraftIndex()].aircraft.GetComponent<FlightControl>().SetConnectToUi(true);
    ui_manager.GetComponent<UiManager>().enabled = true;
  }

  public void NullifyCurrentAircraftIndex() {
    current_aircraft_index = -1;
  }

  public int GetCurrentAircraftIndex() {
    return current_aircraft_index;
  }

  public void AddIndexToDestroyList(int index) {
    need_to_destroy_index_list.Add(index);
  }

  public string GenerateRandomName() {
    string name = "N";
    for (int i = 0; i < 2; i++) {
      char c = (char)(Random.Range(0, 26) + 65);
      name += c;
    }
    for (int i = 0; i < 2; i++) {
      name += Random.Range(0, 10).ToString();
    }
    return name;
  }

  public void InstantiateAircraftAt(int i, AircraftInstantiateInitialConditions cond) {
    aircraft_pool[i].aircraft = Instantiate(aircraft_prefab, cond.position, Quaternion.Euler(cond.quaternion_euler_rotation));
    aircraft_pool[i].aircraft.GetComponent<StateMachine>().SetInitialCondition(cond);
    aircraft_pool[i].aircraft.GetComponent<StateMachine>().SetFlightsManagerDestroyCallback(this.AddIndexToDestroyList);
    aircraft_pool[i].aircraft.GetComponent<StateMachine>().SetFlightsManagerPoolIndex(i);
    Debug.Log("Instantiate called completed.");
    aircraft_pool[i].aircraft.GetComponent<FlightControl>().SetUiManager(ui_manager);
    aircraft_pool[i].aircraft.GetComponent<FlightControl>().SetVelocity(cond.initial_velocity);

    aircraft_pool[i].aircraft.GetComponent<FlightControl>().SetIsControlledByFightsManager(cond.is_controlled_by_flights_manager);
    Debug.Log("FlightControl settings done.");
    aircraft_pool[i].final_descending_control_update_interval = 0.25f;
    aircraft_pool[i].final_descending_control_update_timer = 0.0f;

    // Pass card click set current delegate to StateMachine. It will be further passed to CardHander.
    aircraft_pool[i].aircraft.GetComponent<StateMachine>().AssignSetCurrentByCardClickDelegate(this.SetCurrentAircraftIndex);
  }

  // Start is called before the first frame update
  void Start() {
    incoming_traffic_interval_ = 1.5f; // every 1 minute there will be an aircraft coming.
    incoming_traffic_timer_ = 0;

    // First aircraft is outbound
    AircraftInstantiateInitialConditions cond = new AircraftInstantiateInitialConditions();
    cond.is_inbound = false;
    cond.is_controlled_by_flights_manager = true;
    cond.position = new Vector3(500.0f, 3.283f, 1500.0f);
    cond.quaternion_euler_rotation = new Vector3(-8.045f, 0.0f, 0.0f);
    cond.initial_velocity = new Vector3(0, 0, 0);
    cond.parking_gate_name = "P1";
    cond.inbound_content = incoming_content;
    cond.outbound_content = outgoing_content;
    cond.card_prefab = card_prefab;
    cond.tail_name = GenerateRandomName();
    InstantiateAircraftAt(0, cond);

    // Second aircraft is inbound
    // AircraftInstantiateInitialConditions cond_1 = new AircraftInstantiateInitialConditions();
    // cond_1.is_inbound = true;
    // cond_1.is_controlled_by_flights_manager = true;
    // cond_1.position = new Vector3(1780.0f, 235.28f, -2229.0f);
    // cond_1.quaternion_euler_rotation = new Vector3(-8.045f, 48.0f, 0.0f);
    // Vector3 velocity_vector = new Vector3(38.5833f, 0, 0);
    // cond_1.initial_velocity = Quaternion.Euler(0, -48.0f, 0) * velocity_vector;
    // cond_1.heading = 48.0f;
    // cond_1.speed = 75.0f;
    // cond_1.altitude = 700.0f; // in ft
    // cond_1.inbound_content = incoming_content;
    // cond_1.outbound_content = outgoing_content;
    // cond_1.card_prefab = card_prefab;
    // cond_1.tail_name = GenerateRandomName();
    // InstantiateAircraftAt(1, cond_1);

    // Third aircraft is outbound
    AircraftInstantiateInitialConditions cond_2 = new AircraftInstantiateInitialConditions();
    cond_2.is_inbound = false;
    cond_2.is_controlled_by_flights_manager = true;
    cond_2.position = new Vector3(500.0f, 4.683f, 1500.0f);
    cond_2.quaternion_euler_rotation = new Vector3(-8.045f, 0.0f, 0.0f);
    cond_2.initial_velocity = new Vector3(0, 0, 0);
    cond_2.parking_gate_name = "Q1";
    cond_2.inbound_content = incoming_content;
    cond_2.outbound_content = outgoing_content;
    cond_2.card_prefab = card_prefab;
    cond_2.tail_name = GenerateRandomName();
    InstantiateAircraftAt(2, cond_2);
  }

  void DestroyAllChildrenGameObject(Transform transform) {
    for (int i = transform.childCount - 1; i >= 0; i--) {
      if (transform.GetChild(i).transform.childCount > 0) {
        DestroyAllChildrenGameObject(transform.GetChild(i).transform);
      }
    }
  }

  // Update is called once per frame
  void Update() {
    float dt = Time.deltaTime;

    incoming_traffic_timer_ += dt;
    if (incoming_traffic_timer_ >= incoming_traffic_interval_ * 60.0f) {
      incoming_traffic_timer_ -= incoming_traffic_interval_ * 60.0f;
      if (total_incoming_count_ < max_incoming_traffic_allowed_) {
        int index = NextAvailableAircraftPoolIndex();
        if (index >= 0) {
          AircraftInstantiateInitialConditions cond = new AircraftInstantiateInitialConditions();
          cond.is_inbound = true;
          cond.is_controlled_by_flights_manager = true;
          cond.position = new Vector3(1780.0f, 235.28f, -2229.0f);
          cond.quaternion_euler_rotation = new Vector3(-8.045f, 48.0f, 0.0f);
          Vector3 velocity_vector = new Vector3(38.5833f, 0, 0);
          cond.initial_velocity = Quaternion.Euler(0, -48.0f, 0) * velocity_vector;
          cond.heading = 48.0f;
          cond.speed = 75.0f;
          cond.altitude = 700.0f; // in ft
          cond.inbound_content = incoming_content;
          cond.outbound_content = outgoing_content;
          cond.card_prefab = card_prefab;
          cond.tail_name = GenerateRandomName();
          InstantiateAircraftAt(index, cond);
        }
        total_incoming_count_ += 1;
      }
    }

    // First destroy aircraft if necessary
    foreach (int index in need_to_destroy_index_list) {
      if (aircraft_pool[index].aircraft != null && !aircraft_pool[index].aircraft.activeSelf) {
        DestroyAllChildrenGameObject(aircraft_pool[index].aircraft.transform);
        Destroy(aircraft_pool[index].aircraft);
        aircraft_pool[index].aircraft = null;
      }
    }
  }
}