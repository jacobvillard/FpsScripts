using System.Collections;
using System.Linq;
using Interfaces;
using UnityEngine;
using UnityEngine.AI;
using Game;

namespace Enemy {
    /// <summary>
    /// Enemy class that implements IDamageable interface.
    /// </summary>
    public class Enemy : MonoBehaviour, IDamageable {
        // Animator properties
        private static readonly int IsWalking = Animator.StringToHash("isWalking");
        private static readonly int Aim = Animator.StringToHash("aim");
        private static readonly int Idle = Animator.StringToHash("idle");

        // Enemy properties
        private Rigidbody[] _ragdollRigidbodies;            // Array of rigidbodies for ragdoll effect
        private Animator _animator;                         // Animator component
        private Collider _collider;                         // Collider component
        [SerializeField] private Transform player;          // Player transform
        [SerializeField] private GameObject bloodPrefab;    // Blood particle effect prefab
        [SerializeField] private float bloodParticleLength; // Blood particle effect length
 
        // Enemy movement properties
        [SerializeField] private Transform[] patrolPoints;  // Array of patrol points
        [SerializeField] private float attackRange;         // Attack range
        [SerializeField] private float searchDuration;      // Search duration
        
        // Enemy sight properties
        [SerializeField] private float fieldOfViewAngle = 60f;      // Field of view angle for line of sight check
        [SerializeField] private float searchLookAroundSpeed = 60f; // Speed for rotating when searching
        [SerializeField] private float searchLookAroundAngle = 45f; // Max angle deviation when looking around
        [SerializeField] private LayerMask playerLayer;             // Layer for the player
        [SerializeField] private LayerMask ignoreLayers;            // Layers to ignore
        [SerializeField] private float fireRate = 0.2f;             // Time interval between shots
        [SerializeField] private GameObject muzzleObject;           // Muzzle flash object
        private float _fireCooldown;                                // Time to wait before firing again
        private RedLaserScript _laserScript;                        // Reference to the RedLaserScript
 
                                                                                                                   

        //Audio
        private AudioSource _audioSource;
        [SerializeField] private AudioClip enemyShoot;
        
        
        // NavMeshAgent and EnemyState Variables
        private NavMeshAgent _agent;
        private EnemyState _currentState;
        private int _currentPatrolIndex;
        private float _initialRotationY;
        private float _searchTimer;
        private bool _dead;

        // Enemy states
        private enum EnemyState {
            Patrol,
            Search,
            Attack
        }
    
        private void Awake() {
            // Get components
            _animator = GetComponent<Animator>();
            _collider = GetComponent<Collider>();
            _agent = GetComponent<NavMeshAgent>(); 
            _audioSource = GetComponent<AudioSource>();
            _laserScript = GetComponentInChildren<RedLaserScript>();  
            _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        
            // Disable ragdoll rigidbodies
            DisableRagdollRigidbodies();
            
            // Set the player transform in the RedLaserScript
            _laserScript.player = player;
        }

        #region Ragdoll & Particles

        /// <summary>
        /// Disables the ragdoll rigidbodies.
        /// </summary>
        private void DisableRagdollRigidbodies() {
            // Disable ragdoll rigidbodies for animation
            foreach (var rigidbody in _ragdollRigidbodies) {
                rigidbody.isKinematic = true;
            }
        }
    
        /// <summary>
        /// Enables the ragdoll rigidbodies.
        /// </summary>
        private void EnableRagdollRigidbodies() {
            // Enable ragdoll rigidbodies
            foreach (var rigidbody in _ragdollRigidbodies) {
                rigidbody.isKinematic = false;
            }
        }

        /// <summary>
        /// Takes damage and enables ragdoll rigidbodies.
        /// </summary>
        /// <param name="amount">The amount of damage to take</param>
        /// <param name="hitPoint">The point at which damage was made contact</param>
        public void TakeDamage(float amount, Vector3 hitPoint) {
            // Disable animator and collider
            _animator.enabled = false;
            _collider.enabled = false;
            _dead = true;
        
            // Enable ragdoll rigidbodies
            EnableRagdollRigidbodies();
        
            // Add force to the closest rigidbody part such as head, chest, etc.
            var hitRigidbody = _ragdollRigidbodies.OrderBy(rigidbody => Vector3.Distance(rigidbody.transform.position, hitPoint)).First();
            hitRigidbody.AddForceAtPosition((transform.position - player.position) * 50, hitPoint, ForceMode.Impulse);
        
            // Create blood particle effect at hit point and destroy it after a certain time
            var blood = Instantiate(bloodPrefab, hitPoint, Quaternion.LookRotation(hitPoint.normalized));
            Destroy(blood, bloodParticleLength);
            
            // Call the EnemyKilled method in GameManager
            GameManager.Instance.EnemyKilled(gameObject);
        }

        #endregion
    
        #region Animation & Enemy States
        
        private void OnEnable() {
            GameEvents.OnShoot += AlertEnemy;
        }

        private void OnDisable() {
            GameEvents.OnShoot -= AlertEnemy;
        }
    
        private void Update() {
            // If the enemy is dead, return
            if(_dead)return;
            
            // Switch between enemy states
            switch (_currentState) {
                case EnemyState.Search:
                    Search();
                    break;
                case EnemyState.Attack:
                    Attack();
                    break;
                case EnemyState.Patrol:
                    Patrol();
                    break;
                default:
                    Patrol();
                    break;
            }
            
            //Debug.Log(_currentState);
        }

        /// <summary>
        /// Alerts the enemy to the player's presence.
        /// </summary>
        private void AlertEnemy() {
            // If the enemy is in the patrol state, switch to the search state
            if (_currentState == EnemyState.Patrol) {
                _currentState = EnemyState.Search;
                _searchTimer = searchDuration;
            }
        }
        
        private void Patrol() {
            _agent.isStopped = false;
            
            // Animate the enemy to walk
            _animator.SetBool(IsWalking, true);
            _animator.SetTrigger(Idle);
            
            // If there are no patrol points, return
            if (patrolPoints.Length == 0) return;

            // Set destination to the next patrol point
            if (!_agent.pathPending && _agent.remainingDistance < 0.5f) {
                // Move to the next patrol point
                _currentPatrolIndex++;

                // Reset to the first point if we've reached the end of the list
                if (_currentPatrolIndex >= patrolPoints.Length) {
                    _currentPatrolIndex = 0;
                }

                // Set the destination to the next patrol point
                _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
            }

            // Check if the player is within attack range and line of sight
            if (Vector3.Distance(player.position, transform.position) <= attackRange && CanSeePlayer()) {
                _currentState = EnemyState.Attack;
            } 
        }

        /// <summary>
        ///Searches for the player.
        /// </summary>
        private void Search() {
            _agent.isStopped = true;
            
            // Animate the enemy to look around
            _animator.SetBool(IsWalking, false);
            _animator.SetTrigger(Idle);

            //Decrement the search timer
            _searchTimer -= Time.deltaTime;
            
            //Look around while searching
            var directionToPlayer = (player.position - transform.position).normalized;
            var lookAtPlayerRotation = Quaternion.LookRotation(directionToPlayer);
            var angleOffset = Mathf.Sin(Time.time * searchLookAroundSpeed) * searchLookAroundAngle;
            var targetRotation = lookAtPlayerRotation * Quaternion.Euler(0, angleOffset, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 1.5f);
            
            //Switch to the patrol state if the search timer is less than or equal to 0
            if (_searchTimer <= 0) {
                _currentState = EnemyState.Patrol;
            } 
            //Switch to the attack state if the player is within attack range and line of sight
            else if (Vector3.Distance(player.position, transform.position) <= attackRange && CanSeePlayer()) {
                _currentState = EnemyState.Attack;
            }
        }

        /// <summary>
        /// Attacks the player.   
        /// </summary>
        private void Attack() {
            _agent.isStopped = true;
            
            // Animate the enemy to aim
            _animator.SetTrigger(Aim);
            
            // Switch to the search state if the player is out of attack range or line of sight
            if (Vector3.Distance(player.position, transform.position) > attackRange || !CanSeePlayer()) {
                _currentState = EnemyState.Search;
                _searchTimer = searchDuration;
            }
            
            // Set the destination to the current position and look at the player
            _agent.SetDestination(transform.position);

            //Look towards player
            var directionToPlayer = (new Vector3(player.position.x, transform.position.y, player.position.z) - transform.position).normalized;
            var targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f); 

            // Fire at the player
            if (Time.time >= _fireCooldown) {
                Fire();
                _fireCooldown = Time.time + fireRate;  // Set the next shot time
            }
        }

        private void Fire() {
            StartCoroutine(MuzzleFlashCoroutine());
            
            if(_laserScript.IsPointingAtPlayer()) {
                player.GetComponent<IDamageable>().TakeDamage(10, player.position);
            }
        }
        
        /// <summary>
        /// Coroutine to briefly enable the muzzle flash.
        /// </summary>
        private IEnumerator MuzzleFlashCoroutine() {
            // Enable muzzle flash
            muzzleObject.SetActive(true);
        
            // Play the shooting sound
            _audioSource.PlayOneShot(enemyShoot);

            // Wait for the specified duration
            yield return new WaitForSeconds(0.005f);
        
            // Disable muzzle flash
            muzzleObject.SetActive(false);
        }

        /// <summary>
        /// Checks if the player is within the enemy's line of sight.
        /// </summary>
        /// <returns></returns>
        private bool CanSeePlayer() {
            // Get the direction to the player
            var directionToPlayer = player.position - transform.position;

            // Draw the ray in the editor for visualization
            Debug.DrawRay(transform.position, directionToPlayer * attackRange, Color.red);
    
            // Return true if the player is in line of sight
            if (Physics.Raycast(transform.position, directionToPlayer, out var hit, attackRange, ~ignoreLayers)) {
                //Debug.Log(hit.collider.gameObject.name);
                return hit.transform == player;
            }
    
            return false;

        }
        #endregion
    
    
    }
}

