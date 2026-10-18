using UnityEngine;
using UnityEngine.SceneManagement;

// Tor/Flagge am Levelende mit Collider2D (Is Trigger).
// Alle Level-Szenen vorher unter File > Build Profiles (Build Settings) > Scene List eintragen.
public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Mit LevelTimer in der Szene: Zeit stoppen und Ergebnis-Tafel mit Bestenliste zeigen
        if (LevelTimer.Instance != null)
        {
            LevelTimer.Instance.Finish();
            return;
        }

        // Ohne LevelTimer: direkt das naechste Level laden
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
            next = 0; // letztes Level geschafft: zurueck zum Anfang

        SceneManager.LoadScene(next);
    }
}
