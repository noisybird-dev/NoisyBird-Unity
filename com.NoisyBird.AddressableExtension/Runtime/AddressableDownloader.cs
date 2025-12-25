using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NoisyBird.AddressableExtension
{
    public static class AddressableDownloader
    {
        /// <summary>
        /// Checks the download size for the given key (or keys).
        /// </summary>
        public static void GetDownloadSizeAsync(object key, Action<long> onComplete, Action<Exception> onError = null)
        {
            Addressables.GetDownloadSizeAsync(key).Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    onError?.Invoke(op.OperationException);
                }
            };
        }

        public static void DownloadDependenciesAsync(string key, Action<float> onProgress, Action<bool> onComplete)
        {
            DownloadDependenciesAsync(new[] { key }, onProgress, onComplete);
        }

        /// <summary>
        /// Downloads dependencies for the given key (or keys).
        /// </summary>
        /// <param name="key">Addressable Key or Label</param>
        /// <param name="onProgress">Progress from 0.0 to 1.0</param>
        /// <param name="onComplete">Called when download finishes (success or fail)</param>
        public static void DownloadDependenciesAsync(IEnumerable<string> key, Action<float> onProgress, Action<bool> onComplete)
        {
            var handle = Addressables.DownloadDependenciesAsync(key);
            
            if (AddressableManager.Instance != null)
            {
               AddressableManager.Instance.StartCoroutine(TrackProgress(handle, onProgress, onComplete));
            }
            else
            {
                handle.Completed += (op) => {
                    onProgress?.Invoke(1f);
                    onComplete?.Invoke(op.Status == AsyncOperationStatus.Succeeded);
                    Addressables.Release(handle);
                };
            }
        }

        private static System.Collections.IEnumerator TrackProgress(AsyncOperationHandle handle, Action<float> onProgress, Action<bool> onComplete)
        {
            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                yield return null;
            }
            
            onProgress?.Invoke(1f);
            onComplete?.Invoke(handle.Status == AsyncOperationStatus.Succeeded);
            Addressables.Release(handle);
        }
    }
}
