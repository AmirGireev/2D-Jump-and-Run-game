using UnityEngine;

public class Destructible : MonoBehaviour
{
    public int health = 1;
    public GameObject destroyEffect;

    public void TakeHit(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            if (destroyEffect != null)
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            gameObject.SetActive(false);
        }
    }
}
