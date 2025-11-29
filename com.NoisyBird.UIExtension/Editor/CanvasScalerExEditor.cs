using System;
using NoisyBird.UIExtension.Canvas;
using UnityEditor;
using UnityEditor.UI;

namespace NoisyBird.UIExtension.Editor.Canvas
{
    [CustomEditor(typeof(CanvasScalerEx), true)]
    [CanEditMultipleObjects]
    public class CanvasScalerExEditor : CanvasScalerEditor
    {
        public override void OnInspectorGUI()
        {
            var canvasScalerEx = target as CanvasScalerEx;
            if (canvasScalerEx == null) return;
            float prvMatchValue = canvasScalerEx.matchWidthOrHeight;
            base.OnInspectorGUI();
            if (Math.Abs(prvMatchValue - canvasScalerEx.matchWidthOrHeight) > 0.001f)
            {
                CanvasScalerEx.SetMatchValue(canvasScalerEx.matchWidthOrHeight);
            }
        }
    }
}