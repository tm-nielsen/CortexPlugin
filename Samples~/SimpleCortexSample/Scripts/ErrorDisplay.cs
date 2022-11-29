using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CortexPlugin;
using UnityEngine.UI;
using TMPro;

namespace CortexExamples
{
    public class ErrorDisplay : MonoBehaviour
    {
        public float duration = 5;

        TextMeshProUGUI text;
        CanvasGroup canvasGroup;

        float timer = 0;


        void Start()
        {
            Cortex.ErrorRecieved += OnErrorMessageRecieved;

            text = GetComponent<TextMeshProUGUI>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
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
                timer -= Time.unscaledDeltaTime;
            else
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0, Time.unscaledDeltaTime);
        }
    }
}
