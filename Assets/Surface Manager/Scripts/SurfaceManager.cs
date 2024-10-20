using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScLib.ImpactSystem.Effects;
using System;

namespace ScLib.ImpactSystem
{
    public class SurfaceManager : MonoBehaviour
    {
        IImpactSound impactSound;
        public void Initialize(IImpactSound soundSystem)
        {
            impactSound = soundSystem;
        }

        [SerializeField]
        private List<SurfaceType> Surfaces = new List<SurfaceType>();
        [SerializeField]
        private Surface DefaultSurface;
        private Dictionary<GameObject, IEffectObjectPool> ObjectPools = new();

        static SurfaceManager instance;

        void Awake()
        {
            // インスタンスが既に存在し、自分自身ではない場合、重複を破棄する
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public static void HandleImpact(GameObject HitObject, in Vector3 HitPoint, in Vector3 HitNormal, ImpactType Impact, int TriangleIndex = 0)
        {
            if (HitObject.TryGetComponent(out Terrain terrain))
            {
                instance.PlayEffectsByTerrain(terrain, HitPoint, HitNormal, Impact);
                return;
            }

            if (HitObject.TryGetComponent(out Renderer renderer))
            {
                // TODO Material による判定に変えるべきか？
                instance.PlayEffectsByRenderer(renderer, HitPoint, HitNormal, Impact, TriangleIndex);
                return;
            }

            if (HitObject.TryGetComponent(out IImpactable impactable))
            {
                instance.PlayEffectsByRenderer(impactable.GetRenderer(), HitPoint, HitNormal, Impact, TriangleIndex);
            }
        }

        private void PlayEffectsByTerrain(Terrain terrain, in Vector3 hitPoint, in Vector3 hitNormal, ImpactType impactType)
        {
            List<TextureAlpha> activeTextures = GetActiveTexturesFromTerrain(terrain, hitPoint);
            foreach (TextureAlpha activeTexture in activeTextures)
            {
                SurfaceType surfaceType = Surfaces.Find(surface => surface.Albedo == activeTexture.Texture);
                if (surfaceType != null)
                {
                    foreach (Surface.SurfaceImpactTypeEffect typeEffect in surfaceType.Surface.ImpactTypeEffects)
                    {
                        if (typeEffect.ImpactType == impactType)
                        {
                            PlayEffects(hitPoint, hitNormal, typeEffect.SurfaceEffect);
                        }
                    }
                }
                else
                {
                    foreach (Surface.SurfaceImpactTypeEffect typeEffect in DefaultSurface.ImpactTypeEffects)
                    {
                        if (typeEffect.ImpactType == impactType)
                        {
                            PlayEffects(hitPoint, hitNormal, typeEffect.SurfaceEffect);
                        }
                    }
                }
            }
        }

        private void PlayEffectsByRenderer(Renderer renderer, in Vector3 HitPoint, in Vector3 HitNormal, ImpactType Impact, int TriangleIndex = 0)
        {
            Texture activeTexture = GetActiveTextureFromRenderer(renderer, TriangleIndex);

            SurfaceType surfaceType = Surfaces.Find(surface => surface.Albedo == activeTexture);
            if (surfaceType != null)
            {
                foreach (Surface.SurfaceImpactTypeEffect typeEffect in surfaceType.Surface.ImpactTypeEffects)
                {
                    if (typeEffect.ImpactType == Impact)
                    {
                        PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect);
                    }
                }
            }
            else
            {
                foreach (Surface.SurfaceImpactTypeEffect typeEffect in DefaultSurface.ImpactTypeEffects)
                {
                    if (typeEffect.ImpactType == Impact)
                    {
                        PlayEffects(HitPoint, HitNormal, typeEffect.SurfaceEffect);
                    }
                }
            }

        }

        List<TextureAlpha> workTextures = new List<TextureAlpha>();
        private List<TextureAlpha> GetActiveTexturesFromTerrain(Terrain Terrain, in Vector3 HitPoint)
        {
            Vector3 terrainPosition = HitPoint - Terrain.transform.position;
            Vector3 splatMapPosition = new Vector3(
                terrainPosition.x / Terrain.terrainData.size.x,
                0,
                terrainPosition.z / Terrain.terrainData.size.z
            );

            int x = Mathf.FloorToInt(splatMapPosition.x * Terrain.terrainData.alphamapWidth);
            int z = Mathf.FloorToInt(splatMapPosition.z * Terrain.terrainData.alphamapHeight);

            float[,,] alphaMap = Terrain.terrainData.GetAlphamaps(x, z, 1, 1);

            workTextures.Clear();
            List<TextureAlpha> activeTextures = workTextures;
            for (int i = 0; i < alphaMap.Length; i++)
            {
                if (alphaMap[0, 0, i] > 0)
                {
                    activeTextures.Add(new TextureAlpha(alphaMap[0, 0, i], Terrain.terrainData.terrainLayers[i].diffuseTexture));
                }
            }

            return activeTextures;
        }

        private Texture GetActiveTextureFromRenderer(Renderer Renderer, int TriangleIndex)
        {
            if (Renderer.TryGetComponent(out MeshFilter meshFilter))
            {
                Mesh mesh = meshFilter.mesh;

                return GetTextureFromMesh(mesh, TriangleIndex, Renderer.sharedMaterials);
            }
            else if (Renderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer smr = (SkinnedMeshRenderer)Renderer;
                Mesh mesh = smr.sharedMesh;

                return GetTextureFromMesh(mesh, TriangleIndex, Renderer.sharedMaterials);
            }

            Debug.LogError($"{Renderer.name} has no MeshFilter or SkinnedMeshRenderer! Using default impact effect instead of texture-specific one because we'll be unable to find the correct texture!");
            return null;
        }

        private Texture GetTextureFromMesh(Mesh Mesh, int TriangleIndex, Material[] Materials)
        {
            if (Mesh.isReadable && Mesh.subMeshCount > 1)
            {
                int[] hitTriangleIndices = new int[]
                {
                    Mesh.triangles[TriangleIndex * 3],
                    Mesh.triangles[TriangleIndex * 3 + 1],
                    Mesh.triangles[TriangleIndex * 3 + 2]
                };

                for (int i = 0; i < Mesh.subMeshCount; i++)
                {
                    int[] submeshTriangles = Mesh.GetTriangles(i);
                    for (int j = 0; j < submeshTriangles.Length; j += 3)
                    {
                        if (submeshTriangles[j] == hitTriangleIndices[0]
                            && submeshTriangles[j + 1] == hitTriangleIndices[1]
                            && submeshTriangles[j + 2] == hitTriangleIndices[2])
                        {
                            return Materials[i].mainTexture;
                        }
                    }
                }
            }

            return Materials[0].mainTexture;
        }


        private IEffectObjectPool CreateEffectObjectPool(SpawnObjectEffect spawnObjectEffect) => spawnObjectEffect.effectType switch
        {
            EffectType.Particle => new ParticleObjectPool(spawnObjectEffect.Prefab),
            EffectType.Decal => new DecalObjectPool(spawnObjectEffect.Prefab),
            _ => throw new InvalidOperationException()
        };

        private void PlayEffects(in Vector3 HitPoint, in Vector3 HitNormal, SurfaceEffect SurfaceEffect)
        {
            foreach (SpawnObjectEffect spawnObjectEffect in SurfaceEffect.SpawnObjectEffects)
            {
                if (spawnObjectEffect.Probability <= UnityEngine.Random.value) continue;

                if (!ObjectPools.ContainsKey(spawnObjectEffect.Prefab))
                {
                    ObjectPools.Add(spawnObjectEffect.Prefab, CreateEffectObjectPool(spawnObjectEffect));
                }

                Vector3 foward = HitNormal;
                Vector3 offset = Vector3.zero;

                if (spawnObjectEffect.RandomizeRotation)
                {
                    offset = new Vector3(
                        UnityEngine.Random.Range(0, 180 * spawnObjectEffect.RandomizedRotationMultiplier.x),
                        UnityEngine.Random.Range(0, 180 * spawnObjectEffect.RandomizedRotationMultiplier.y),
                        UnityEngine.Random.Range(0, 180 * spawnObjectEffect.RandomizedRotationMultiplier.z)
                    );
                }

                ObjectPools[spawnObjectEffect.Prefab].PlayEffect(HitPoint + HitNormal * 0.001f, foward, offset, transform, destroyCancellationToken).Forget();
            }

            foreach (PlayAudioEffect playAudioEffect in SurfaceEffect.PlayAudioEffects)
            {
                AudioClip clip = playAudioEffect.AudioClips[UnityEngine.Random.Range(0, playAudioEffect.AudioClips.Count)];

                impactSound?.Play(clip, playAudioEffect.audioMixerGroup, HitPoint);
            }
        }

        private readonly struct TextureAlpha : IEquatable<TextureAlpha>
        {
            public float Alpha { get; }
            public Texture Texture { get; }

            public TextureAlpha(float alpha, Texture texture)
            {
                Alpha = alpha;
                Texture = texture;
            }

            public bool Equals(TextureAlpha other)
            {
                return Alpha == other.Alpha && Equals(Texture, other.Texture);
            }

            public override bool Equals(object obj)
            {
                return obj is TextureAlpha other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Alpha, Texture?.GetHashCode() ?? 0);
            }

            public static bool operator ==(TextureAlpha left, TextureAlpha right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(TextureAlpha left, TextureAlpha right)
            {
                return !(left == right);
            }
        }
    }
}