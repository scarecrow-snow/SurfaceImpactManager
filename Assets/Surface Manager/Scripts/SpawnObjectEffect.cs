using System;
using UnityEngine;

namespace ScLib.ImpactSystem.Effects
{
    [CreateAssetMenu(menuName = "Impact System/Spawn Object Effect", fileName = "SpawnObjectEffect")]
    public class SpawnObjectEffect : ScriptableObject
    {
        public EffectType effectType = EffectType.Particle;
        public GameObject Prefab;
        public float Probability = 1;
        public bool RandomizeRotation;
        [Tooltip("Zero values will lock the rotation on that axis. Values up to 360 are sensible for each X,Y,Z")]
        public Vector3 RandomizedRotationMultiplier = Vector3.zero;
    }

    [Serializable]
    public enum EffectType
    {
        Particle,
        Decal,
    }
}