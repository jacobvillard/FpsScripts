using UnityEngine;

public class RedLaserScript : MonoBehaviour
{
    public float laserRange = 50f;                // Maximum range of the laser
    public float laserWidth = 0.05f;              // Width of the laser beam
    public Color laserColor = Color.red;          // Color of the laser
    public Transform laserOrigin;                 // Starting point of the laser (e.g., gun muzzle)
    public Transform player;                      // Player object

    private LineRenderer _lineRenderer;

    private void Start() {
        // Initialize the LineRenderer component
        _lineRenderer = GetComponent<LineRenderer>();
        
        if (_lineRenderer == null) {
            // Add a LineRenderer if one isn't attached to the GameObject
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Set laser appearance properties
        _lineRenderer.startWidth = laserWidth;
        _lineRenderer.endWidth = laserWidth;
        _lineRenderer.startColor = laserColor;
        _lineRenderer.endColor = laserColor;
        _lineRenderer.positionCount = 2; // The laser will have a start and an endpoint
        _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        _lineRenderer.material.color = laserColor; // Make sure the laser is visible with the correct color
    }

    private void LateUpdate () {
        ShootLaser();
    }

    private void ShootLaser() {
        // Set the starting position of the laser to the laser origin
        var laserStart = laserOrigin.position;
        _lineRenderer.SetPosition(0, laserStart);

        // Cast a ray forward to determine where the laser hits
        if (Physics.Raycast(laserStart, laserOrigin.forward, out var hit, laserRange)) {
            // Set the laser endpoint at the hit point
            _lineRenderer.SetPosition(1, hit.point);

            // Optional: if you want to apply effects to the object hit by the laser
            // Example: hit.collider.GetComponent<Health>()?.TakeDamage(damage);
        }
        else {
            // If no hit, extend the laser to its maximum range
            var laserEnd = laserStart + laserOrigin.forward * laserRange;
            _lineRenderer.SetPosition(1, laserEnd);
        }
    }
    
    /// <summary>
    /// Checks if the laser is pointing at the player.
    /// </summary>
    /// <returns>True if the laser is pointing at the player, false otherwise.</returns>
    public bool IsPointingAtPlayer() {
        // Cast a ray from the laser origin in the forward direction
        if (Physics.Raycast(laserOrigin.position, laserOrigin.forward, out var hit, laserRange)) {
            // Check if the hit object is the player
            if (hit.transform == player) {
                return true;
            }
        }
        return false;
    }
}
