using System;
using System.Collections;
using Game;
using Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script is in the Player namespace
namespace Player {
    
    
    /// <summary>
    /// This class is used to control the player character. It handles movement, shooting, reloading, and gravity.
    /// </summary>
    public class PlayerController : MonoBehaviour, IDamageable {
        
        // Public variables
        
        [Header("Player Movement & Look Settings")]
        [SerializeField] private float moveSpeed = 2.5f;              // The speed at which the player moves
        [SerializeField] private float sprintSpeed = 5f;              // The speed at which the player sprints
        [SerializeField] private float mouseSensitivity = 2f;         // The sensitivity of the mouse
        [SerializeField] private float jumpHeight = 2.0f;             // The height the player can jump
        [SerializeField] private float gravity = -9.81f;              // The gravity applied to the player
        [SerializeField] private float tiltSpeed = 2f;                // The speed at which the player tilts
        [SerializeField] private float maxTiltAngle = 20f;            // The angle that the player tilts to
        [SerializeField] private float upperCamClampLimit = 60;       // The upper limit of the camera's rotation
        [SerializeField] private float lowerCamClampLimit = -90;      // The lower limit of the camera's rotation
        [SerializeField] private float rotationLerpTime = 4f;         // The time it takes to lerp the camera's rotation
        [Space(10)]
        
        [Header("Player Transforms")]
        [SerializeField] private Transform bodyTransform;             // The transform of the player's body
        [SerializeField] private Transform tiltTransform;             // The transform of the player's tilt
        [SerializeField] private Transform cameraTransform;           // The transform of the player's camera
        [SerializeField] private Transform volumeTransform;           // The transform of the player's volume
        [Space(10)]
        
        [Header("Animator & Audio")]
        [SerializeField] private Animator animator;                   // The animator component of the player
        [SerializeField] private AudioSource audioSource;             // The audio source component of the player
        [SerializeField] private AudioClip shoot;                     // The shooting sound
        [SerializeField] private AudioClip clipIn;                    // The reload sound
        [SerializeField] private AudioClip clipOut;                   // The reload sound
        [Space(10)]
        
        [Header("Weapon and combat settings")]
        [SerializeField] private Transform crosshair;                 // The transform of the crosshair
        [SerializeField] private Transform flashLight;                // The transform of the flashlight
        [SerializeField] private GameObject muzzleFlash;              // The muzzle flash GameObject
        [SerializeField] private Transform magazine;                  // Reference to the magazine
        [SerializeField] private Transform handTransform;             // Transform of the hand to hold the magazine
        [SerializeField] private Transform gunTransform;              // Transform of the gun to parent the magazine back to
        [Space(10)]

        // Private variables
        private CharacterController _controller;                      // The character controller component of the player
        private Vector3 _velocity;                                    // The velocity of the player
        private float _xRotation;                                     // The rotation of the player around the x-axis
        private float _currentTilt;                                   // The current tilt of the player
        private bool _isSprinting;                                    // The sprinting state of the player
        private bool _isReloading;                                    // The reloading state of the player
        private bool _isReloadingCoroutineRunning;                    // The reloading coroutine state of the player
        private bool gameOver;                                        // The game over state of the player

        private void Start() {
            // Get the required components
            _controller = GetComponent<CharacterController>();

            //Lock the cursor
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        private void Update() {
            if (gameOver) {
                if (Input.GetKeyDown(KeyCode.R)) {
                    ReloadScene();
                }
                return;
            }
            HandleTilt();                   // Handle the camera tilt
            HandleMovement();               // Handle player movement
            HandleShootingAndReloading();   // Handle shooting and reloading
            HandleGravity();                // Handle gravity
            HandleFlashlight();             // Handle flashlight toggle
        }

        /// <summary>
        /// Handles the flashlight toggle.
        /// </summary>
        private void HandleFlashlight() {
            // Toggle the flashlight on/off with the F key
            if (Input.GetKeyDown(KeyCode.F)) 
                flashLight.gameObject.SetActive(!flashLight.gameObject.activeSelf);
        }

        /// <summary>
        /// Handles the camera tilt based on player input.
        /// </summary>
        private void HandleTilt() {
            // Handle camera tilt with Q and E keys
            if (Input.GetKey(KeyCode.E))      // Tilt right
                _currentTilt = Mathf.MoveTowards(_currentTilt, -maxTiltAngle, tiltSpeed * Time.deltaTime);
            else if (Input.GetKey(KeyCode.Q)) // Tilt left
                _currentTilt = Mathf.MoveTowards(_currentTilt, maxTiltAngle, tiltSpeed * Time.deltaTime);
            else                              // Reset tilt
                _currentTilt = Mathf.MoveTowards(_currentTilt, 0, tiltSpeed * Time.deltaTime);
            
            // Set the tilt rotation of the player
            tiltTransform.localRotation = Quaternion.Euler(0f, 0f, _currentTilt);
        }

        /// <summary>
        /// Handles player movement based on input from the player.
        /// </summary>
        private void HandleMovement() {
            // Get the player's input
            var moveX = Input.GetAxis("Horizontal");
            var moveZ = Input.GetAxis("Vertical");
            
            // Calculate the movement direction
            var move = transform.right * moveX + transform.forward * moveZ;

            // Move the player character
            _controller.Move(move * (moveSpeed * Time.deltaTime));

            // Set running state based on whether the player is moving
            var isMoving = moveX != 0 || moveZ != 0;                    // Check if the player is moving
            _isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift); // Sprinting is only possible when moving
            moveSpeed = _isSprinting ? sprintSpeed : moveSpeed;         // Set the move speed based on sprinting
            var zRotation = _isSprinting ? 0 : 30;                   // Set the z rotation of the camera based on sprinting
        
        
            // Lerp the camera's Z rotation smoothly
            var currentRotation = cameraTransform.localRotation;
            var targetRotation = Quaternion.Euler(0f, -14.5f, zRotation);
            cameraTransform.localRotation = Quaternion.Lerp(currentRotation, targetRotation, rotationLerpTime * Time.deltaTime);

            // Set the running state in the animator based on sprinting
            animator.SetBool("isRunning", _isSprinting); 

            // Rotate the player left/right based on mouseX
            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Rotate the player left/right based on mouseX
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera up/down based on mouseY
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, lowerCamClampLimit, upperCamClampLimit); 
            bodyTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }

        
    
    
        /// <summary>
        /// Handles shooting and reloading of the player.
        /// </summary>
        private void HandleShootingAndReloading() {
            // Check if the reload animation has started and start the reload coroutine if it hasn't already
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Reload") && !_isReloadingCoroutineRunning && _isReloading)
                StartCoroutine(ReloadCoroutine());
        
            // Shooting
            if (Input.GetKeyDown(KeyCode.Mouse0) && !_isSprinting && !_isReloading) {
                StartCoroutine(MuzzleFlashCoroutine());
                PerformRaycast();
                GameEvents.TriggerShoot();
            }

            // Reloading
            if (Input.GetKeyDown(KeyCode.R) && !_isReloading) {
                _isReloading = true;
                animator.SetTrigger("reload"); // Trigger the reload animation
            }
        }
    
        /// <summary>
        /// Performs a raycast from the crosshair GameObject's position and direction, and tries to damage any object implementing IDamageable.
        /// </summary>
        private void PerformRaycast()
        {
            // Ensure the crosshair reference is set
            if (crosshair == null) {
                Debug.LogWarning("Crosshair GameObject not set.");
                return;
            }

            // Cast a ray from the crosshair's position in its forward direction
            var ray = new Ray(crosshair.position, (crosshair.position  - Camera.main.transform.position).normalized);

            // Check if the ray hits anything
            if (Physics.Raycast(ray, out var hit, 1000f)) {
                // Check if the hit object has a component that implements IDamageable
                var damageable = hit.collider.GetComponent<IDamageable>();
                // Apply damage
                damageable?.TakeDamage(100f, hit.point);
            }
            
            Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);
        }
    
    
        /// <summary>
        /// Coroutine to briefly enable the muzzle flash.
        /// </summary>
        private IEnumerator MuzzleFlashCoroutine() {
            // Enable muzzle flash
            muzzleFlash.SetActive(true);
        
            // Play the shooting sound
            audioSource.PlayOneShot(shoot); 

            // Wait for the specified duration
            yield return new WaitForSeconds(0.005f);
        
            // Disable muzzle flash
            muzzleFlash.SetActive(false);
        }

        
        /// <summary>
        /// Coroutine to handle reloading, including parenting magazine to hand.
        /// </summary>
        private IEnumerator ReloadCoroutine() {
            // Set the reloading coroutine state
            _isReloadingCoroutineRunning = true;
            
            // Wait for a short duration
            yield return new WaitForSeconds(0.2f);
            
            // Store the original position of the magazine
            var originalPos = magazine.localPosition; 
            magazine.SetParent(handTransform);// Parent the magazine to the player's hand
            magazine.localPosition = new Vector3(-0.00139999995f,0.0777999982f,0.1127f);// Set the position and rotation of the magazine in the player's hand
            audioSource.PlayOneShot(clipOut); // Play the reload sound

            // Wait for the reload duration
            yield return new WaitForSeconds(1f);
        
            // Play the reload sound
            audioSource.PlayOneShot(clipIn); 
            
            magazine.SetParent(gunTransform); // Re-parent the magazine back to the gun
            magazine.localPosition = originalPos; // Adjust position if needed
            magazine.localRotation = Quaternion.identity; // Adjust rotation if needed
            
            _isReloadingCoroutineRunning = false; // Set the reloading coroutine state
            _isReloading = false; // Set the reloading state
        }

        /// <summary>
        /// Handles the gravity and jumping of the player.
        /// </summary>
        private void HandleGravity() {
            // Check if the player is grounded
            if (_controller.isGrounded && _velocity.y < 0)
                _velocity.y = -2f; 

            // Jumping
            if (Input.GetButtonDown("Jump") && _controller.isGrounded)
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            // Apply gravity
            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        /// <summary>
        /// This method is used to take damage. It takes in the amount of damage and the hit point.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="hitPoint"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void TakeDamage(float amount, Vector3 hitPoint) {
            Debug.Log("Player took damage: " + amount);
            flashLight.gameObject.SetActive(false);
            volumeTransform.gameObject.SetActive(true);
            GameManager.Instance.PlaySoundGameOverFailed();
            Time.timeScale = 0f;
            gameOver = true;
        }
        
        /// <summary>
        /// Reloads the current scene.
        /// </summary>
        private static void ReloadScene() {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        public void GameOverSuccess() {
            StartCoroutine(EndGame());

        }

        private IEnumerator EndGame() {
            yield return new WaitForSeconds(6f);
            Time.timeScale = 0f;
            volumeTransform.gameObject.SetActive(true);
            gameOver = true;
        }
    }
}
