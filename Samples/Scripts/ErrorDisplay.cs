using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmotivUnityPlugin;
using UnityEngine.UI;
using TMPro;

public class ErrorDisplay : MonoBehavior
{
    public float duration = 5;

    TextMeshProUGUI text;
    CanvasGroup canvasGroup;

    float timer = 0;


    void Start()
    {
        if (!ShouldPersist()) return;

        Cortex.ErrorRecieved += OnErrorMessageRecieved;

        text = GetComponentInChildren<TextMeshProUGUI>(true);
        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        canvasGroup.alpha = 0;
    }

    void OnErrorMessageRecieved(ErrorMsgEventArgs args)
    {
        timer = duration;

        text.text = $"Error: {args.MessageError} on method {args.MethodName}";
        if (args.Code == -32000)
            text.text += ", this can happen due to a faulty internet connection.";

        canvasGroup.alpha = 1;
    }

    void Update()
    {
        if (timer > 0)
            timer -= Time.deltaTime;
        else
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0, Time.deltaTime);
    }
}
