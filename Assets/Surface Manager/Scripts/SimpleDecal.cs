using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace ScLib.ImpactSystem
{
    public class SimpleDecal : MonoBehaviour
    {
        [SerializeField] float visibleDuration = 3f;
        [SerializeField] float fadeDuration = 2f;

        Vector3 initialScale;

        void Awake()
        {
            initialScale = transform.localScale;
        }

        void OnEnable()
        {
            transform.localScale = initialScale;
        }

        public async UniTask FadeOutDecal(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(visibleDuration), cancellationToken: ct);
            await LMotion.Create(transform.localScale, Vector3.zero, fadeDuration)
                .BindToLocalScale(transform).ToUniTask(ct);
        }
    }
}
