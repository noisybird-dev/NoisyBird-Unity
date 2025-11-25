using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoisyBird.UIExtension.Canvas
{
    public class CanvasScalerEx : CanvasScaler
    {
        private static HashSet<CanvasScalerEx> _canvasScaler = new HashSet<CanvasScalerEx>();
        private static int _updatedFrameCount = -1;
        private static float _lastScaleMode = -1f;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _canvasScaler.Add(this);
            if (uiScaleMode != ScaleMode.ScaleWithScreenSize) return;
            if (screenMatchMode != ScreenMatchMode.MatchWidthOrHeight) return;
            SetMatchValue(_lastScaleMode < 0f ? matchWidthOrHeight : _lastScaleMode);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_canvasScaler.Contains(this))
            {
                _canvasScaler.Remove(this);
            }
        }
        
        /// <summary>
        /// Change Scaler Match
        /// </summary>
        /// <param name="value">0 ~ 1</param>
        public static void SetMatchValue(float value)
        {
            if (_updatedFrameCount == Time.renderedFrameCount && Math.Abs(_lastScaleMode - value) <= 0.01f)
            {
                return;
            }
            _updatedFrameCount = Time.renderedFrameCount;
            _lastScaleMode = value;
            value = Math.Clamp(value, 0f, 1f);
            foreach (var canvasScalerEx in _canvasScaler)
            {
                if(canvasScalerEx.uiScaleMode != ScaleMode.ScaleWithScreenSize) 
                    continue;
                if(canvasScalerEx.screenMatchMode != ScreenMatchMode.MatchWidthOrHeight) 
                    continue;
                canvasScalerEx.matchWidthOrHeight = value;
            }
        }
    }
}
