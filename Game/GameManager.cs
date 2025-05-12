using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

// This script is in the Game namespace
namespace Game {
    
    /// <summary>
    /// This class is used to manage the game
    /// </summary>
    public class GameManager : MonoBehaviour {
        // Singleton instance
        public static GameManager Instance { get; private set; }
        
        // Variables for enemies
        [SerializeField]private List<GameObject> enemies = new ();
        
        // Variables for audio clips
        private AudioSource _audioSource;
        private readonly Queue<AudioClip> _soundQueue = new ();
        private bool _isPlayingSound;
        [SerializeField]private AudioClip gameOverSuccessClip;
        [SerializeField]private AudioClip gameOverFailureClip;
        [SerializeField]private AudioClip enemyDownClip;
        [SerializeField]private AudioClip gameStartClip;
        [SerializeField]private GameObject player;
        

        private void Awake() {
            // If an instance already exists and it's not this, destroy this GameObject
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Set the instance to this GameObject
            Instance = this;
        
        }

        private void Start() {
            _audioSource = GetComponent<AudioSource>();
            
            EnqueueSound(gameStartClip);
        }

        private void PlaySoundGameOverSuccess() {
            EnqueueSound(gameOverSuccessClip);
            player.GetComponent<PlayerController>().GameOverSuccess();
        }
        
        public void PlaySoundGameOverFailed() {
            EnqueueSound(gameOverFailureClip);
        }

        private void PlaySoundEnemyKilled() {
            EnqueueSound(enemyDownClip);
        }

        public void EnemyKilled(GameObject enemy) {
            //PlaySoundEnemyKilled();
            enemies.Remove(enemy);  
            if (enemies.Count == 0) {
                PlaySoundGameOverSuccess();  
            }
        }
        
        /// <summary>   
        /// Adds a sound to the queue and starts playing if no other sound is currently playing.
        /// </summary>
        private void EnqueueSound(AudioClip clip) {
            if (clip == null) return;

            _soundQueue.Enqueue(clip);

            // If no sound is currently playing, start playing the queued sounds
            if (!_isPlayingSound) {
                StartCoroutine(PlayQueuedSounds());
            }
        }

        /// <summary>
        /// Coroutine to play queued sounds sequentially.
        /// </summary>
        private IEnumerator PlayQueuedSounds() {
            _isPlayingSound = true;

            while (_soundQueue.Count > 0) {
                var clipToPlay = _soundQueue.Dequeue();
                _audioSource.PlayOneShot(clipToPlay);

                // Wait for the current sound to finish
                yield return new WaitForSeconds(clipToPlay.length);
            }

            _isPlayingSound = false;
        }

    }
}
