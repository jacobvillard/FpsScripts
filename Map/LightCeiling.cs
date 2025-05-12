using Interfaces;
using UnityEngine;

public class LightCeiling : MonoBehaviour, IDamageable
{
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        Destroy(gameObject);
    }
}
