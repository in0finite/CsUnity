using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CsUnity
{
    public static class Extensions
    {
        public static Vector3 ToUnityVec3(this SourceUtils.ValveBsp.Vector3S v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static SourceUtils.Vector3 ToSourceUtilsVec3(this Vector3 v)
        {
            return new SourceUtils.Vector3(v.x, v.y, v.z);
        }

        public static Vector3 Absolute(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }
    }
}
