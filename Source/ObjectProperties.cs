#if UNITY_EDITOR
using UnityEngine;
using System.Linq;
using VRC.SDK3.Avatars.Components;

namespace VRCAvatarActions
{
    //Object Toggles
    [System.Serializable]
    public class ObjectProperty
    {
        public ObjectProperty()
        {
        }
        public ObjectProperty(ObjectProperty source)
        {
            this.path = string.Copy(source.path);
            this.type = source.type;
            this.objects = source.objects?.ToArray();
            this.values = source.values?.ToArray();
            this.objRef = source.objRef;
        }

        public enum Type
        {
            ObjectToggle = 3450,
            MaterialSwap = 7959,
            BlendShape = 9301,
            PlayAudio = 9908,
        }
        public Type type = Type.ObjectToggle;

        //Data
        public string path;
        public UnityEngine.Object[] objects;
        public float[] values;

        //Meta-data
        public GameObject objRef;

        public void Clear()
        {
            objRef = null;
            path = null;
            objects = null;
            values = null;
        }

        public abstract class PropertyWrapper
        {
            public ObjectProperty prop;
            public PropertyWrapper(ObjectProperty property)
            {
                this.prop = property;
            }
            public string path
            {
                get { return prop.path; }
            }
            public GameObject objRef
            {
                get { return prop.objRef; }
            }
            public abstract void AddKeyframes(AnimationClip animation);
        }
        public class BlendShape : PropertyWrapper
        {
            public BlendShape(ObjectProperty property) : base(property) { }
            public void Setup()
            {
                if (prop.values == null || prop.values.Length != 2)
                    prop.values = new float[2];
                prop.objects = null;
            }
            public int index
            {
                get { return (int)prop.values[0]; }
                set { prop.values[0] = value; }
            }
            public float weight
            {
                get { return prop.values[1]; }
                set { prop.values[1] = value; }
            }
            public override void AddKeyframes(AnimationClip animation)
            {
                var skinned = objRef.GetComponent<SkinnedMeshRenderer>();
                var name = skinned.sharedMesh.GetBlendShapeName(index);

                //Create curve
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, weight));
                animation.SetCurve(path, typeof(SkinnedMeshRenderer), $"blendShape.{name}", curve);
            }
        }
        public class PlayAudio : PropertyWrapper
        {
            public PlayAudio(ObjectProperty property) : base(property) { }
            public void Setup()
            {
                if (prop.values == null || prop.values.Length != 4)
                {
                    prop.values = new float[4];
                    spatial = true;
                    volume = 1;
                    near = 6;
                    far = 20;
                }
                if (prop.objects == null || prop.objects.Length != 1)
                    prop.objects = new UnityEngine.Object[1];
            }
            public AudioClip audioClip
            {
                get { return prop.objects[0] as AudioClip; }
                set { prop.objects[0] = value; }
            }
            public float volume
            {
                get { return prop.values[1]; }
                set { prop.values[1] = value; }
            }
            public bool spatial
            {
                get { return prop.values[0] != 0; }
                set { prop.values[0] = value ? 1f : 0f; }
            }
            public float near
            {
                get { return prop.values[2]; }
                set { prop.values[2] = value; }
            }
            public float far
            {
                get { return prop.values[3]; }
                set { prop.values[3] = value; }
            }
            public override void AddKeyframes(AnimationClip animation)
            {
                if (audioClip == null)
                    return;

                //Find/Create child object
                var name = $"Audio_{audioClip.name}";
                var child = objRef.transform.Find(name)?.gameObject;
                if (child == null)
                {
                    child = new GameObject(name);
                    child.transform.SetParent(objRef.transform, false);
                }
                child.SetActive(false); //Disable

                //Find/Create component
                var audioSource = child.GetComponent<AudioSource>();
                if(audioSource == null)
                    audioSource = child.AddComponent<AudioSource>();
                audioSource.clip = audioClip;
                audioSource.volume = 0f; //Audio 0 by default

                //Spatial
                var spatialComp = child.GetComponent<VRCSpatialAudioSource>();
                if (spatialComp == null)
                    spatialComp = child.AddComponent<VRCSpatialAudioSource>();
                spatialComp.EnableSpatialization = spatial;
                spatialComp.Near = near;
                spatialComp.Far = far;

                //Create curve
                var subPath = $"{path}/{name}";
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(new Keyframe(0f, volume));
                    animation.SetCurve(subPath, typeof(AudioSource), $"m_Volume", curve);
                }
                {
                    var curve = new AnimationCurve();
                    curve.AddKey(new Keyframe(0f, 1f));
                    animation.SetCurve(subPath, typeof(GameObject), $"m_IsActive", curve);
                }
            }
        }
    }
}
#endif