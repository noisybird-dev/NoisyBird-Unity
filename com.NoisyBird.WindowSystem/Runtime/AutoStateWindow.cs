using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// AutoState Attribute를 사용하여 자동으로 상태를 저장/복구하는 Window 기본 클래스입니다.
    /// [AutoState] Attribute가 붙은 필드들을 자동으로 저장하고 복구합니다.
    /// </summary>
    public abstract class AutoStateWindow : WindowBase
    {
        // 지원하는 컴포넌트 타입별 상태 추출/복구 전략
        private static readonly Dictionary<Type, IStateStrategy> _strategies = new Dictionary<Type, IStateStrategy>
        {
            { typeof(ScrollRect), new ScrollRectStrategy() },
            { typeof(InputField), new InputFieldStrategy() },
            { typeof(Toggle), new ToggleStrategy() },
            { typeof(Slider), new SliderStrategy() },
            { typeof(Scrollbar), new ScrollbarStrategy() },
            { typeof(Dropdown), new DropdownStrategy() },
        };

        public override WindowState CaptureState()
        {
            AutoWindowState state = new AutoWindowState();

            // Reflection으로 [AutoState] 필드 찾기
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                AutoStateAttribute attr = field.GetCustomAttribute<AutoStateAttribute>();
                if (attr == null) continue;

                string key = string.IsNullOrEmpty(attr.Key) ? field.Name : attr.Key;
                object fieldValue = field.GetValue(this);

                if (fieldValue == null) continue;

                // 타입에 맞는 전략 찾기
                Type fieldType = field.FieldType;
                if (_strategies.TryGetValue(fieldType, out IStateStrategy strategy))
                {
                    object stateValue = strategy.CaptureState(fieldValue);
                    state.SetValue(key, stateValue);
                }
                else
                {
                    // 기본 타입이면 그대로 저장
                    if (IsSimpleType(fieldType))
                    {
                        state.SetValue(key, fieldValue);
                    }
                    else
                    {
                        Debug.LogWarning($"[AutoStateWindow] Unsupported type '{fieldType.Name}' for field '{field.Name}'. Skipping.");
                    }
                }
            }

            return state;
        }

        public override void RestoreState(WindowState state)
        {
            if (state is not AutoWindowState autoState)
            {
                Debug.LogError("[AutoStateWindow] State is not AutoWindowState.");
                return;
            }

            // Reflection으로 [AutoState] 필드 찾기
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                AutoStateAttribute attr = field.GetCustomAttribute<AutoStateAttribute>();
                if (attr == null) continue;

                string key = string.IsNullOrEmpty(attr.Key) ? field.Name : attr.Key;
                if (!autoState.HasKey(key)) continue;

                object fieldValue = field.GetValue(this);
                if (fieldValue == null) continue;

                // 타입에 맞는 전략 찾기
                Type fieldType = field.FieldType;
                if (_strategies.TryGetValue(fieldType, out IStateStrategy strategy))
                {
                    object stateValue = autoState.StateData[key];
                    strategy.RestoreState(fieldValue, stateValue);
                }
                else
                {
                    // 기본 타입이면 그대로 복구
                    if (IsSimpleType(fieldType))
                    {
                        object value = autoState.StateData[key];
                        field.SetValue(this, value);
                    }
                }
            }
        }

        /// <summary>
        /// 간단한 타입인지 확인 (int, float, string, bool 등)
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
        }

        // ===== State Strategies =====

        private interface IStateStrategy
        {
            object CaptureState(object component);
            void RestoreState(object component, object state);
        }

        private class ScrollRectStrategy : IStateStrategy
        {
            public object CaptureState(object component)
            {
                ScrollRect scrollRect = (ScrollRect)component;
                return new ScrollRectState
                {
                    HorizontalNormalizedPosition = scrollRect.horizontalNormalizedPosition,
                    VerticalNormalizedPosition = scrollRect.verticalNormalizedPosition
                };
            }

            public void RestoreState(object component, object state)
            {
                ScrollRect scrollRect = (ScrollRect)component;
                ScrollRectState scrollState = (ScrollRectState)state;
                
                // Canvas 업데이트 후 복구 (레이아웃이 완전히 계산된 후)
                Canvas.ForceUpdateCanvases();
                scrollRect.horizontalNormalizedPosition = scrollState.HorizontalNormalizedPosition;
                scrollRect.verticalNormalizedPosition = scrollState.VerticalNormalizedPosition;
            }

            [Serializable]
            private class ScrollRectState
            {
                public float HorizontalNormalizedPosition;
                public float VerticalNormalizedPosition;
            }
        }

        private class InputFieldStrategy : IStateStrategy
        {
            public object CaptureState(object component)
            {
                InputField inputField = (InputField)component;
                return inputField.text;
            }

            public void RestoreState(object component, object state)
            {
                InputField inputField = (InputField)component;
                inputField.text = (string)state;
            }
        }

        private class ToggleStrategy : IStateStrategy
        {
            public object CaptureState(object component)
            {
                Toggle toggle = (Toggle)component;
                return toggle.isOn;
            }

            public void RestoreState(object component, object state)
            {
                Toggle toggle = (Toggle)component;
                toggle.isOn = (bool)state;
            }
        }

        private class SliderStrategy : IStateStrategy
        {
            public object CaptureState(object component)
            {
                Slider slider = (Slider)component;
                return slider.value;
            }

            public void RestoreState(object component, object state)
            {
                Slider slider = (Slider)component;
                slider.value = (float)state;
            }
        }

        private class ScrollbarStrategy : IStateStrategy
        {
            public object CaptureState(object component)
            {
                Scrollbar scrollbar = (Scrollbar)component;
                return scrollbar.value;
            }

            public void RestoreState(object component, object state)
            {
                Scrollbar scrollbar = (Scrollbar)component;
                scrollbar.value = (float)state;
            }
        }

        private class DropdownStrategy : IStateStrategy
        {
            public object CaptureState(object component)
            {
                Dropdown dropdown = (Dropdown)component;
                return dropdown.value;
            }

            public void RestoreState(object component, object state)
            {
                Dropdown dropdown = (Dropdown)component;
                dropdown.value = (int)state;
            }
        }
    }
}
