using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace ScLib.ImpactSystem
{
    public class ParticleObjectPool : IEffectObjectPool
    {
        GameObject particlePrefab;
        private ObjectPool<ParticleSystem> particlePool;

        
        public ParticleObjectPool(GameObject particlePrefab, bool collectionCheck = true, int defaultCapacity = 30, int maxSize = 50)
        {
            this.particlePrefab = particlePrefab;
            // ObjectPool の初期化
            particlePool = new ObjectPool<ParticleSystem>(
                createFunc: CreateParticle,            // パーティクルの生成
                actionOnGet: OnGetParticle,            // 取得時の初期化処理
                actionOnRelease: OnReleaseParticle,    // 返却時のリセット処理
                actionOnDestroy: DestroyParticle,      // 破棄時の処理
                collectionCheck: collectionCheck,                // 重複チェック（必要に応じて）
                defaultCapacity: defaultCapacity,                   // 初期プールサイズ
                maxSize: maxSize                            // 最大プールサイズ
            );
        }

        // パーティクルを生成するメソッド（初回取得時のみ実行）
        private ParticleSystem CreateParticle()
        {
            var particle = GameObject.Instantiate(particlePrefab).GetComponent<ParticleSystem>();
            particle.gameObject.SetActive(false);
            return particle;
        }

        // パーティクルを取得する際の処理
        private void OnGetParticle(ParticleSystem particle)
        {
            particle.gameObject.SetActive(true);
        }

        // パーティクルをプールに返す際の処理
        private void OnReleaseParticle(ParticleSystem particle)
        {
            particle.Stop();
            particle.gameObject.SetActive(false);
        }

        // パーティクルを破棄する際の処理
        private void DestroyParticle(ParticleSystem particle)
        {
            GameObject.Destroy(particle.gameObject);
        }

        // パーティクルを取得するメソッド
        public async UniTaskVoid PlayEffect(Vector3 position, Vector3 foward, Vector3 offset, Transform parent, CancellationToken ct)
        {
            ParticleSystem particle = particlePool.Get();
            particle.transform.position = position;
            particle.transform.forward = foward;
            particle.transform.rotation = Quaternion.Euler(particle.transform.rotation.eulerAngles + offset);
            particle.transform.SetParent(parent);
            particle.Play();
            
            await UniTask.Yield(ct);
            await UniTask.WaitWhile(() => particle.IsAlive(true), cancellationToken: ct);
            
            particle.gameObject.SetActive(false);
            particlePool.Release(particle);
        }
    }
}