using UnityEngine;
using UGameCore.Utilities.Editor;

namespace CsUnity
{
    public class FpsLimiter : MonoBehaviour
    {
        public ushort fpsLimit = 60;


        static FpsLimiter()
        {
            SetEditModeLimit(30);
        }

        void Start()
        {
            Application.targetFrameRate = this.fpsLimit;
            SetEditModeLimit(this.fpsLimit);
        }

        void OnValidate()
        {
            Application.targetFrameRate = this.fpsLimit;
            SetEditModeLimit(this.fpsLimit);
        }

        static void SetEditModeLimit(ushort value)
        {
            FpsLimiterForEditMode.IsEnabled = true;
            FpsLimiterForEditMode.TargetFps = value;
        }
    }
}
