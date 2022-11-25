using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using EmotivUnityPlugin;

/* Handles the logic of actively training a profile, giving live feedback after the first few rounds.
 * The process of which is as follows:
 * 
 * each round of training is comprised of 2 stages, a neutral relaxed state, and an active input state,
 * each stage consists of a short countdown followed by an eight second period of training,
 * after which the system will query cortex for the quality of the training (if enough rounds have been finished)
 * and display this quality to the user, informing a choice to accept or deny the training.
 * if the player accepts, move on, if not, redo the current stage
 * 
 * If not enough rounds have been completed to warrant the display of training quality,
 * and acceptance of the stage, a simple continue button will be shown, which advances to the next stage.
 */
public class TrainingSubmenu : MonoBehaviour, IRequiresInit
{
    enum TrainingState { NEUTRAL, BRUSHING, VALIDATION }
    TrainingState trainingState = TrainingState.NEUTRAL;

    string action => trainingState == TrainingState.BRUSHING ? "push" : "neutral";


    [Header("Settings")]
    public BCITrainingSettings bciPrefs;
    [Header("References")]
    public GameObject trainingCompletionOptions;
    public GameObject earlyCompletionOption;
    public GameObject completionButton;
    public GameObject backButton;
    public GameObject backOptions;
    //EKL Edit
    public GameObject repeatTraining;

    //public TextMeshProUGUI trainingQualityText;
    [Header("Text references")]
    public TextMeshProUGUI countDownText;
    public TextMeshProUGUI upNextText;
    public TextMeshProUGUI commandText;
    public TextMeshProUGUI failureText;
    public Animator feedbackAnim;

    [Header("Misc")]
    public ProgressBar trainingProgressBar;
    public ProgressBar progressBar;

    public Action OnTrainingComplete;

    TrainingGradeDisplay gradeDisplay;

    [HideInInspector]
    public string headsetID;
    [HideInInspector]
    public int roundsTrained;

    int minRounds = 11;
    int maxRounds = 16;
    int assistRounds = 6;
    int feedbackThreshold = 4;
    int countdownTime = 4;
    int minAcceptGrade = 70;
    int numCommands = 1; //Only including this so that we can expand to more than one command for different actions down the line.

    string profileName;
    float timer = 0;
    int trainingRounds = 0;
    //EKL Edit
    int neutralCounter = 0;
    int totalNeutral = 0;
    int command1Counter = 0;
    int[] totalCommand = new int[3];
    //Won't need these until later perhaps.
    //int command2Counter = 0; 
    //int command3Counter = 0;
    //int command4Counter = 0;

    bool feedbackEnabled = false;
    bool completionEnabled = false;
    bool trainingCountdown = false;
    bool returning = false;

    float assistance
    {
        get
        {
            if (trainingRounds < feedbackThreshold)
                return 1;
            else if (trainingRounds < feedbackThreshold + assistRounds)
                return 1 - ((trainingRounds - feedbackThreshold) / (float)assistRounds);
            return 0;
        }
    }

    public void Init()
    {
        trainingCompletionOptions.SetActive(false);
        earlyCompletionOption.SetActive(false);
        //EKL edit
        repeatTraining.SetActive(false);

        gradeDisplay = GetComponentInChildren<TrainingGradeDisplay>(true);

        trainingProgressBar.Init();
        progressBar.Init();
        completionButton.SetActive(false);
        backOptions.SetActive(false);
        commandText.text = "";

        //EKL Edit - Grab the values from the BCIPrefs scriptable object
        minRounds = bciPrefs.minimumRounds;
        maxRounds = bciPrefs.maximumRounds;
        assistRounds = bciPrefs.assistRounds;
        feedbackThreshold = bciPrefs.feedbackThreshold;
        countdownTime = bciPrefs.countdownTime;
        minAcceptGrade = bciPrefs.minimumAcceptGrade;

        gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        Cortex.training.TrainingThresholdResult += OnTrainingThresholdResult;
        Cortex.SubscribeMentalCommands(headsetID, OnMentalCommandRecieved);
        Cortex.SubscribeSysEvents(headsetID, OnSysEventReceived);
        Cortex.training.ProfileLoaded += OnProfileLoaded;

        // disables background scrolling with cursor
        CursorOffset.active = false;
        failureText.enabled = false;
    }
    public void OnDisable()
    {
        Cortex.training.TrainingThresholdResult -= OnTrainingThresholdResult;
        Cortex.UnsubscribeMentalCommands(headsetID, OnMentalCommandRecieved);
        Cortex.UnsubscribeSysEvents(headsetID, OnSysEventReceived);
        Cortex.training.ProfileLoaded -= OnProfileLoaded;

        CursorOffset.active = true;

        timer = 0;
        trainingCountdown = false;
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            timer = 0;
            if (trainingCountdown) // start training
            {
                Cortex.training.StartTraining(action);
                trainingCountdown = false;
            }
        }
        countDownText.text = trainingCountdown ? $"{(int)timer + 1}" : "";
        trainingProgressBar.SetProgress((8 - timer) / 8f);
    }

    void OnProfileLoaded(string profile) => profileName = profile;

    void OnMentalCommandRecieved(MentalCommand command)
    {
        if (trainingState == TrainingState.BRUSHING)
        {
            if (command.action == "neutral")
                feedbackAnim.SetFloat("brush speed", assistance);
            else
                feedbackAnim.SetFloat("brush speed", (float)command.power + assistance);
        }
        else
            feedbackAnim.SetFloat("brush speed", 1);
    }

    // when a result is recieved after asking for threshold following training success
    void OnTrainingThresholdResult(TrainingThreshold args)
    {
        gradeDisplay.SetGrade((float)args.lastTrainingScore);
        //trainingQualityText.text = $"{(int)(args.lastTrainingScore * 100)}%";

        if (feedbackEnabled && trainingState == TrainingState.BRUSHING)
            trainingCompletionOptions.SetActive(true);
        else
            earlyCompletionOption.SetActive(true);
    }

    void OnSysEventReceived(SysEventArgs args)
    {
        switch (args.eventMessage)
        {
            case "MC_Started":
                OnTrainingStart();
                break;
            case "MC_Succeeded":
                OnTrainingSucceeded();
                break;
            case "MC_Failed":
                OnTrainingFail();
                break;
            case "MC_Completed":
                OnTrainingCompleted();
                break;
            case "MC_Rejected":
                OnTrainingRejected();
                break;
            default:
                print($"Unhandled system message: {args.eventMessage}");
                break;
        }
        backButton.SetActive(args.eventMessage != "MC_Started");
    }

    // when training has been successfully initiated
    void OnTrainingStart()
    {
        failureText.enabled = false;
        bool neutral = trainingState == TrainingState.NEUTRAL;
        commandText.text = neutral ? "relax" : "brush!";

        trainingProgressBar.Activate();
        timer = 8;

        backButton.SetActive(false);
    }

    // when training stage completes with a success
    void OnTrainingSucceeded()
    {
        Cortex.training.GetTrainingThreshold();
        countDownText.enabled = false;
        timer = Mathf.Infinity;
        commandText.text = "";
        trainingProgressBar.Deactivate();
        if (!returning)
            progressBar.Activate();
    }

    // when training stage completed with a failure
    void OnTrainingFail()
    {
        commandText.text = "";
        countDownText.enabled = false;
        failureText.enabled = true;
        timer = Mathf.Infinity;
        ActivateUpNext();
        ApplyState();
    }

    // when training was successfully rejected
    void OnTrainingRejected()
    {
        ActivateUpNext();
    }

    // when training has been started, succeeded, and accepted
    void OnTrainingCompleted()
    {
        if (numCommands <= 1) //Default - works how expected originally.
        {
            switch (trainingState)
            {
                case TrainingState.NEUTRAL:
                    trainingState = TrainingState.BRUSHING;
                    neutralCounter++;
                    progressBar.SetProgress((trainingRounds + 0.5f) / maxRounds);
                    break;
                case TrainingState.BRUSHING:
                    trainingState = TrainingState.NEUTRAL;
                    command1Counter++;
                    progressBar.SetProgress((float)trainingRounds / maxRounds);
                    break;
            }
            //Adding the ability to repeat commands, but then also updating training round progress - EKL Edit
            if((neutralCounter + command1Counter) >= 2)
            {
                totalNeutral += neutralCounter;
                neutralCounter = 0;
                totalCommand[0] += command1Counter;
                command1Counter = 0;
                //Debug.Log("Total Neutral = " + totalNeutral);
                //Debug.Log("Total Command = " + totalCommand[0].ToString());
                //Debug.Log("Total Neutral Counter = " + neutralCounter);
                //Debug.Log("Total Command1 counter = " + command1Counter);
                trainingRounds++;

            }

            completionEnabled = trainingRounds >= minRounds;

        }

        ActivateUpNext();


        ApplyState();
    }

    void ApplyState()
    {
        feedbackEnabled = trainingRounds >= 4;
        feedbackAnim.SetBool("brushing", trainingState != TrainingState.NEUTRAL);
        completionButton.SetActive(completionEnabled);
    }

    void ActivateUpNext()
    {
        trainingProgressBar.Deactivate();
        upNextText.gameObject.SetActive(true);
        upNextText.text = "Ready to\n" +
            (trainingState == TrainingState.NEUTRAL ? "relax" : "brush") +
            "?";
    }

    public void StartTrainingCountdown()
    {
        trainingCountdown = true;
        timer = countdownTime;
        countDownText.enabled = true;
        countDownText.text = $"{(int)timer}";
        upNextText.gameObject.SetActive(false);
        completionButton.SetActive(false);
        progressBar.Deactivate();
    }

    // called by in engine UI
    public void AcceptTraining()
    {
        Cortex.training.AcceptTraining(action);
        trainingCompletionOptions.SetActive(false);
        earlyCompletionOption.SetActive(false);
    }
    // called by in engine UI
    public void RejectTraining()
    {
        Cortex.training.RejectTraining(action);
        trainingCompletionOptions.SetActive(false);
        earlyCompletionOption.SetActive(false);
        //ActivateUpNext();
    }

    // callsed by in engine UI
    public void Back()
    {
        timer = 0;
        trainingCountdown = false;
        backOptions.SetActive(true);
    }

    // called by overseer script
    public void ResetTraining()
    {
        gameObject.SetActive(true);
        progressBar.gameObject.SetActive(true);
        trainingState = TrainingState.NEUTRAL;
        completionEnabled = false;
        returning = false;
        
        trainingRounds = roundsTrained;
        progressBar.SetProgress((float)trainingRounds / maxRounds);

        ApplyState();
        ActivateUpNext();
    }
    // called by overseer script
    public void ResumeTraining()
    {
        gameObject.SetActive(true);
        progressBar.gameObject.SetActive(false);
        trainingState = TrainingState.NEUTRAL;
        completionEnabled = true;
        returning = true;
        trainingRounds = minRounds;
        ApplyState();
        ActivateUpNext();
    }
}
