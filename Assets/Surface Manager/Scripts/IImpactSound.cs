
using UnityEngine;
using UnityEngine.Audio;

namespace ScLib.ImpactSystem
{
    public interface IImpactSound
    {
        public void Play(AudioClip clip, AudioMixerGroup mixerGroup, Vector3 point);
    }
}