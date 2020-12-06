using System.Collections.Generic;
using UnityEngine;

namespace VRCAvatarActions
{
    public interface ITemporaryComponent
    {
    }

    public class ClipSource : MonoBehaviour, IAnimationClipSource, ITemporaryComponent
    {
        public List<AnimationClip> clips = new List<AnimationClip>();

        public void GetAnimationClips(List<AnimationClip> results)
        {
            results.AddRange(clips);
        }
    }
}