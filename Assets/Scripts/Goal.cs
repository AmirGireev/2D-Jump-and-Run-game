using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Mit Timer
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.Finish();
            return;
        }

        // Ohne Timer
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
            next = 0;

        SceneManager.LoadScene(next);
    }
}
