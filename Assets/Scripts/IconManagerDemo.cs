using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IconManagerDemo : MonoBehaviour
{
    #region //VARIABLES REGION

    // This stores the location and the labels per session as one structure.
    public struct Target
    {
        public string name;
        public GameObject tPos;

        public Target(string label, GameObject go)
        {
            name = label;
            tPos = go;
        }
    }

    List<Target> m_targets = new List<Target>();
    Vector3 m_sourcePos, m_targetPos = Vector3.zero;
    Vector3 m_captionOffset = new Vector3(0, 0.15f, 0); // This puts the label above the icon.
    Quaternion m_sourceRot, m_targetRot = Quaternion.identity;
    GameObject m_sourceObj, m_targetObj;

    List<GameObject> m_pointObjs = new List<GameObject>();
    string m_targetName = "";

    enum IconState { Start, Guess, Show, Moving, Correct, Wrong, End };
    IconState m_iconState = IconState.Start;
    GameObject m_movingObj, m_endObj, m_correctObj, m_wrongObj, m_captionObj; // Set active according to the icon state

    MeshCollider m_frontWall, m_leftWall, m_rightWall;

    string m_label = " ";

    float m_moveSpeed = 30f;
    float m_fracJourney = 0;
    float m_yEulerAngle = 0.0f;
    float m_accumDistance = 0;
    float m_totalAngle = 0;

    string m_answer = "";
    GameObject m_collidingObj;

    ExperimentWriter m_expWriter;

    enum Session { Trial, Training1, Training2, Short };
    Session m_currentSession = Session.Trial;

    List<string> m_labels = new List<string>();

    static int MAX_TRIALS = 1;
    static int MAX_SHORT_TARGETS = 12;
    static int MAX_SESSIONS = 4;

    int m_targetCount = 1;
    int m_sessionCount = 1;

    string m_currentTag = "None";

    enum LabelLanguage { English, Japanese, None };
    LabelLanguage m_currentLang = LabelLanguage.English;

    enum Mode { AR, VR, None };
    Mode m_currentMode = Mode.VR;

    bool m_initialized = false;

    float m_time, m_inputTime, m_endTime, m_showTime, m_moveTime, m_guessTime = 0;
    static float MAX_END_TIME = 5.0f;
    static float MAX_SHOW_TIME = 5.0f;

    #endregion


    #region //STATES, MONOBEHAVIOR
    void Start()
    {
        //InputManager.Instance.AddGlobalListener(gameObject); // for OnInputDown to work
        m_expWriter = transform.GetComponent<ExperimentWriter>(); // to connect to the CSV generator

    }

    // Check first if all settings have been configured before starting the training session
    bool IsInitialized()
    {
        InitObjects();
        if (m_currentMode == Mode.None || m_currentLang == LabelLanguage.None || m_currentTag == "None")
        {
            return false;
        }
        else
        {
            if (!m_initialized)
            {
                InitObjects();
                LoadLanguage(m_currentLang); //must be adjustable upon setup
                PairLabelTargets();
                ChangeMode(m_currentMode);
                m_initialized = true;
            }
            return true;
        }
    }

    //Turns off the mesh renderer for the walls so that users can see the actual walls in the real environment
   void ChangeMode (Mode currentMode)
    {
        m_frontWall.transform.GetComponent<MeshRenderer>().enabled = currentMode != Mode.AR;
        m_leftWall.transform.GetComponent<MeshRenderer>().enabled = currentMode != Mode.AR;
        m_rightWall.transform.GetComponent<MeshRenderer>().enabled = currentMode != Mode.AR;
    }

    //Tags are put on room component elements (knobs, buttons) so that they can be identified and included in the list of possible locations
    void SetTag(string tag)
    {
        m_currentTag = tag;
    }

    // Update is called once per frame
    void Update() {

        m_time += Time.deltaTime;
        
        if (m_iconState == IconState.End)
        {
            if (m_endTime >= MAX_END_TIME)
            {
                SaveState("highlight", m_endTime);
                m_endTime = 0;
                ResetIconPosition();
                UpdateSession();

                m_fracJourney = 0;
                m_accumDistance = 0;
                SaveSession();
                ChangeIconState(IconState.Start);
            }
            else
            {
                m_endTime += Time.deltaTime;
            }
        }
        if (m_iconState == IconState.Show)
        {
            if (m_showTime >= MAX_SHOW_TIME)
            {
                SaveState("stimulus", m_showTime);
                m_showTime = 0;
                ChangeIconState(IconState.Moving);
            }
            else
            {
                m_showTime += Time.deltaTime;
            }
        }
        if (m_iconState == IconState.Guess)
        {
            m_guessTime += Time.deltaTime;
        }

        if (!m_initialized)
        {
            IsInitialized();
            return;
        }

        //From moving until arriving
        if (m_iconState == IconState.Moving)
        {
            m_moveTime += Time.deltaTime;
            if (m_fracJourney < 1.0f)
            {
                UpdateIconMovement();
            }
            else if (m_fracJourney >= 1.0f)
            {
                SaveState("movement", m_moveTime); m_moveTime = 0;
                m_endObj.transform.position = m_movingObj.transform.position;
                m_endObj.transform.rotation = m_movingObj.transform.rotation;
                ChangeIconState(IconState.End);
            }
        }

        // For Unity editor purposes only / needs Spatial Mapping and Anchor Scripts turned off
        if (Input.GetKeyUp(KeyCode.Space)) 
        {
            if (m_currentSession == Session.Short)
            {
                m_frontWall.enabled = false;
                m_leftWall.enabled = false;
                m_rightWall.enabled = false;
                UpdateNextShortTestItem();
            }
            else
            {
                m_frontWall.enabled = true;
                m_leftWall.enabled = true;
                m_rightWall.enabled = true;
                UpdateNextTrainingSequence();
            }
        }
    }


    /*public void OnInputUp(InputEventData eventData)
    {
        
    }*/

    // Upon AirTap / Clicker
    /* public void OnInputDown(InputEventData eventData)
     {
         if (m_currentMode == Mode.AR) { ChangeMode(Mode.VR); }
         else { ChangeMode(Mode.AR); }

         Debug.Log(m_currentMode);

         /* m_inputTime = m_time;
          if (m_iconState == IconState.Show)
          {
              SaveState("stimulus", m_showTime); m_showTime = 0;
          }
          if (m_iconState == IconState.End)
          {
              SaveState("highlight", m_endTime); m_endTime = 0;
              m_endTime = 0;
              ResetIconPosition();
              UpdateSession();

              m_fracJourney = 0;
              m_accumDistance = 0;
              SaveSession();
              ChangeIconState(IconState.Start);
          }

          // configures the language, mode, and set
          RaycastHit rayCast;
          if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out rayCast, Mathf.Infinity, LayerMask.GetMask("Choices UI")))
          {
              GameObject choice = rayCast.collider.gameObject;

              if (choice.transform.parent.name == "Language")
              {
                  if (choice.name.Equals("JP"))
                  { m_currentLang = LabelLanguage.Japanese; Destroy(choice.transform.parent.Find("EN").gameObject); }
                  else if (choice.name.Equals("EN"))
                  { m_currentLang = LabelLanguage.English; Destroy(choice.transform.parent.Find("JP").gameObject); }

              }
              else if (choice.transform.parent.name == "Mode")
              {
                  if (choice.name.Equals("AR"))
                  { m_currentMode = Mode.AR; Destroy(choice.transform.parent.Find("VR").gameObject); }
                  else if (choice.name.Equals("VR"))
                  { m_currentMode = Mode.VR; Destroy(choice.transform.parent.Find("AR").gameObject); }

              }
              else if (choice.transform.parent.name == "Set")
              {
                  SetTag("Set" + choice.name);
                  if (choice.name.Equals("A"))
                  { Destroy(choice.transform.parent.Find("B").gameObject); }
                  else if (choice.name.Equals("B"))
                  { Destroy(choice.transform.parent.Find("A").gameObject); }
              }
              Destroy(choice);
          }

          if (m_frontWall != null && m_leftWall != null && m_rightWall != null)
          { 
              if (m_currentSession == Session.Short)
              {
                  m_frontWall.enabled = false;
                  m_leftWall.enabled = false;
                  m_rightWall.enabled = false;
                  UpdateNextShortTestItem();
              }
              else
              {
                  m_frontWall.enabled = true;
                  m_leftWall.enabled = true;
                  m_rightWall.enabled = true;
                  UpdateNextTrainingSequence();
              }
          }

     } */

    void UpdateNextShortTestItem()
    {
        if (m_iconState == IconState.Start)
        {
            ChangeTarget();
            ChangeIconState(IconState.Guess);
            return;
        }
        else if (m_iconState == IconState.Guess)
        {
            SaveState("guess", m_guessTime); m_guessTime = 0; // Moved outside of raycast to show total guessing time in CSV file. Maybe they did not align the cursor towards the game object enough, etc.
            RaycastHit rayCast;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out rayCast, Mathf.Infinity, LayerMask.GetMask("Test Raycast")))
            {

                m_collidingObj = rayCast.collider.gameObject;

                if (m_targetObj.name == m_collidingObj.name && m_targetObj.transform.parent == m_collidingObj.transform.parent)
                {
                    ChangeIconState(IconState.Correct);
                    m_correctObj.transform.position = m_collidingObj.transform.position;
                    m_correctObj.transform.rotation = Quaternion.LookRotation(Vector3.up, rayCast.normal);
                    m_answer = "Correct";
                }
                else
                {
                    ChangeIconState(IconState.Wrong);
                    m_wrongObj.transform.position = m_collidingObj.transform.position;
                    m_wrongObj.transform.rotation = Quaternion.LookRotation(Vector3.up, rayCast.normal);
                    m_answer = "Wrong";
                }

            }
            return;
        }
        else if (m_iconState == IconState.Correct || m_iconState == IconState.Wrong)
        {
            UpdateSession();
            ChangeIconState(IconState.Start);
            return;
        }

    }

    void UpdateNextTrainingSequence()
    {
        if (m_iconState == IconState.Start)
        {
            ChangeTarget();

            m_sourcePos = m_sourceObj.transform.position;
            m_targetPos = m_targetObj.transform.position;

            m_sourceRot = Quaternion.LookRotation(m_sourcePos - transform.position);
            m_targetRot = Quaternion.LookRotation(m_targetPos - transform.position);
            m_totalAngle = Quaternion.Angle(m_sourceRot, m_targetRot);

            ChangeIconState(IconState.Show);
            return;
        }
        else if (m_iconState == IconState.Show)
        {
            ChangeIconState(IconState.Moving);
            return;
        }
        else if (m_iconState == IconState.End)
        {
            ResetIconPosition();
            UpdateSession();

            m_fracJourney = 0;
            m_accumDistance = 0;
            ChangeIconState(IconState.Start);
            return;
        }
    }

    #endregion

    #region //INITIALIZERS

    void ResetIconPosition()
    {
        if (m_sourceObj != null)
        {
            // MOVE THE ICON BACK TO THE SOURCE AREA
            m_sourcePos = m_sourceObj.transform.position;
        
            m_movingObj.transform.position = m_sourceObj.transform.position;
            m_movingObj.transform.rotation = m_sourceObj.transform.rotation;

            m_endObj.transform.position = m_sourceObj.transform.position;
            m_endObj.transform.rotation = m_sourceObj.transform.rotation;

            m_captionObj.transform.position = m_sourceObj.transform.position + m_captionOffset;
            m_captionObj.transform.rotation = m_sourceObj.transform.rotation;
            
        }
    }

    void InitObjects()
    {
        m_movingObj = transform.Find("Icon/moving").gameObject;
        m_endObj = transform.Find("Icon/end").gameObject;
        m_correctObj = transform.Find("Icon/correct").gameObject;
        m_wrongObj = transform.Find("Icon/wrong").gameObject;
        m_captionObj = transform.Find("Icon/caption").gameObject;
        m_sourceObj = transform.parent.Find("Source").gameObject;
        //ResetIconPosition();
  
        m_frontWall = GameObject.Find("FrontWall").GetComponent<MeshCollider>();
        m_leftWall =  GameObject.Find("LeftWall").GetComponent<MeshCollider>();
        m_rightWall = GameObject.Find("RightWall").GetComponent<MeshCollider>();

        //Load all the game object positions with the tag "Set A" or "Set B" in the list of possible positions
        foreach (Transform child in GameObject.Find("Front").transform)
        {
            if (child.gameObject.tag == m_currentTag)
            {
                m_pointObjs.Add(child.gameObject);
            }
        }

        foreach (Transform child in GameObject.Find("Left").transform)
        {
            if (child.gameObject.tag == m_currentTag)
            {
                m_pointObjs.Add(child.gameObject);
            }
        }

        foreach (Transform child in GameObject.Find("Right").transform)
        {
            if (child.gameObject.tag == m_currentTag)
            {
                m_pointObjs.Add(child.gameObject);
            }
        }

        m_pointObjs.Shuffle();
    }
    #endregion

    #region // ICON CHANGES

    void ChangeIconState(IconState state)
    {
        switch (state)
        {
            //MEMORIZATION + TEST MODE
            case IconState.Start: ToggleVisibility(state, false); break;
            case IconState.Show: ToggleVisibility(state, true); break;
            case IconState.Moving: ToggleVisibility(state, true); break;
            case IconState.End: ToggleVisibility(state, true); break;

            //TEST MODE
            case IconState.Guess: ToggleVisibility(state, true); break;
            case IconState.Correct: ToggleVisibility(state, false); break;
            case IconState.Wrong: ToggleVisibility(state, false); break;

            default: break;
        }

        m_iconState = state;
    }

    void ToggleVisibility(IconState state, bool isCaptionShown)
    {
        if (m_captionObj != null) { m_captionObj.SetActive(isCaptionShown); }

        m_movingObj.SetActive(state == IconState.Moving);
        m_endObj.SetActive(state == IconState.End || state == IconState.Start || state == IconState.Show);
        m_correctObj.SetActive(state == IconState.Correct);
        m_wrongObj.SetActive(state == IconState.Wrong);
    }


    void UpdateIconMovement()
    {
        
        //STEP 1: CHECK IF ON SCREEN. IF NOT, STOP MOVEMENT
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(m_captionObj.transform.position);
        bool onScreen = screenPoint.z > 0.1 && screenPoint.x > 0.1 && screenPoint.x < 0.9 && screenPoint.y > 0.1 && screenPoint.y < 0.9;

        if (m_movingObj != null && onScreen)
        {
            m_sourcePos = m_sourceObj.transform.position;
            m_targetPos = m_targetObj.transform.position;

            m_sourceRot = Quaternion.LookRotation(m_sourcePos - transform.position);
            m_targetRot = Quaternion.LookRotation(m_targetPos - transform.position);
            m_totalAngle = Quaternion.Angle(m_sourceRot, m_targetRot);

            m_accumDistance += m_moveSpeed * Time.deltaTime;
            m_fracJourney = m_accumDistance/m_totalAngle;

            transform.rotation = Quaternion.Lerp(m_sourceRot, m_targetRot, m_fracJourney);

            //STEP 2: MOVE THE ICONS AND CAPTION AGAINST THE SURFACE OF THE WALL
            RaycastHit rayCast;
            if (Physics.Raycast(transform.position, transform.forward, out rayCast, Mathf.Infinity, LayerMask.GetMask("Wall Raycast")))
            {
                m_movingObj.transform.position = rayCast.point;
                m_movingObj.transform.rotation = Quaternion.LookRotation(Vector3.up, rayCast.normal);

                m_endObj.transform.position = rayCast.point;
                m_endObj.transform.rotation = Quaternion.LookRotation(Vector3.up, rayCast.normal);

                m_captionObj.transform.position = new Vector3(rayCast.point.x, rayCast.point.y + 0.25f, rayCast.point.z);
                m_captionObj.transform.rotation = Quaternion.LookRotation(-Vector3.up, rayCast.normal);
            }

            //STEP 3: ROTATE THE COMPASS
            float yEulerAngle = Mathf.Rad2Deg * Mathf.Atan2(m_sourcePos.y - m_targetPos.y, m_sourcePos.x - m_targetPos.x);
            Transform compass = m_movingObj.transform.Find("default");
            compass.localEulerAngles = new Vector3(0, -yEulerAngle, 0);
        }

    }

    void ChangeTarget()
    {
        if (m_targets.Count > 0)
        {
            Target t = m_targets[m_targetCount-1];
            m_label = t.name;
            m_targetObj = t.tPos;
            m_targetPos = m_targetObj.transform.position;
            m_targetName = m_targetObj.name;

            if (m_captionObj != null)
            {
                TextMesh labelMesh = m_captionObj.GetComponentInChildren<TextMesh>();
                if (labelMesh != null)
                {
                    labelMesh.text = m_label;
                }
            }

            if (m_currentSession == Session.Trial)
            {
                m_targets.Remove(t);
            }
        }
        else
        {
            Debug.Log("Target positions missing. No target found.");
        }
    }

    #endregion

    #region //SESSIONS

    void LoadLanguage(LabelLanguage lang)
    {
        if (m_labels == null)
        {
            return; 
        }

        if (m_labels.Count > 0)
        { 
            m_labels.Clear();
        }

        string extension = "";

        switch (lang)
        {
            case LabelLanguage.English: extension = "en"; break;
            case LabelLanguage.Japanese: extension = "jp"; break;
            default: extension = ""; break;
        }

        TextAsset labels = Resources.Load("complete_" + extension) as TextAsset;

        if (labels != null)
        {
            string[] labelArray = labels.text.Split('\n');

            foreach (string l in labelArray)
            { m_labels.Add(l); }
        }

        m_labels.Shuffle();
    }

    void PairLabelTargets()
    {
        int i = 0;
        while (i < MAX_TRIALS + MAX_SHORT_TARGETS)
        {
            m_targets.Add(new Target(m_labels[i], m_pointObjs[i]));
            SaveLabels(m_labels[i], m_pointObjs[i].transform.parent.name, m_pointObjs[i].name);
            i++;            
        }
    }

    /** TRIAL >> TRAINING 1 >> TRAINING 2 >> SHORT TERM >> TRAINING 1 >>... **/

    void UpdateSession()
    {
        int maxLimit = (m_currentSession == Session.Trial) ? MAX_TRIALS : MAX_SHORT_TARGETS;
        SaveSession();

        if (m_targetCount < maxLimit)
        {
            m_targetCount++;
        }
        else
        {
            m_targetCount = 1;
            m_targets.Shuffle();

            switch (m_currentSession)
            {
                case Session.Trial: m_currentSession = Session.Training1; return;
                case Session.Training1: m_currentSession = Session.Training2; return;
                case Session.Training2: m_currentSession = Session.Short; return;
                case Session.Short:
                    {
                        if (m_sessionCount < MAX_SESSIONS) { m_sessionCount++; }
                        else
                        {
                            transform.gameObject.SetActive(false);
                            Debug.Log("IT'S OVER!!!");
                        }


                        m_currentSession = Session.Training1;
                        return;
                    }
                default: return;

            }
        }

    }

       /** Methods in this section write the data to a CSV file as immediately as possible. **/

    void SaveSession()
    {
        if (m_expWriter == null)
        {
            return;
        }


        if (m_currentSession != Session.Short)
        {
            m_expWriter.SaveExperimentData(m_currentSession.ToString() + "," + m_sessionCount + "," + m_targetCount + "," + m_label + "," + m_targetObj.transform.parent.name + " " + m_targetName);
        }
        else if (m_collidingObj != null)
        {
            m_expWriter.SaveExperimentData(m_currentSession.ToString() + "," + m_sessionCount + "," + m_targetCount + "," + m_label + "," + m_targetName + "," + m_answer + "," + m_collidingObj.transform.parent.name + " " + m_collidingObj.name);
        }
    }

    void SaveState(string desc, float savedTime)
    {
        m_expWriter.SaveExperimentData(desc + "," + savedTime);
    }

    void SaveLabels(string label, string wall,  string gamePosition)
    {
        m_expWriter.SaveExperimentData(label + "," + wall + "," + gamePosition);
    }

    #endregion

}
