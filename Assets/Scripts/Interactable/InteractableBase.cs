using UnityEngine;

namespace Interactable
{
    /// <summary>
    /// Base class for anything to interact with to inherit from.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour
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
            // Add the interactable to the manager.
            GameManager.interactableObjects.Add(this);
        }

        private void OnDestroy()
        {
            // Remove the interactable from the manager.
            GameManager.interactableObjects.Remove(this);
        }
    }
}