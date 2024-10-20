using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ScLib.ImpactSystem
{
    public interface IEffectObjectPool
    {
        public UniTaskVoid PlayEffect(Vector3 position, Vector3 foward, Vector3 offset, Transform parent, CancellationToken ct);
    }
}