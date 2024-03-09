using UnityEngine;

namespace Interact
{
    /// <summary>
    /// Base class for anything to interact with to inherit from.
    /// </summary>
    public abstract class InteractBase : MonoBehaviour
    {
        /// <summary>
        /// The message to display when in range.
        /// </summary>
        [field: SerializeField]
        [field: Tooltip("The message to display when in range.")]
        public string DisplayMessage { get; private set; }

        /// <summary>
        /// How far away this can be activated from.
        /// </summary>
        [field: SerializeField]
        [field: Tooltip("How far away this can be activated from.")]
        [field: Min(float.Epsilon)]
        public float ActivationDistance { get; private set; } = 3;

        /// <summary>
        /// The actions to perform when interacted with.
        /// </summary>
        public abstract void Interact();

        private void Awake()
        {
            // Add to the manager.
            GameManager.interactObjects.Add(this);
        }

        private void OnDestroy()
        {
            // Remove to the from the manager.
            GameManager.interactObjects.Remove(this);
        }
    }
}