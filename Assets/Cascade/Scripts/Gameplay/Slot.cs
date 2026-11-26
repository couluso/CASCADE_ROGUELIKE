// CASCADE — Auteur : Coulu
using UnityEngine;

namespace Cascade
{
    /// <summary>Case d'arrivée.</summary>
    public class Slot : MonoBehaviour
    {
        public int index;
        void OnTriggerEnter2D(Collider2D other)
        {
            var ball = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<BallController>() : null;
            if (ball != null) ball.Land(index);
        }
    }
}
