using System;

namespace Game {
    public static class GameEvents {
        public static event Action OnShoot;

        public static void TriggerShoot() {
            OnShoot?.Invoke();
        }
    }
}