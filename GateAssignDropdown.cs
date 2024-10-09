using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GateAssignDropdown : MonoBehaviour, IPointerEnterHandler {

  private AirportManager airport_;

  public void SetAirportManager(AirportManager airport) {
    airport_ = airport;
  }

  // Start is called before the first frame update
  void Start()
  {
        
  }

  public void RefreshAvailableGates() {
    var dropdown = transform.GetComponent<Dropdown>();
    dropdown.options.Clear();
    dropdown.options.Add(new Dropdown.OptionData() { text = "---" });
    List<string> gates_available = airport_.GetAvailableGates();
    foreach (string gate in gates_available) {
      dropdown.options.Add(new Dropdown.OptionData() { text = gate });
    }
  }

  public void OnPointerEnter(PointerEventData pointerEventData) {
    Debug.Log("Drop clicked.");
    RefreshAvailableGates();
  }

    // Update is called once per frame
  void Update()
  {
        
  }
}
