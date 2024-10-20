using UnityEngine;
using UnityEngine.Pool;

namespace ScLib.ImpactSystem.Pool
{
    [RequireComponent(typeof(ParticleSystem))]
    public class PoolableObject : MonoBehaviour
    {
        public ObjectPool<GameObject> poolOrigine;

        void OnParticleSystemStopped ()
        {
            if (poolOrigine != null)
            {
                poolOrigine.Release(gameObject);
                gameObject.SetActive(false);
            }
        }
    }
}