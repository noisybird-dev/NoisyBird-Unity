using System;
using UnityEngine;

namespace NoisyBird.MonoExtension
{
    [Flags]
    public enum CallBackEvent
    {
        None = 0,
        OnAwake = 1 << 0,
        OnStart = 1 << 1,
        OnDisable = 1 << 2,
        OnEnable = 1 << 3,
        OnDestroy = 1 << 4,
        OnUpdate = 1 << 5,
        OnLateUpdate = 1 << 6,
        OnFixedUpdate = 1 << 7,
    }
    
    public class CallBackMonoBehaviour<T> : MonoBehaviour where T : CallBackMonoBehaviour<T>
    {
        [SerializeField] private CallBackEvent _callBackEvent = CallBackEvent.None;
        
        public Action<T> OnAwakeAction;
        public Action<T> OnStartAction;
        public Action<T> OnDisableAction;
        public Action<T> OnEnableAction;
        public Action<T> OnDestroyAction;
        public Action<T> OnUpdateAction;
        public Action<T> OnLateUpdateAction;
        public Action<T> OnFixedUpdateAction;

        public virtual void Awake()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnAwake) == false) return;
            OnAwakeAction?.Invoke(this as T);
        }

        public virtual void Start()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnStart) == false) return;
            OnStartAction?.Invoke(this as T);
        }

        public virtual void OnEnable()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnEnable) == false) return;
            OnEnableAction?.Invoke(this as T);
        }

        public virtual void OnDisable()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnDisable) == false) return;
            OnDisableAction?.Invoke(this as T);
        }

        public virtual void OnDestroy()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnDestroy) == false) return;
            OnDestroyAction?.Invoke(this as T);
        }

        public virtual void Update()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnUpdate) == false) return;
            OnUpdateAction?.Invoke(this as T);
        }

        public virtual void LateUpdate()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnLateUpdate) == false) return;
            OnLateUpdateAction?.Invoke(this as T);
        }

        public virtual void FixedUpdate()
        {
            if (_callBackEvent.HasFlag(CallBackEvent.OnFixedUpdate) == false) return;
            OnFixedUpdateAction?.Invoke(this as T);
        }
    }
}