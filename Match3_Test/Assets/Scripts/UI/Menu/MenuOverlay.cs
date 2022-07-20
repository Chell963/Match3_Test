using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Menu
{
    public class MenuOverlay : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        private void Start()
        {
            playButton.onClick.AddListener(GoToGameplay);
        }

        private void GoToGameplay()
        {
            SceneManager.LoadScene("CoreGameplay");
        }
    }
}
