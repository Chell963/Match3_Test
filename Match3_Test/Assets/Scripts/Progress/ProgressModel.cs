using System;
using UnityEngine;

namespace Progress
{
    public class ProgressModel : MonoBehaviour
    {
        //Подписки для вьюшки
        public event Action<int> TimeCountdown;
        public event Action<int> ScoreEvent;
        
        public event Action GameOver;
        
        private float timeLeft = 60;
        private int score;

        //Подсчет очков
        public void CountScore()
        {
            score += 1;
            ScoreEvent?.Invoke(score);
        }

        //Подсчет времени
        private void Update()
        {
            if ((int)timeLeft <= 0)
            {
                TimeCountdown?.Invoke(0);
                GameOver?.Invoke();
            }
            else
            {
                timeLeft -= Time.deltaTime;
                if (Mathf.Abs(timeLeft - (int)timeLeft) < 0.3f)
                {
                    TimeCountdown?.Invoke((int)timeLeft);
                }
            }
        }
    }
}
