using UnityEngine;
using TMPro;
using RPGPinball.Core;

namespace RPGPinball.Core
{
    public class ScoreDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged += UpdateScoreText;
                UpdateScoreText(GameManager.Instance.Score);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= UpdateScoreText;
            }
        }

        private void UpdateScoreText(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = "Score: " + newScore.ToString("N0");
            }
        }
    }
}