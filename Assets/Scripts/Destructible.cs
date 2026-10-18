using UnityEngine;

// Auf Kisten, Waende, Gegner usw. legen. Braucht einen Collider2D.
public class Destructible : MonoBehaviour
{
    public int health = 1;
    public GameObject destroyEffect; // optional: Partikel-Prefab

    public void TakeHit(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            if (destroyEffect != null)
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
