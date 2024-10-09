using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

public struct HumanVoice {
  public HumanVoice(string voice_name_string) {
    voice_name = voice_name_string;
    System.Random r = new System.Random();
    rate = (float)(r.NextDouble() * 0.35 + 1);
    pitch = (float)(r.NextDouble() * 0.7 + 0.8);
    volume = (float)(1 - r.NextDouble() * 0.1);
  }
  public string voice_name;
  public float rate;
  public float pitch;
  public float volume;
}

public enum AircraftToGroundRequestType {
  DepartureRunway = 0,
  Pushback = 1,
}

public struct GroundToAircraftResponse {
  public AircraftToGroundRequestType type;
  public string from_whom;
  public string to_whom;
  public string what;
  public HumanVoice voice;
  public StateTransitionDelegate callback;
  public string from_state_name;
  public string to_state_name;
  public int request_id; // used to pair the request
}

public struct AircraftToGroundRequest {
  public AircraftToGroundRequestType type;
  public string to_whom;
  public string from_whom;
  public string for_what;
  public HumanVoice voice;
  public RequestSpeakCompleteCallback callback;
  public int request_id;
}
public class GroundChannelManager : MonoBehaviour {
  private bool channel_locked_ = false;
  private int request_id_ = 0;

  private HumanVoice ground_voice_;

  private Queue<AircraftToGroundRequest> aircraft_to_ground_request_queue_ = new();
  private Queue<GroundToAircraftResponse> ground_to_aircraft_response_queue_ = new();
  private Dictionary<string, AircraftToGroundRequest> speak_id_to_aircraft_to_ground_request_ = new();
  private Dictionary<string, GroundToAircraftResponse> speak_id_to_ground_to_aircraft_response_ = new();
  private Dictionary<int, AircraftToGroundRequest> request_id_to_aircraft_to_ground_request_ = new(); // store request for generate response
  private Dictionary<int, GroundToAircraftResponse> request_id_to_ground_to_aircraft_response_ = new();

  public int GetRequestId() {
    request_id_++;
    return request_id_ - 1;
  }

  private string ConstructResponseWhat(AircraftToGroundRequestType type, string response_info) {
    if (type == AircraftToGroundRequestType.DepartureRunway) {
      return "departure using runway " + response_info;
    }
    if (type == AircraftToGroundRequestType.Pushback) {
      return "clear to pushback runway " + response_info;
    }
    return "Construct response text going wrong.";
  }

  public void GenerateResponse(int request_id,
                               string response_info,
                               StateTransitionDelegate state_transition,
                               string from_state_name,
                               string to_state_name) {
    AircraftToGroundRequest request;
    if (request_id_to_aircraft_to_ground_request_.ContainsKey(request_id)) {
      request = request_id_to_aircraft_to_ground_request_[request_id];

      GroundToAircraftResponse response = new();
      response.type = request.type;
      response.from_whom = request.to_whom;
      response.to_whom = request.from_whom;
      response.what = ConstructResponseWhat(request.type, response_info);
      response.voice = ground_voice_;
      response.callback = state_transition;
      response.from_state_name = from_state_name;
      response.to_state_name = to_state_name;
      response.request_id = request.request_id;

      ground_to_aircraft_response_queue_.Enqueue(response);
      request_id_to_ground_to_aircraft_response_.Add(request.request_id, response);
    }
  }

  public void Request(AircraftToGroundRequest request) {
    Debug.Log("Ground request called." + request.from_whom + "id:" + request.request_id);
    aircraft_to_ground_request_queue_.Enqueue(request);
    request_id_to_aircraft_to_ground_request_.Add(request.request_id, request);
  }

  public void SpeakComplete(Crosstales.RTVoice.Model.Wrapper wrapper) {
    string speak_id = wrapper.Uid;
    if (speak_id_to_aircraft_to_ground_request_.ContainsKey(speak_id)) {
      speak_id_to_aircraft_to_ground_request_[speak_id].callback.Invoke();
      speak_id_to_aircraft_to_ground_request_.Remove(speak_id);
    }
    if (speak_id_to_ground_to_aircraft_response_.ContainsKey(speak_id)) {
      var response = speak_id_to_ground_to_aircraft_response_[speak_id];
      speak_id_to_ground_to_aircraft_response_[speak_id].callback.Invoke(response.from_state_name, response.to_state_name);
      speak_id_to_ground_to_aircraft_response_.Remove(speak_id);
    }
    channel_locked_ = false;
  }

  private void SpeakRequest(AircraftToGroundRequest request) {
    Debug.Log("Speak request called.");
    channel_locked_ = true;
    speak_id_to_aircraft_to_ground_request_.Add(
      Speaker.Speak(ConstructRequestText(request),
                    /*audio source*/null,
                    Speaker.VoiceForName(request.voice.voice_name),
                    /*immediately*/true,
                    request.voice.rate,
                    request.voice.pitch,
                    request.voice.volume),
      request);
  }

  private void SpeakResponse(GroundToAircraftResponse response) {
    Debug.Log("Speak response called.");
    channel_locked_ = true;
    speak_id_to_ground_to_aircraft_response_.Add(
      Speaker.Speak(ConstructResponseText(response),
                    /*audio source*/null,
                    //Speaker.VoiceForName(response.voice.voice_name),
                    Speaker.VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender.FEMALE, "en"),
                    /*immediately*/true,
                    response.voice.rate,
                    response.voice.pitch,
                    response.voice.volume),
      response); ;
  }

  // Start is called before the first frame update
  void Start() {
    Speaker.OnSpeakComplete += this.SpeakComplete;
    ground_voice_ = new HumanVoice("Microsoft Zira Desktop (en-US, FEMALE)");
  }

  // Update is called once per frame
  void Update() {
    if (!channel_locked_) {
      int request_queue_id = -1, response_queue_id = -1;
      if (aircraft_to_ground_request_queue_.Count > 0) {
        request_queue_id = aircraft_to_ground_request_queue_.Peek().request_id;
      }
      if (ground_to_aircraft_response_queue_.Count > 0) {
        response_queue_id = ground_to_aircraft_response_queue_.Peek().request_id;
      }
      if (request_queue_id >= 0) {
        if (response_queue_id >= 0 && response_queue_id < request_queue_id) {
          SpeakResponse(ground_to_aircraft_response_queue_.Dequeue());
          return;
        } else {
          SpeakRequest(aircraft_to_ground_request_queue_.Dequeue());
          return;
        }
      }
      if (response_queue_id >= 0) {
        // Here request queue id must be < 0. Hence no need to consider request.
        SpeakResponse(ground_to_aircraft_response_queue_.Dequeue());
      }
    }
  }

  private string ConstructRequestText(AircraftToGroundRequest request) {
    string text = "";
    text += request.to_whom;
    text += ", ";
    text += NatoCallName(request.from_whom);
    text += ", ";
    text += request.for_what;
    text += ".";
    return text;
  }

  private string ConstructResponseText(GroundToAircraftResponse response) {
    string text = "";
    text += NatoCallName(response.to_whom);
    text += ", ";
    text += response.from_whom;
    text += ", ";
    text += response.what;
    text += ".";
    return text;
  }

  private string NatoCallName(string input) {
    Dictionary<char, string> nato_dictionary = new Dictionary<char, string>() {
      { 'A', "Alpha" },
      { 'B', "Bravo" },
      { 'C', "Charlie" },
      { 'D', "Delta" },
      { 'E', "Echo" },
      { 'F', "Foxtrot" },
      { 'G', "Golf" },
      { 'H', "Hotel" },
      { 'I', "India" },
      { 'J', "Juliett" },
      { 'K', "Kilo" },
      { 'L', "Lima" },
      { 'M', "Mike" },
      { 'N', "November" },
      { 'O', "Oscar" },
      { 'P', "Papa" },
      { 'Q', "Quebec" },
      { 'R', "Romeo" },
      { 'S', "Sierra" },
      { 'T', "Tango" },
      { 'U', "Uniform" },
      { 'V', "Victor" },
      { 'W', "Whiskey" },
      { 'X', "Xray" },
      { 'Y', "Yankee" },
      { 'Z', "Zulu" },
      { '0', "Zero"},
      { '1', "One"},
      { '2', "Two"},
      { '3', "Three"},
      { '4', "Four"},
      { '5', "Five"},
      { '6', "Six"},
      { '7', "Seven"},
      { '8', "Eight"},
      { '9', "Niner"},
    };
    string output = "";
    for (int i = 0; i < input.Length; i++) {
      char c = input[i];
      if (nato_dictionary.ContainsKey(c)) {
        output += nato_dictionary[c];
        if (i != input.Length - 1) {
          output += " ";
        }
      }
    }
    return output;
  }
}
