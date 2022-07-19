using System;
using UnityEngine;

namespace Progress
{
    public class ProgressModel : MonoBehaviour
    {
        public event Action<int> TimeCountdown;
        public event Action<int> ScoreEvent;

        public event Action GameOver;
        
        private float timeSpent;
        private int score = 0;

        public int GetScore()
        {
            return score;
        }

        public float GetTime()
        {
            return timeSpent;
        }
    
        public void CountScore()
        {
            score += 1;
            ScoreEvent?.Invoke(score);
        }

        private void Update()
        {
            if ((int)timeSpent >= 60)
            {
                GameOver?.Invoke();
            }
            else
            {
                timeSpent += Time.deltaTime;
                if (Mathf.Abs(timeSpent - (int)timeSpent) < 0.3f)
                {
                    TimeCountdown?.Invoke((int)timeSpent);
                }
            }
        }
    }
}
