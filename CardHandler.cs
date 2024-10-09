using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHandler : MonoBehaviour, IPointerClickHandler {

  private CameraManager camera_;

  private GameObject current_indicator_image;
  private List<GameObject> runway_buttons = new List<GameObject>();
  private List<GameObject> approach_buttons = new List<GameObject>();
  private List<GameObject> exit_buttons = new List<GameObject>();
  private GameObject taxiway_automatic_button;

  private SetCurrentByCardClick card_click_callback_;

  private int flight_manager_pool_index_;

  private bool is_current_ = false;

  public void AssignSetCurrentByCardClick(SetCurrentByCardClick fcn, int pool_index) {
    card_click_callback_ = fcn;
    flight_manager_pool_index_ = pool_index;
  }

  public void OnPointerClick(PointerEventData pointerEventData) {
    // Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
    Debug.Log(name + " Game Object Clicked!");
    if (!is_current_) {
      is_current_ = true;
      card_click_callback_(flight_manager_pool_index_);
      camera_.SetCameraToCurrentAircraft();
    } else {
      is_current_ = false;
      card_click_callback_(-1);
      camera_.SetCameraToTower();
    }
  }

  public void DestroyAllChildrenGameObject(Transform transform) {
    for (int i = transform.childCount - 1; i >= 0; i--) {
      if (transform.GetChild(i).transform.childCount > 0) {
        DestroyAllChildrenGameObject(transform.GetChild(i).transform);
      }
    }
    Debug.Log("Childcount = " + transform.childCount.ToString());
  }

  // Let the StateMachine to call to inform the possible runway for departure/arrival.
  public void RunwayRequest(List<string> runways, RunwayChoosingDelegate runway_choosing_delegate) {
    int num_of_runways = runways.Count;
    if (num_of_runways > 4) {
      Debug.LogError("Departure runway choosing supports up to 4 runways but it has " + num_of_runways);
    }
    for (int i = 0; i < num_of_runways; i++) {
      string runway_name = runways[i];
      runway_buttons[i].SetActive(true);
      runway_buttons[i].transform.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = runway_name;
      runway_buttons[i].GetComponent<Button>().onClick.AddListener(() => runway_choosing_delegate(runway_name));
    }
  }

  public void ApproachRequest(List<string> approaches, ApproachChoosingDelegate approach_choosing_delegate) {
    int num_of_approaches = approaches.Count;
    if (num_of_approaches > 4) {
      Debug.LogError("Approach choosing supports up to 4 approaches but it has " + num_of_approaches);
    }
    for (int i = 0; i < num_of_approaches; i++) {
      string approach_name = approaches[i];
      approach_buttons[i].SetActive(true);
      approach_buttons[i].transform.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = approach_name;
      approach_buttons[i].GetComponent<Button>().onClick.AddListener(() => approach_choosing_delegate(approach_name));
    }
  }

  public void TaxiwayRequest(TaxiwayChoosingDelegate taxiway_choosing_delegate) {
    taxiway_automatic_button.SetActive(true);
    taxiway_automatic_button.GetComponent<Button>().onClick.AddListener(() => taxiway_choosing_delegate(true));
  }

  public void DeactivateTaxiwayChoosingButtons() {
    taxiway_automatic_button.GetComponent<Button>().onClick.RemoveAllListeners();
    taxiway_automatic_button.SetActive(false);
  }

  public void ExitRequest(List<string> exit_list, ExitChoosingDelegate exit_choosing_delegate) {
    int num_of_exits = exit_list.Count;
    if (num_of_exits > 4) {
      Debug.LogError("Exit choosing supports up to 4 exits but it has " + num_of_exits);
    }
    for (int i = 0; i < num_of_exits; i++) {
      string exit_name = exit_list[i];
      exit_buttons[i].SetActive(true);
      exit_buttons[i].transform.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = exit_name;
      exit_buttons[i].GetComponent<Button>().onClick.RemoveAllListeners();
      exit_buttons[i].GetComponent<Button>().onClick.AddListener(() => exit_choosing_delegate(exit_name));
    }
  }

  public void DisableExitChoosingButton(string exit) {
    for (int i = 0; i < exit_buttons.Count; i++) {
      if (exit_buttons[i].GetComponentInChildren<TextMeshProUGUI>().text == exit) {
        exit_buttons[i].GetComponent<Button>().onClick.RemoveAllListeners();
        exit_buttons[i].SetActive(false);
        return;
      }
    }
  }

  public void DeactivateExitChoosingButtons() {
    foreach (GameObject button in exit_buttons) {
      button.GetComponent<Button>().onClick.RemoveAllListeners();
      button.SetActive(false);
    }
  }

  public void DeactivateApproachChoosingButtons() {
    foreach (GameObject button in approach_buttons) {
      button.GetComponent<Button>().onClick.RemoveAllListeners();
      button.SetActive(false);
    }
  }

  public void DeactivateRunwayChoosingButtons() {
    foreach (GameObject button in runway_buttons) {
      button.GetComponent<Button>().onClick.RemoveAllListeners();
      button.SetActive(false);
    }
  }

  public void PrintStateName(string state_name) {
    transform.Find("Panel/Text_2_1").gameObject.GetComponent<TextMeshProUGUI>().text = state_name;
  }

  public void PrintTailName(string tail_name) {
    transform.Find("Panel/Text_1_1").gameObject.GetComponent<TextMeshProUGUI>().text = tail_name;
  }

  public void PrintAltitudeHeadingSpeed(float altitude, float heading, float speed) {
    string t = "A " + altitude.ToString("00000") + " H " + heading.ToString("000") + " S " + speed.ToString("000");
    transform.Find("Panel/Text_1_2").gameObject.GetComponent<TextMeshProUGUI>().text = t;
  }

  public void PrintStateInfo(string info) {
    transform.Find("Panel/Text_2_2").gameObject.GetComponent<TextMeshProUGUI>().text = info;
  }

  public void DeactivateAllDepartureButtons() {
    List<GameObject> button_list = new List<GameObject>();
    button_list.Add(transform.Find("Panel/Runway1").gameObject);
    button_list.Add(transform.Find("Panel/Runway2").gameObject);
    button_list.Add(transform.Find("Panel/Runway3").gameObject);
    button_list.Add(transform.Find("Panel/Runway4").gameObject);
    button_list.Add(transform.Find("Panel/TaxiwayAutomaticallyChosenButton").gameObject);
    button_list.Add(transform.Find("Panel/HoldOnCommandButton").gameObject);
    button_list.Add(transform.Find("Panel/AlignButton").gameObject);
    button_list.Add(transform.Find("Panel/TakeoffButton").gameObject);
    button_list.Add(transform.Find("Panel/PushbackApproveButton").gameObject);

    foreach (GameObject button in button_list) {
      button.GetComponent<Button>().onClick.RemoveAllListeners();
      button.SetActive(false);
    }
  }

  public void SetCurrentIndicator(bool value) {
    Vector4 color = current_indicator_image.GetComponent<Image>().color;
    color.w = value ? 1f : 0f; // current? 1(solid) : 0(transparent)
    current_indicator_image.GetComponent<Image>().color = color;
    if (!value) {
      is_current_ = false;
    }
  }

  // Start is called before the first frame update
  void Start() {
    camera_ = GameObject.Find("MainCamera").GetComponent<CameraManager>();

    runway_buttons.Add(transform.Find("Panel/Runway1").gameObject);
    runway_buttons.Add(transform.Find("Panel/Runway2").gameObject);
    runway_buttons.Add(transform.Find("Panel/Runway3").gameObject);
    runway_buttons.Add(transform.Find("Panel/Runway4").gameObject);

    approach_buttons.Add(transform.Find("Panel/Approach1").gameObject);
    approach_buttons.Add(transform.Find("Panel/Approach2").gameObject);
    approach_buttons.Add(transform.Find("Panel/Approach3").gameObject);
    approach_buttons.Add(transform.Find("Panel/Approach4").gameObject);

    exit_buttons.Add(transform.Find("Panel/Exit1").gameObject);
    exit_buttons.Add(transform.Find("Panel/Exit2").gameObject);
    exit_buttons.Add(transform.Find("Panel/Exit3").gameObject);
    exit_buttons.Add(transform.Find("Panel/Exit4").gameObject);

    taxiway_automatic_button = transform.Find("Panel/TaxiwayAutomaticallyChosenButton").gameObject;

    current_indicator_image = transform.Find("Panel/CurrentIndicatorImage").gameObject;
  }

  // Update is called once per frame
  void Update() {
    
  }
}
