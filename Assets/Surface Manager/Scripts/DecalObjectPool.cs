using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Pool;

namespace ScLib.ImpactSystem
{
    public class DecalObjectPool : IEffectObjectPool
    {
        GameObject decalPrefab;
        private ObjectPool<SimpleDecal> decalPool;

        public DecalObjectPool(GameObject decalPrefab, bool collectionCheck = true, int defaultCapacity = 30, int maxSize = 50)
        {
            this.decalPrefab = decalPrefab;

            // ObjectPool の初期化
            decalPool = new ObjectPool<SimpleDecal>
            (
                createFunc: CreateDecal,            // パーティクルの生成
                actionOnGet: OnGetDecal,            // 取得時の初期化処理
                actionOnRelease: OnReleaseDecal,    // 返却時のリセット処理
                actionOnDestroy: DestroyDecal,      // 破棄時の処理
                collectionCheck: collectionCheck,   // 重複チェック（必要に応じて）
                defaultCapacity: defaultCapacity,   // 初期プールサイズ
                maxSize: maxSize                    // 最大プールサイズ
            );
        }


        private SimpleDecal CreateDecal()
        {
            var decal = GameObject.Instantiate(decalPrefab).GetComponent<SimpleDecal>();
            decal.gameObject.SetActive(false);
            return decal;
        }

        private void OnGetDecal(SimpleDecal decal)
        {
            decal.gameObject.SetActive(true);
        }
        
        private void OnReleaseDecal(SimpleDecal decal)
        {
            decal.gameObject.SetActive(false);
        }

        private void DestroyDecal(SimpleDecal decal)
        {
            GameObject.Destroy(decal.gameObject);
        }

        public async UniTaskVoid PlayEffect(Vector3 position, Vector3 foward, Vector3 offset, Transform parent, CancellationToken ct)
        {
            var decal = decalPool.Get();
            decal.transform.position = position;
            decal.transform.forward = foward;
            decal.transform.rotation = Quaternion.Euler(decal.transform.rotation.eulerAngles + offset);
            decal.transform.SetParent(parent);

            await decal.FadeOutDecal(ct);
            decal.gameObject.SetActive(false);
            decalPool.Release(decal);
        }
    }
}