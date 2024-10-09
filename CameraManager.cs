using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraManager : MonoBehaviour {

  private float camera_zoom;
  private float camera_zoom_change_speed = 0.25f; // change 0.05x per second;
  private float camera_rotation_speed = 15.0f; // 5 degrees per second;
  private float default_tower_field_of_view = 50.0f; // default tower field of view
  private float default_aircraft_following_camera_field_of_view = 22.0f;
  private float aircraft_following_camera_look_down_degree = 11.0f;
  private Vector3 tower_camera_offset = new Vector3(5.0f, 0.0f, 0.0f);
  private Vector3 aircraft_following_camera_offset = new Vector3(0.0f, 5.9f, -21.3f);

  private bool is_camera_on_tower;

  private GameObject current_following_aircraft_object;
  private GameObject tower_sphere_object;

  [SerializeField] GameObject flights_manager;

  public void SetCameraToCurrentAircraft() {
    if (is_camera_on_tower) {
      CameraSwitchBetweenTowerAndAircraft();
    } else {
      current_following_aircraft_object = flights_manager.GetComponent<FlightsManager>().CurrentAircraft();
    }
  }

  public void SetCameraToTower() {
    if (!is_camera_on_tower) {
      CameraSwitchBetweenTowerAndAircraft();
    }
  }

  public void CameraSwitchBetweenTowerAndAircraft() {
    if (is_camera_on_tower) {
      current_following_aircraft_object = flights_manager.GetComponent<FlightsManager>().CurrentAircraft();
      if (current_following_aircraft_object == null) {
        return;
      }
      this.GetComponent<Camera>().fieldOfView = default_aircraft_following_camera_field_of_view;
      is_camera_on_tower = false;
    } else {
      is_camera_on_tower = true;
      InitializeTowerCamera();
    }
  }

  // Make far object bigger
  public void CameraZoomIn() {
    camera_zoom -= camera_zoom_change_speed * Time.deltaTime;
    UpdateCameraZoom();
  }

  // Make far object smaller
  public void CameraZoomOut() {
    camera_zoom += camera_zoom_change_speed * Time.deltaTime;
    UpdateCameraZoom();
  }

  public void CameraDown() {
    transform.Rotate(Vector3.right, camera_rotation_speed * Time.deltaTime);
  }

  public void CameraUp() {
    transform.Rotate(Vector3.right, -camera_rotation_speed * Time.deltaTime);
  }

  public void CameraLeft() {
    transform.Rotate(Vector3.up, -camera_rotation_speed * Time.deltaTime, Space.World);
  }

  public void CameraRight() {
    transform.Rotate(Vector3.up, camera_rotation_speed * Time.deltaTime, Space.World);
  }

  private void InitializeTowerCamera() {
    transform.position = tower_sphere_object.transform.position + tower_camera_offset;
    transform.rotation = Quaternion.Euler(1.0f, 105.0f, 0.0f);
    this.GetComponent<Camera>().fieldOfView = default_tower_field_of_view;
    camera_zoom = 1.0f;
  }

  // Start is called before the first frame update
  void Start() {
    float screen_width = (float)Screen.width;
    float screen_height = (float)Screen.height;
    float rect_width = (screen_width - 512) / screen_width;
    float rect_height = (screen_height - 270) / screen_height;
    Debug.Log("Screen width " + screen_width + " Screen height " + screen_height);
    this.GetComponent<Camera>().rect = new Rect(0, 1 - rect_height, rect_width, rect_height);
    camera_zoom = 1.0f;
    is_camera_on_tower = true;
    tower_sphere_object = GameObject.Find("PaloAltoAirport/Tower/Sphere").gameObject;
    InitializeTowerCamera();
  }

  private void UpdateCameraZoom() {
    this.GetComponent<Camera>().fieldOfView = (is_camera_on_tower ? 
                                                default_tower_field_of_view : default_aircraft_following_camera_field_of_view)
                                              * camera_zoom;
  }

  // Update is called once per frame
  void Update() {
    if (!is_camera_on_tower) {
      if (current_following_aircraft_object == null) {
        is_camera_on_tower = true;
        InitializeTowerCamera();
        return;
      }
      transform.rotation = current_following_aircraft_object.transform.rotation;
      transform.RotateAround(transform.position, transform.right, aircraft_following_camera_look_down_degree);
      transform.position = current_following_aircraft_object.transform.position
                             + aircraft_following_camera_offset.x * current_following_aircraft_object.transform.right
                             + aircraft_following_camera_offset.y * current_following_aircraft_object.transform.up
                             + aircraft_following_camera_offset.z * current_following_aircraft_object.transform.forward;
    }
  }
}
