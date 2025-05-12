using UnityEngine;

// This interface is in the Interfaces namespace
namespace Interfaces {
    
    // This interface is used to make sure that any object that implements it can take damage.
    public interface IDamageable {
        
        // This method is used to take damage. It takes in the amount of damage and the hit point.
        void TakeDamage(float amount, Vector3 hitPoint);
    }
}