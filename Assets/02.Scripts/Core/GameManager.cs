using UnityEngine;

namespace RPGPinball.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public int Score { get; private set; }
        public System.Action<int> OnScoreChanged;

        public void AddScore(int amount)
        {
            Score += amount;
            Debug.Log($"Score Added: {amount}. Total Score: {Score}");
            OnScoreChanged?.Invoke(Score);
        }
    }
}
