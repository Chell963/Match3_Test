using System;
using TMPro;
using UnityEngine;

namespace Progress
{
    public class ProgressView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerComponent;
        [SerializeField] private TextMeshProUGUI scoreComponent;
        
        public void RenderTime(int time)
        {
            var timeSpan = TimeSpan.FromSeconds(time);
            timerComponent.text =
                $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        
        public void RenderScore(int score)
        {
            scoreComponent.text = score.ToString();
        }
    }
}
