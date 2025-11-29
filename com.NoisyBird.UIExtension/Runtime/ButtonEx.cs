using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NoisyBird.UIExtension.UI
{
    [AddComponentMenu("UI/NoisyBird/ButtonEx")]
    public class ButtonEx : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [Serializable]
        public class ButtonClickedEvent : UnityEvent {}

        // Event delegates triggered on click.
        [FormerlySerializedAs("onClick")]
        [SerializeField]
        protected ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

        protected ButtonEx()
        {}

        public ButtonClickedEvent onClick
        {
            get { return m_OnClick; }
            set { m_OnClick = value; }
        }

        public enum ClickTransition
        {
            None,
            ScalePunch
        }

        [HideInInspector]
        [SerializeField]
        private ClickTransition m_ClickTransition = ClickTransition.None;

        [HideInInspector]
        [SerializeField]
        private Vector3 m_PunchScale = new Vector3(1.2f, 1.2f, 1.2f);

        [HideInInspector]
        [SerializeField]
        private float m_PunchDuration = 0.2f;

        public ClickTransition clickTransition
        {
            get { return m_ClickTransition; }
            set { m_ClickTransition = value; }
        }

        private Coroutine m_PunchCoroutine;
        private Vector3 m_OriginalScale;
        private bool m_IsScaling = false;

        protected override void Awake()
        {
            base.Awake();
            m_OriginalScale = transform.localScale;
        }

        protected virtual void Press()
        {
            if (!IsActive() || !IsInteractable())
                return;

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            m_OnClick.Invoke();
            PlaySound();

            if (m_ClickTransition == ClickTransition.ScalePunch)
            {
                if (m_PunchCoroutine != null)
                    StopCoroutine(m_PunchCoroutine);
                
                m_PunchCoroutine = StartCoroutine(PunchScaleCoroutine());
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        public virtual void PlaySound()
        {
            //Add Sound Play
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_IsScaling)
            {
                transform.localScale = m_OriginalScale;
                m_IsScaling = false;
            }
        }

        protected virtual IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }

        private IEnumerator PunchScaleCoroutine()
        {
            if (!m_IsScaling)
            {
                m_OriginalScale = transform.localScale;
                m_IsScaling = true;
            }

            float elapsedTime = 0f;
            float halfDuration = m_PunchDuration * 0.5f;

            // Scale Up
            while (elapsedTime < halfDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = elapsedTime / halfDuration;
                transform.localScale = Vector3.Lerp(m_OriginalScale, Vector3.Scale(m_OriginalScale, m_PunchScale), t);
                yield return null;
            }

            elapsedTime = 0f;

            // Scale Down
            while (elapsedTime < halfDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = elapsedTime / halfDuration;
                transform.localScale = Vector3.Lerp(Vector3.Scale(m_OriginalScale, m_PunchScale), m_OriginalScale, t);
                yield return null;
            }

            transform.localScale = m_OriginalScale;
            m_IsScaling = false;
        }
    }   
}
