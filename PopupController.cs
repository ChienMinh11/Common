using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChieChie.Core
{
    public class PopupController : IPopupService, IPopupQueueService, IDisposable
    {
        private const float DestroyDuration = 7f;

        private readonly PopupConfig _config;
        private readonly IpopupFactory _popupFactory;

        private Canvas _canvas;
        private Transform _parentUp;
        private Transform _parentDown;
        private CanvasGroup _canvasGroup;

        private IPopupLoader _popupLoader;
        private PopupQueueManager _queueManager;
        private bool _isBackground;
        
        private readonly Dictionary<int, IPopup> _popups = new Dictionary<int, IPopup>();
        private readonly Stack<IPopup> _activePopups = new Stack<IPopup>();
        private readonly Stack<TemporaryPopupState> _temporaryPopupHistory = new Stack<TemporaryPopupState>();
        private readonly HashSet<int> _loadingPopupHashes = new HashSet<int>();
        private readonly Dictionary<int, CancellationTokenSource> _pendingDestroyTokens = new Dictionary<int, CancellationTokenSource>();
        
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource _fadeCts;

        public CanvasGroup CanvasGroup => _canvasGroup;
        public bool IsInitialized { get; private set; }

        public Action<IPopup> OnPopupClosedAction { get; set; }
        public Action<IPopup> OnPopupChangedAction { get; set; }

        public PopupController(PopupConfig config, IpopupFactory popupFactory,IPopupLoader popupLoader)
        {
            _config = config;
            _popupFactory = popupFactory; 
            _popupLoader = popupLoader;
        }

        public bool IsBackground
        {
            get => _isBackground;
            set
            {
                _isBackground = value;
                if (_parentUp != null) _parentUp.gameObject.SetActive(_isBackground);
            }
        }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            
            SetActive(false);

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _queueManager = new PopupQueueManager(this);
            
            IsInitialized = true;
            return UniTask.FromResult(true);
        }

        private void OnSceneUnloaded(Scene currentScene)
        {
            CloseAllPopups(noFade: true);
        }

        private int GetPopupHash(string nameId)
        {
            if (string.IsNullOrEmpty(nameId)) return 0;
            return nameId.GetHashCode();
        }
        private void OnPopupClosed(IPopup closedPopup)
        {
            if (_temporaryPopupHistory.Count > 0)
            {
                var nextRestoreState = _temporaryPopupHistory.Peek();
                int closedPopupHash = GetPopupHash(closedPopup.PopupNameId);

                if (closedPopupHash == nextRestoreState.TriggerPopupHash)
                {
                    _temporaryPopupHistory.Pop();
                    int restoreHash = GetPopupHash(nextRestoreState.TemporarilyClosedPopup.PopupNameId);
                    if (_popups.TryGetValue(restoreHash, out var restorePopup))
                    {
                        ShowPopupByHashInternal(restorePopup, restoreHash).Forget();
                    }
                }
            }
            
            OnPopupClosedAction?.Invoke(closedPopup);
        }

        private void OnPopupRequestedClose(IPopup requestedPopup)
        {
            if (_activePopups.Count > 0 && _activePopups.Peek() == requestedPopup)
            {
                CloseLastPopup();
            }
        }
        private void OnPopupRequestedCloseAll(IPopup requestedPopup)
        {
            CloseAllPopups(noFade: false);
        }

        public async UniTask<IPopup> PreloadPopup(string popupNameId)
        {
            int hashKey = popupNameId.GetHashCode();

            if (_popups.TryGetValue(hashKey, out IPopup cachedPopup) && cachedPopup != null)
            {
                if (_pendingDestroyTokens.TryGetValue(hashKey, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _pendingDestroyTokens.Remove(hashKey);
                   
                    if (cachedPopup is MonoBehaviour mono)
                    {
                        mono.gameObject.SetActive(false); 
                    }
                }
                return cachedPopup;
            }
            
            if (_loadingPopupHashes.Contains(hashKey))
            {
                await UniTask.WaitUntil(() => !_loadingPopupHashes.Contains(hashKey), cancellationToken: _cts.Token);
                if (_popups.TryGetValue(hashKey, out var loadedPopup)) return loadedPopup;
            }

            _loadingPopupHashes.Add(hashKey);

            try
            {
                GameObject prefab = await _popupLoader.LoadPrefabAsync(popupNameId, _cts.Token);
                if (prefab == null) return null;

                GameObject spawnedObj = _popupFactory.CreatePopup(prefab, _parentDown);
                spawnedObj.gameObject.SetActive(false);

                IPopup targetPopup = spawnedObj.GetComponent<IPopup>();
                if (targetPopup == null)
                {
                    UnityEngine.Object.Destroy(spawnedObj);
                    return null;
                }

                _popups.Add(hashKey, targetPopup);
                targetPopup.OnClosed += OnPopupClosed;
                targetPopup.OnRequestClose += OnPopupRequestedClose;
                targetPopup.OnRequestCloseAll += OnPopupRequestedCloseAll;
                targetPopup.OnHideRootCanvas += OnHideRootCanvas;
                targetPopup.Initialize();

                return targetPopup;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
            finally
            {
                _loadingPopupHashes.Remove(hashKey);
            }
        }

        public async UniTask<bool> ShowPopup(string popupNameId, string message = "", bool closeAndRestore = false, bool noFade = false)
        {
            int hashKey = popupNameId.GetHashCode();
            if (_popups.TryGetValue(hashKey, out IPopup cachedPopup) && cachedPopup != null)
            {
                if (_pendingDestroyTokens.TryGetValue(hashKey, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _pendingDestroyTokens.Remove(hashKey);
                   
                    if (cachedPopup is MonoBehaviour mono)
                    {
                        mono.gameObject.SetActive(false); 
                    }
                }
                return await ShowPopupByHashInternal(cachedPopup, hashKey, message, closeAndRestore, noFade);
            }
            if (_loadingPopupHashes.Contains(hashKey))
            {
                return false; 
            }
            _loadingPopupHashes.Add(hashKey);

            IPopup targetPopup = null;
            try
            {
                GameObject prefab = await _popupLoader.LoadPrefabAsync(popupNameId, _cts.Token);

                if (prefab == null)
                {
                    Debug.LogError($"[PopupController] Không thể nạp Prefab cho {popupNameId}");
                    _loadingPopupHashes.Remove(hashKey);
                    return false;
                }

                GameObject spawnedObj =_popupFactory.CreatePopup(prefab, _parentDown);
                spawnedObj.gameObject.SetActive(false);

                targetPopup = spawnedObj.GetComponent<IPopup>();
                if (targetPopup == null)
                {
                    UnityEngine.Object.Destroy(spawnedObj);
                    _loadingPopupHashes.Remove(hashKey);
                    return false;
                }
                _popups.Add(hashKey, targetPopup);
           
                targetPopup.OnClosed += OnPopupClosed;
                targetPopup.OnRequestClose += OnPopupRequestedClose;
                targetPopup.OnRequestCloseAll += OnPopupRequestedCloseAll;
                targetPopup.OnHideRootCanvas += OnHideRootCanvas;
                targetPopup.Initialize();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _loadingPopupHashes.Remove(hashKey); 
                return false;
            }
            finally
            {
                _loadingPopupHashes.Remove(hashKey);
            }

            bool isShowSuccess = await ShowPopupByHashInternal(targetPopup, hashKey, message, closeAndRestore, noFade);

            if (!isShowSuccess && targetPopup != null && !targetPopup.IsCacheable)
            {
                UnregisterPopupActions(targetPopup);
                _popups.Remove(hashKey);
                if (targetPopup is MonoBehaviour mono && mono != null)
                {
                    UnityEngine.Object.Destroy(mono.gameObject);
                }
            }

            return isShowSuccess;
        }

        public T GetPopup<T>(string popupNameId) where T : class, IPopup
        {
            int hashKey = GetPopupHash(popupNameId);
            if (_popups.TryGetValue(hashKey, out IPopup popup))
            {
                return popup as T;
            }
            return null;
        }

        private async UniTask<bool> ShowPopupByHashInternal(IPopup targetPopup, int hashKey, string message = "", bool closeAndRestore = false, bool noFade = false)
        {
            if (_activePopups.Count > 0 && _activePopups.Peek() == targetPopup)
            {
                return false;
            }

            bool isFirstPopup = _activePopups.Count == 0;

            if (isFirstPopup)
            {
                SetActive(true);
                if (_canvasGroup != null)
                {
                    if (noFade)
                    {
                        SetBackgroundAlphaImmediately(1f);
                    }
                    else
                    {
                        _canvasGroup.alpha = 0f;
                        FadeBackgroundAsync(fadeIn: true, _cts.Token).Forget();
                    }
                }
            }

            if (closeAndRestore && _activePopups.Count > 0)
            {
                IPopup currentTopPopup = _activePopups.Peek();
                _temporaryPopupHistory.Push(new TemporaryPopupState(currentTopPopup, hashKey));
                await CloseLastPopupInternal(isTemporary: true);
            }
            else if (_activePopups.Count > 0)
            {
                var previousPopup = _activePopups.Peek();
                previousPopup.transform.SetParent(_parentDown);
                if (previousPopup is MonoBehaviour monoPrev) monoPrev.gameObject.SetActive(false);
            }

            targetPopup.transform.SetParent(_parentUp);

            if (targetPopup is MonoBehaviour monoPopup)
            {
                monoPopup.gameObject.SetActive(true);
            }

            if (targetPopup is IPopupWithData popupWithData)
            {
                popupWithData.SetMessage(message);
            }

            _activePopups.Push(targetPopup);
            SetActive(true);

            await targetPopup.Show();

            OnPopupChangedAction?.Invoke(targetPopup);
            return true;
        }

        public void CloseLastPopup() => CloseLastPopupInternal(false).Forget();

        private async UniTask CloseLastPopupInternal(bool isTemporary)
        {
            if (_activePopups.Count <= 0) return;

            var popup = _activePopups.Pop();
            await popup.Hide();

            if (popup is MonoBehaviour monoPopup)
            {
                if (!popup.IsCacheable)
                {
                    int hashKey = GetPopupHash(popup.PopupNameId);
                    DestroyPopupWithDelayAsync(hashKey, popup, monoPopup, DestroyDuration).Forget();
                }
                else
                {
                    monoPopup.transform.SetParent(_parentDown);
                    monoPopup.gameObject.SetActive(false);
                }
            }
           
            OnPopupClosed(popup);

            if (_activePopups.Count > 0)
            {
                var previousPopup = _activePopups.Peek();
                previousPopup.transform.SetParent(_parentUp);
                if (previousPopup is MonoBehaviour monoPrev)
                {
                    monoPrev.gameObject.SetActive(true);
                }
            }

            OnPopupChangedAction?.Invoke(_activePopups.Count > 0 ? _activePopups.Peek() : null);

            if (_activePopups.Count == 0 && !isTemporary)
            {
                await HandleFadeOutAsync(noFade: false);
            }
        }

        public void CloseAllPopups(bool noFade = false)
        {
            _temporaryPopupHistory.Clear();

            while (_activePopups.Count > 0)
            {
                var popup = _activePopups.Pop();
                popup.Hide().Forget();

                if (popup is MonoBehaviour monoPopup)
                {
                    if (monoPopup == null) continue;
                    if (!popup.IsCacheable)
                    {
                        int hashKey = GetPopupHash(popup.PopupNameId);
                        DestroyPopupWithDelayAsync(hashKey, popup, monoPopup, DestroyDuration).Forget();
                    }
                    else
                    {
                        if (_parentDown != null)
                        {
                            monoPopup.transform.SetParent(_parentDown);
                        }
                        monoPopup.gameObject.SetActive(false);
                    }
                }

                OnPopupClosed(popup);
            }

            OnPopupChangedAction?.Invoke(null);
            HandleFadeOutAsync(noFade).Forget();
        }

        public void BindSceneCanvas(Canvas newCanvas, Transform up, Transform down, CanvasGroup group)
        {
            CloseAllPopups(noFade: true);
            _canvas = newCanvas;
            _parentUp = up;
            _parentDown = down;
            _canvasGroup = group;
            SetActive(false);
        }

        public void UnbindSceneCanvas()
        {
            CloseAllPopups(noFade: true);
            _canvas = null;
            _parentUp = null;
            _parentDown = null;
            _canvasGroup = null;
        }

        private async UniTask HandleFadeOutAsync(bool noFade)
        {
            if (!noFade && _canvasGroup != null)
            {
                await FadeBackgroundAsync(fadeIn: false, _cts.Token);
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            if (_activePopups.Count > 0)
            {
                return; 
            }

            SetActive(false);
        }

        private async UniTask FadeBackgroundAsync(bool fadeIn, CancellationToken cancellationToken)
        {
            if (_canvasGroup == null) return;
            if (_fadeCts != null)
            {
                _fadeCts.Cancel();
                _fadeCts.Dispose();
            }
            _fadeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken linkedToken = _fadeCts.Token;

            float targetAlpha = fadeIn ? 1f : 0f;
            float startAlpha = _canvasGroup.alpha;
            float duration = 0.25f; 
            float elapsed = 0f;

            if (Mathf.Approximately(startAlpha, targetAlpha))
            {
                _canvasGroup.alpha = targetAlpha;
                return;
            }

            while (elapsed < duration)
            {
                if (linkedToken.IsCancellationRequested) return; 
        
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
            }

            _canvasGroup.alpha = targetAlpha;
        }

        private void SetBackgroundAlphaImmediately(float alpha)
        {
            if (_fadeCts != null)
            {
                _fadeCts.Cancel();
                _fadeCts.Dispose();
                _fadeCts = null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }

        void OnHideRootCanvas(IPopup requestPopup)
        {
            FadeBackgroundAsync(false, _cts.Token).Forget();
        }

        private async UniTask DestroyPopupWithDelayAsync(int hashKey, IPopup popup, MonoBehaviour monoPopup, float delaySeconds)
        {
            if (_pendingDestroyTokens.TryGetValue(hashKey, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
                _pendingDestroyTokens.Remove(hashKey);
            }

            var cts = new CancellationTokenSource();
            _pendingDestroyTokens[hashKey] = cts;

            try
            {
                monoPopup.transform.SetParent(_parentDown);
                monoPopup.gameObject.SetActive(false);
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: cts.Token);
           
                UnregisterPopupActions(popup);
                _popups.Remove(hashKey);
                _pendingDestroyTokens.Remove(hashKey);
        
                if (monoPopup != null)
                {
                    UnityEngine.Object.Destroy(monoPopup.gameObject);
                }
                _popupLoader.ReleasePrefab(popup.PopupNameId);
        
                cts.Dispose();
            }
            catch (OperationCanceledException)
            {
             
            }
        }

        public void ReleaseUnusedPopup(string popupNameId)
        {
            int hashKey = popupNameId.GetHashCode();
        
            if (_pendingDestroyTokens.TryGetValue(hashKey, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _pendingDestroyTokens.Remove(hashKey);
            }

            if (_popups.TryGetValue(hashKey, out IPopup popup))
            {
                UnregisterPopupActions(popup);
                _popups.Remove(hashKey);
      
                if (_popupLoader != null)
                {
                    _popupLoader.ReleasePrefab(popupNameId);
                }
      
                if (popup is MonoBehaviour monoPopup && monoPopup != null)
                {
                    UnityEngine.Object.Destroy(monoPopup.gameObject);
                }
                Debug.Log($"[PopupController] Đã giải phóng NGAY LẬP TỨC '{popupNameId}' khỏi bộ nhớ.");
            }
        }

        private void UnregisterPopupActions(IPopup popup)
        {
            if (popup == null) return;
            popup.OnClosed -= OnPopupClosed;
            popup.OnRequestClose -= OnPopupRequestedClose;
            popup.OnRequestCloseAll -= OnPopupRequestedCloseAll;
            popup.OnHideRootCanvas -= OnHideRootCanvas;
        }

        public bool HasActivePopups() => _activePopups.Count > 0;
        public void SetActive(bool active) => IsBackground = active;

        public void Dispose()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            
            if (_popupLoader != null)
            {
                _popupLoader.ReleaseAll();
            }
            foreach (var cts in _pendingDestroyTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }

            foreach (var popup in _popups.Values)
            {
                UnregisterPopupActions(popup);
            }

            _cts.Cancel();
            _cts.Dispose();
            _popups.Clear();
            
            if (_queueManager != null)
            {
                _queueManager.Dispose();
            }
            if (_fadeCts != null)
            {
                _fadeCts.Cancel();
                _fadeCts.Dispose();
            }

            OnPopupClosedAction = null;
            OnPopupChangedAction = null;
        }

        #region Implement IPopupQueueService
        public void Enqueue(PopupQueueRequest request) => _queueManager.Enqueue(request);
        public void EnqueueMultiple(IEnumerable<PopupQueueRequest> requests) => _queueManager.EnqueueMultiple(requests);
        public void ClearQueue() => _queueManager.ClearQueue();
        public void CancelQueue() => _queueManager.CancelQueue();
        #endregion
    }

    public class TemporaryPopupState
    {
        public IPopup TemporarilyClosedPopup { get; private set; }
        public int TriggerPopupHash { get; private set; }

        public TemporaryPopupState(IPopup closedPopup, int triggerHash)
        {
            TemporarilyClosedPopup = closedPopup;
            TriggerPopupHash = triggerHash;
        }
    }
}
