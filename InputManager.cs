using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{

  [SerializeField] GameObject main_camera;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    // ****************************  Camera related  ****************************
    // T switch between tower and aircrafts
    if (Input.GetKeyDown(KeyCode.T)) {
      main_camera.GetComponent<CameraManager>().CameraSwitchBetweenTowerAndAircraft();
    }

    // Camera zoom in, make far object bigger
    if (Input.GetKey(KeyCode.KeypadPlus)) {
      main_camera.GetComponent<CameraManager>().CameraZoomIn();
    }

    // Camera zoom out, make far object smaller
    if (Input.GetKey(KeyCode.KeypadMinus)) {
      main_camera.GetComponent<CameraManager>().CameraZoomOut();
    }

    // Camera down
    if (Input.GetKey(KeyCode.Keypad5)) {
      main_camera.GetComponent<CameraManager>().CameraDown();
    }

    // Camera up
    if (Input.GetKey(KeyCode.Keypad8)) {
      main_camera.GetComponent<CameraManager>().CameraUp();
    }

    // Camera left
    if (Input.GetKey(KeyCode.Keypad4)) {
      main_camera.GetComponent<CameraManager>().CameraLeft();
    }

    // Camera right
    if (Input.GetKey(KeyCode.Keypad6)) {
      main_camera.GetComponent<CameraManager>().CameraRight();
    }

    // ****************************  Something Else related  ****************************

  }
}
