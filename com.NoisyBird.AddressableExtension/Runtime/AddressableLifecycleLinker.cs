using UnityEngine;

namespace NoisyBird.AddressableExtension
{
    public class AddressableLifecycleLinker : MonoBehaviour
    {
        private AddressableManager _manager;
        private string _key;
        private bool _isInitialized = false;

        public void Init(AddressableManager manager, string key)
        {
            _manager = manager;
            _key = key;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized && _manager != null)
            {
                _manager.ReleaseAsset(_key);
            }
        }
    }
}
