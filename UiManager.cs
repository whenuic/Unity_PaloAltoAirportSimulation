using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour {

  [SerializeField] private GameObject flights_manager;

  // Autopilot switches
  [SerializeField] private Toggle autopilot_switch;
  [SerializeField] private Toggle auto_heading_switch;
  [SerializeField] private Toggle auto_altitude_switch;
  [SerializeField] private Toggle auto_speed_switch;
  [SerializeField] private Toggle auto_vertical_rate_display_switch;

  // Autopilot setting buttons, interactive
  [SerializeField] private Button auto_heading_right_button;
  [SerializeField] private Button auto_heading_left_button;
  [SerializeField] private Button auto_altitude_up_button;
  [SerializeField] private Button auto_altitude_down_button;
  [SerializeField] private Button auto_speed_up_button;
  [SerializeField] private Button auto_speed_down_button;
  [SerializeField] private Button auto_vertical_rate_display_up_button;
  [SerializeField] private Button auto_vertical_rate_display_down_button;

  // Autopilot input fields, interactive
  [SerializeField] private TMP_InputField auto_heading_target_box;
  [SerializeField] private TMP_InputField auto_altitude_target_box;
  [SerializeField] private TMP_InputField auto_speed_target_box;
  [SerializeField] private TMP_InputField auto_vertical_rate_display_target_box;

  // Engine switch, interactive
  [SerializeField] private Toggle engine_switch;
  
  // Input manuever indicators, non-interactive
  [SerializeField] private Slider throttle_slider;
  [SerializeField] private Slider roll_slider;
  [SerializeField] private Slider pitch_slider;
  [SerializeField] private Slider flap_slider;

  // States displays, non-interactive
  [SerializeField] private TextMeshProUGUI pitch_angle_display;
  [SerializeField] private TextMeshProUGUI heading_display;
  [SerializeField] private TextMeshProUGUI speed_display;
  [SerializeField] private TextMeshProUGUI altitude_display;
  [SerializeField] private TextMeshProUGUI vertical_rate_display;

  // Debug data display, non-interactive
  [SerializeField] private TextMeshProUGUI current_aircraft_state_data;
  [SerializeField] private TextMeshProUGUI current_aircraft_debug_data;

  private bool initial_synced = false; // Check if a newly established aircraft has intial status synced.


  private float sensor_display_update_timer = 0.0f; // used to count the ui update interval
  private float sensor_display_update_interval = 0.1f; // 5 times per second
  private bool need_update_sensor_display = false; // flag to tell whether the current update function should refresh the display
  private AircraftStateOutput aircraft_state_output; // allocate state output, also can determine autopilot state and whether to send joystick command to aircraft control or not

  private static Toggle.ToggleEvent emptyToggleEvent = new Toggle.ToggleEvent();

  public void SetAutoHeadingValue() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoHeadingValue(System.Convert.ToInt32(auto_heading_target_box.text));
  }

  public void SetAutoAltitudeValue() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoAltitudeValue(System.Convert.ToInt32(auto_altitude_target_box.text));
  }

  public void SetAutoSpeedValue() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoSpeedValue(System.Convert.ToInt32(auto_speed_target_box.text));
  }

  public void SetAutoVerticalRateValue() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoVerticalRateValue(System.Convert.ToInt32(auto_vertical_rate_display_target_box.text));
  }

  public void SetAutoHeadingText(string text) {
    auto_heading_target_box.text = text;
  }

  public void SetAutoAltitudeText(string text) {
    auto_altitude_target_box.text = text;
  }

  public void SetAutoSpeedText(string text) {
    auto_speed_target_box.text = text;
  }

  public void SetAutoVerticalRateText(string text) {
    auto_vertical_rate_display_target_box.text = text;
  }

  public void SetThrottleSlider(float value) {
    throttle_slider.value = value;
  }

  public void SetAutopilotSwitch(bool value) {
    Toggle.ToggleEvent original_toggle_event = autopilot_switch.onValueChanged;
    autopilot_switch.onValueChanged = emptyToggleEvent;
    autopilot_switch.isOn = value;
    autopilot_switch.onValueChanged = original_toggle_event;
  }

  public void SetAutoHeadingSwitch(bool value) {
    Toggle.ToggleEvent original_toggle_event = auto_heading_switch.onValueChanged;
    auto_heading_switch.onValueChanged = emptyToggleEvent;
    auto_heading_switch.isOn = value;
    auto_heading_switch.onValueChanged = original_toggle_event;
  }

  public void SetAutoAltitudeSwitch(bool value) {
    Toggle.ToggleEvent original_toggle_event = auto_altitude_switch.onValueChanged;
    auto_altitude_switch.onValueChanged = emptyToggleEvent;
    auto_altitude_switch.isOn = value;
    auto_altitude_switch.onValueChanged = original_toggle_event;
  }

  public void SetAutoSpeedSwitch(bool value) {
    Toggle.ToggleEvent original_toggle_event = auto_speed_switch.onValueChanged;
    auto_speed_switch.onValueChanged = emptyToggleEvent;
    auto_speed_switch.isOn = value;
    auto_speed_switch.onValueChanged = original_toggle_event;
  }

  public void SetAutoVerticalRateSwitch(bool value) {
    Toggle.ToggleEvent original_toggle_event = auto_vertical_rate_display_switch.onValueChanged;
    auto_vertical_rate_display_switch.onValueChanged = emptyToggleEvent;
    auto_vertical_rate_display_switch.isOn = value;
    auto_vertical_rate_display_switch.onValueChanged = original_toggle_event;
  }

  public void SetEngineSwitch(bool value) {
    Toggle.ToggleEvent original_toggle_event = engine_switch.onValueChanged;
    engine_switch.onValueChanged = emptyToggleEvent;
    engine_switch.isOn = value;
    engine_switch.onValueChanged = original_toggle_event;
  }

  public void ToggleEngine() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().ToggleEngine();
  }

  public void ThrottleUp(float dt) {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().ThrottleUp(dt);
  }

  public void ThrottleDown(float dt) {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().ThrottleDown(dt);
  }

  public void SetRollInput(float roll) {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetRollInput(roll);
  }

  public void SetPitchInput(float pitch) {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetPitchInput(pitch);
  }

  public void SetPitchSlider(float pitch) {
    pitch_slider.value = pitch;
  }

  public void SetRollSlider(float roll) {
    roll_slider.value = roll;
  }

  public void ApplyBrake() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().ApplyBrake();
  }

  public void ReleaseBrake() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().ReleaseBrake();
  }

  public int ExtendFlap() {
    return flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().ExtendFlap();
  }

  public int RetreatFlap() {
    return flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().RetreatFlap();
  }

  public void SetFlapSlider(int value) {
    flap_slider.value = value;
  }

  public void TrimIncrease(float dt) {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().TrimIncrease(dt);
  }

  public void TrimDecrease(float dt) {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().TrimDecrease(dt);
  }

  public void SetAutopilotSwitch() {
    // autopilot_switch.isOn = !autopilot_switch.isOn;
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutopilotSwitch(autopilot_switch.isOn);
  }

  public void SetAutoHeadingSwitch() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoHeadingSwitch(auto_heading_switch.isOn);
  }

  public void SetAutoAltitudeSwitch() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoAltitudeSwitch(auto_altitude_switch.isOn);
  }

  public void SetAutoSpeedSwitch() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoSpeedSwitch(auto_speed_switch.isOn);
  }

  public void SetAutoVerticalRateSwitch() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().SetAutoVerticalRateSwitch(auto_vertical_rate_display_switch.isOn);
  }

  public void AutoHeadingRight() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoHeadingRight();
  }
  public void AutoHeadingLeft() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoHeadingLeft();
  }

  public void AutoAltitudeUp() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoAltitudeUp();
  }

  public void AutoAltitudeDown() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoAltitudeDown();
  }

  public void AutoSpeedUp() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoSpeedUp();
  }

  public void AutoSpeedDown() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoSpeedDown();
  }

  public void AutoVerticalRateUp() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoVerticalRateUp();
  }

  public void AutoVerticalRateDown() {
    flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().AutoVerticalRateDown();
  }

  public AircraftStateOutput GetAircraftStateOutput() {
    return flights_manager.GetComponent<FlightsManager>().CurrentAircraft().GetComponent<FlightControl>().GetAircraftStateOutput();
  }

  private void DeactivateAll() {
    Debug.Log("DeactivateAll called.");
    if (autopilot_switch == null) {
      return;
    }
    // Autopilot switches
    autopilot_switch.gameObject.SetActive(false);
    auto_heading_switch.gameObject.SetActive(false);
    auto_altitude_switch.gameObject.SetActive(false);
    auto_speed_switch.gameObject.SetActive(false);
    auto_vertical_rate_display_switch.gameObject.SetActive(false);

    // Autopilot setting buttons, interactive
    auto_heading_right_button.gameObject.SetActive(false);
    auto_heading_left_button.gameObject.SetActive(false);
    auto_altitude_up_button.gameObject.SetActive(false);
    auto_altitude_down_button.gameObject.SetActive(false);
    auto_speed_up_button.gameObject.SetActive(false);
    auto_speed_down_button.gameObject.SetActive(false);
    auto_vertical_rate_display_up_button.gameObject.SetActive(false);
    auto_vertical_rate_display_down_button.gameObject.SetActive(false);

    // Autopilot input fields, interactive
    auto_heading_target_box.gameObject.SetActive(false);
    auto_altitude_target_box.gameObject.SetActive(false);
    auto_speed_target_box.gameObject.SetActive(false);
    auto_vertical_rate_display_target_box.gameObject.SetActive(false);

    // Engine switch, interactive
    engine_switch.gameObject.SetActive(false);

    // Input manuever indicators, non-interactive
    throttle_slider.gameObject.SetActive(false);
    roll_slider.gameObject.SetActive(false);
    pitch_slider.gameObject.SetActive(false);
    flap_slider.gameObject.SetActive(false);

    // States displays, non-interactive
    pitch_angle_display.gameObject.SetActive(false);
    heading_display.gameObject.SetActive(false);
    speed_display.gameObject.SetActive(false);
    altitude_display.gameObject.SetActive(false);
    vertical_rate_display.gameObject.SetActive(false);

    // Debug data display, non-interactive
    current_aircraft_state_data.gameObject.SetActive(false);
    current_aircraft_debug_data.gameObject.SetActive(false);
  }

  private void ActivateAll() {
    Debug.Log("ActivateAll called.");
    // Autopilot switches
    autopilot_switch.gameObject.SetActive(true);
    auto_heading_switch.gameObject.SetActive(true);
    auto_altitude_switch.gameObject.SetActive(true);
    auto_speed_switch.gameObject.SetActive(true);
    auto_vertical_rate_display_switch.gameObject.SetActive(true);

    // Autopilot setting buttons, interactive
    auto_heading_right_button.gameObject.SetActive(true);
    auto_heading_left_button.gameObject.SetActive(true);
    auto_altitude_up_button.gameObject.SetActive(true);
    auto_altitude_down_button.gameObject.SetActive(true);
    auto_speed_up_button.gameObject.SetActive(true);
    auto_speed_down_button.gameObject.SetActive(true);
    auto_vertical_rate_display_up_button.gameObject.SetActive(true);
    auto_vertical_rate_display_down_button.gameObject.SetActive(true);

    // Autopilot input fields, interactive
    auto_heading_target_box.gameObject.SetActive(true);
    auto_altitude_target_box.gameObject.SetActive(true);
    auto_speed_target_box.gameObject.SetActive(true);
    auto_vertical_rate_display_target_box.gameObject.SetActive(true);

    // Engine switch, interactive
    engine_switch.gameObject.SetActive(true);

    // Input manuever indicators, non-interactive
    throttle_slider.gameObject.SetActive(true);
    roll_slider.gameObject.SetActive(true);
    pitch_slider.gameObject.SetActive(true);
    flap_slider.gameObject.SetActive(true);

    // States displays, non-interactive
    pitch_angle_display.gameObject.SetActive(true);
    heading_display.gameObject.SetActive(true);
    speed_display.gameObject.SetActive(true);
    altitude_display.gameObject.SetActive(true);
    vertical_rate_display.gameObject.SetActive(true);

    // Debug data display, non-interactive
    current_aircraft_state_data.gameObject.SetActive(true);
    current_aircraft_debug_data.gameObject.SetActive(true);
  }

  public void SetInteractiveAll(bool value) {
    autopilot_switch.interactable = value;
    auto_heading_switch.interactable = value;
    auto_altitude_switch.interactable = value;
    auto_speed_switch.interactable = value;
    auto_vertical_rate_display_switch.interactable = value;

    auto_heading_right_button.interactable = value;
    auto_heading_left_button.interactable = value;
    auto_altitude_up_button.interactable = value;
    auto_altitude_down_button.interactable = value;
    auto_speed_up_button.interactable = value;
    auto_speed_down_button.interactable = value;
    auto_vertical_rate_display_up_button.interactable = value;
    auto_vertical_rate_display_down_button.interactable = value;

    auto_heading_target_box.interactable = value;
    auto_altitude_target_box.interactable = value;
    auto_speed_target_box.interactable = value;
    auto_vertical_rate_display_target_box.interactable = value;

    engine_switch.interactable = value;

    throttle_slider.interactable = value;
    roll_slider.interactable = value;
    pitch_slider.interactable = value;
    flap_slider.interactable = value;
  }

  // Start is called before the first frame update
  void Start() {
    DeactivateAll();
  }

  private void SyncOnEnable() {
    Debug.Log("Sync on enable called.");
    SetAutopilotSwitch(aircraft_state_output.is_autopilot_on);
    SetAutoHeadingSwitch(aircraft_state_output.is_auto_heading_on);
    SetAutoAltitudeSwitch(aircraft_state_output.is_auto_altitude_on);
    SetAutoSpeedSwitch(aircraft_state_output.is_auto_speed_on);
    SetAutoVerticalRateSwitch(aircraft_state_output.is_auto_vertical_rate_display_on);

    SetAutoHeadingText(aircraft_state_output.auto_heading_target.ToString());
    SetAutoAltitudeText(aircraft_state_output.auto_altitude_target.ToString());
    SetAutoSpeedText(aircraft_state_output.auto_speed_target.ToString());
    SetAutoVerticalRateText(aircraft_state_output.auto_vertical_rate_display_target.ToString());

    SetEngineSwitch(aircraft_state_output.engine_switch);
    SetFlapSlider(aircraft_state_output.flap_degree_table_pointer);
    SetRollSlider(aircraft_state_output.roll_input);
    SetThrottleSlider(aircraft_state_output.thrust_input);
    SetPitchSlider(aircraft_state_output.elevator_input);
  }

  private void OnEnable() {
    if (flights_manager != null && flights_manager.GetComponent<FlightsManager>().GetCurrentAircraftIndex() >= 0) {
      ActivateAll();
      aircraft_state_output = GetAircraftStateOutput();
      SetInteractiveAll(!aircraft_state_output.is_controlled_by_flights_manager); // not interactive if by flights manager
      SyncOnEnable();
      sensor_display_update_timer = 0.0f;
      need_update_sensor_display = false;
    }
  }

  private void OnDisable() {
    DeactivateAll();
  }

  // Update is called once per frame
  void Update() {
    if (flights_manager.GetComponent<FlightsManager>().CurrentAircraft() == null) {
      this.enabled = false;
      return;
    }
    float dt_frame = Time.deltaTime;
    sensor_display_update_timer += dt_frame;
    if (sensor_display_update_timer > sensor_display_update_interval) {
      need_update_sensor_display = true;
      sensor_display_update_timer -= sensor_display_update_interval;
    }

    // Keyboard I, toggle engine
    if (Input.GetKeyDown(KeyCode.I)) {
      ToggleEngine();
    }

    // Joystick Y key or "=" key, throttle up
    if (Input.GetKey(KeyCode.JoystickButton3) || Input.GetKey(KeyCode.Equals)) {
      ThrottleUp(dt_frame);
    }
    // Joystick A key or "-" key, throttle down
    if (Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.Minus)) {
      ThrottleDown(dt_frame);
    }

    // Joystick X Key, brake applied
    if (Input.GetKey(KeyCode.JoystickButton2)) {
      ApplyBrake();
    }

    if (Input.GetKeyUp(KeyCode.JoystickButton2)) {
      ReleaseBrake();
    }

    // Joystick RB or "]" key, extend flap
    if (Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.RightBracket)) {
      flap_slider.value = ExtendFlap();
    }

    if (Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.LeftBracket)) {
      flap_slider.value = RetreatFlap();
    }

    // Trim decrease cause nose up
    if (Input.GetKey(KeyCode.PageDown)) {
      TrimDecrease(dt_frame);
    }

    // Trim increase cause nose down
    if (Input.GetKey(KeyCode.PageUp)) {
      TrimIncrease(dt_frame);
    }

    float roll_input;
    if (aircraft_state_output.is_autopilot_on && aircraft_state_output.is_auto_heading_on) {
      roll_input = aircraft_state_output.roll_input;
    } else {
      roll_input = Input.GetAxis("Horizontal");
      SetRollInput(roll_input);
    }


    float pitch_input;
    if (aircraft_state_output.is_autopilot_on && aircraft_state_output.is_auto_altitude_on || aircraft_state_output.is_controlled_by_flights_manager) {
      pitch_input = aircraft_state_output.elevator_input;
    } else {
      pitch_input = Input.GetAxis("Vertical");
      SetPitchInput(pitch_input);
    }
    

    // Update UI display
    // throttle slider value is updated by callback
    roll_slider.value = roll_input;
    pitch_slider.value = pitch_input;
    
    if (need_update_sensor_display) {
      need_update_sensor_display = false;

      // Get updated states
      aircraft_state_output = GetAircraftStateOutput();

      // Update display panel value
      pitch_angle_display.SetText("PIT" + "\n" + aircraft_state_output.pitch_angle.ToString("F1"));
      heading_display.SetText("HDG" + "\n" + aircraft_state_output.heading.ToString("F0"));
      speed_display.SetText("SPD" + "\n" + aircraft_state_output.speed.ToString("F0"));
      altitude_display.SetText("ALT" + "\n" + aircraft_state_output.altitude.ToString("F0"));
      vertical_rate_display.SetText("VRT" + "\n" + aircraft_state_output.vertical_rate_display.ToString("F0"));

      

      // Update Autopilot UI
      // autopilot_switch.isOn = state_output.is_autopilot_on;
      // auto_heading_switch.isOn = state_output.is_auto_heading_on;
      // auto_altitude_switch.isOn = state_output.is_auto_altitude_on;
      // auto_speed_switch.isOn = state_output.is_auto_speed_on;
      // auto_vertical_rate_display_switch.isOn = state_output.is_auto_vertical_rate_display_on;

      current_aircraft_state_data.SetText(
        "AOA=" + aircraft_state_output.angle_of_attack.ToString("F1") +
        "\n" + "Thrust=" + aircraft_state_output.thrust_force.ToString("F1") +
        "\n" + "LeftWingLift=" + aircraft_state_output.left_wing_lift_force_magnitude.ToString("F0") +
        "\n" + "SlideAngle=" + aircraft_state_output.slide_angle.ToString("F1") + "deg" +
        "\n" + "u=" + aircraft_state_output.u +
        "\n" + "YawTorque=" + aircraft_state_output.local_yaw_torque_y.ToString("F1") +
        "\n" + "Trim: " + aircraft_state_output.tail_wing_trim_angle.ToString("F1") +
        "\n" + "Autopilot: " + (aircraft_state_output.is_autopilot_on ? "ON " : "OFF") + " Hea: " + (aircraft_state_output.is_auto_heading_on ? "ON " : "OFF") + " Alt: " + (aircraft_state_output.is_auto_altitude_on ? "ON " : "OFF") + " Spd: " + (aircraft_state_output.is_auto_speed_on ? "ON " : "OFF") + " VRt: " + (aircraft_state_output.is_auto_vertical_rate_display_on ? "ON" : "OFF"));

      current_aircraft_debug_data.SetText(aircraft_state_output.debug_data);
    }
  }
}