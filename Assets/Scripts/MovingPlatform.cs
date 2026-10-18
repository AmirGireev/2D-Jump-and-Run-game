using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Ziele")]
    public Transform punktA;
    public Transform punktB;
    
    [Header("Einstellungen")]
    public float geschwindigkeit = 3f;

    private Vector2 ziel;

    void Start()
    {
        // Die Plattform fährt zuerst zu Punkt B
        ziel = punktB.position;
    }

    void Update()
    {
        // Bewegt die Plattform schrittweise zum aktuellen Ziel
        transform.position = Vector2.MoveTowards(transform.position, ziel, geschwindigkeit * Time.deltaTime);

        // Prüfen, ob das Ziel fast erreicht ist (Abstand kleiner als 0.1)
        if (Vector2.Distance(transform.position, ziel) < 0.1f)
        {
            // Ziel wechseln
            if (ziel == (Vector2)punktA.position) 
            {
                ziel = punktB.position;
            }
            else 
            {
                ziel = punktA.position;
            }
        }
    }

    // Wenn der Spieler auf die Plattform springt
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Spieler an die Plattform heften
            collision.transform.SetParent(transform);
        }
    }

    // Wenn der Spieler die Plattform verlässt
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Spieler wieder von der Plattform lösen
            collision.transform.SetParent(null);
        }
    }
}