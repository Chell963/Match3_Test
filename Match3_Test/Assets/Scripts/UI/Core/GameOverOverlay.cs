using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Core
{
    public class GameOverOverlay : MonoBehaviour
    {
        [SerializeField] private Button exitButton;
        private void Start()
        {
            exitButton.onClick.AddListener(GoToMenu);
        }
        
        private void GoToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
