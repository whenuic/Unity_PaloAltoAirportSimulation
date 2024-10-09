using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PapiManager : MonoBehaviour {
  [SerializeField] GameObject flights_manager;
  [SerializeField] GameObject l1;
  [SerializeField] GameObject l2;
  [SerializeField] GameObject l3;
  [SerializeField] GameObject l4;

  private static Color red = new Color(1.0f, 0.0f, 0.0f, 1.0f);
  private static Color white = new Color(1.0f, 1.0f, 1.0f, 1.0f);

  private GameObject current_aircraft;

  // Start is called before the first frame update
  void Start() {

  }

  // Update is called once per frame
  void Update() {
    current_aircraft = flights_manager.GetComponent<FlightsManager>().CurrentAircraft();
    if (current_aircraft == null) {
      return;
    }
    float distance = Vector3.Distance(current_aircraft.transform.position, transform.position);
    float altitude = current_aircraft.GetComponent<FlightControl>().GetAltitude() * 0.3048f; // Flight return ft, here covert to meter

    float degree = Mathf.Tan(altitude / distance) * Mathf.Rad2Deg;

    float scale = Mathf.Min(4.0f, Mathf.Max(distance * 15.0f / 8000.0f, 2.0f));
    l1.transform.localScale = new Vector3(scale, 0.01f, scale);
    l2.transform.localScale = new Vector3(scale, 0.01f, scale);
    l3.transform.localScale = new Vector3(scale, 0.01f, scale);
    l4.transform.localScale = new Vector3(scale, 0.01f, scale);

    if (degree <= 2.5f) {
      l1.GetComponent<Renderer>().material.SetColor("_Color", red);
      l2.GetComponent<Renderer>().material.SetColor("_Color", red);
      l3.GetComponent<Renderer>().material.SetColor("_Color", red);
      l4.GetComponent<Renderer>().material.SetColor("_Color", red);
    } else if (degree <= 2.833f) {
      l1.GetComponent<Renderer>().material.SetColor("_Color", white);
      l2.GetComponent<Renderer>().material.SetColor("_Color", red);
      l3.GetComponent<Renderer>().material.SetColor("_Color", red);
      l4.GetComponent<Renderer>().material.SetColor("_Color", red);
    } else if (degree <= 3.166f) {
      l1.GetComponent<Renderer>().material.SetColor("_Color", white);
      l2.GetComponent<Renderer>().material.SetColor("_Color", white);
      l3.GetComponent<Renderer>().material.SetColor("_Color", red);
      l4.GetComponent<Renderer>().material.SetColor("_Color", red);
    } else if (degree <= 3.5f) {
      l1.GetComponent<Renderer>().material.SetColor("_Color", white);
      l2.GetComponent<Renderer>().material.SetColor("_Color", white);
      l3.GetComponent<Renderer>().material.SetColor("_Color", white);
      l4.GetComponent<Renderer>().material.SetColor("_Color", red);
    } else {
      l1.GetComponent<Renderer>().material.SetColor("_Color", white);
      l2.GetComponent<Renderer>().material.SetColor("_Color", white);
      l3.GetComponent<Renderer>().material.SetColor("_Color", white);
      l4.GetComponent<Renderer>().material.SetColor("_Color", white);
    }

  }
}
