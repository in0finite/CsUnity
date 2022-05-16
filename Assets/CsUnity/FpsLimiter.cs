using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CsUnity
{
    public class FpsLimiter : MonoBehaviour
    {
        public int fpsLimit = 60;

        void Start()
        {
            Application.targetFrameRate = this.fpsLimit;
        }

        void OnValidate()
        {
            Application.targetFrameRate = this.fpsLimit;
        }
    }
}
