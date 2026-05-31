using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class PopupController : MonoBehaviour, IPopupService, IInitialisable
    {
        [Header("Registry & Config")] [SerializeField]
        private List<MonoBehaviour> _popupRegistryComponents = new List<MonoBehaviour>();

        [SerializeField] private string _resourcesPath = "Popups/";
        [SerializeField] private string _addressablePrefix = "Assets/Prefabs/Popups/";
        [SerializeField] private string _addressableSuffix = ".prefab";

        [Header("Fade Tween Config")] [SerializeField]
        private TweenUI _canvasTween;

        private Canvas _canvas;
        private Transform _parrentUp;
        private Transform _parrentDown;
        private CanvasGroup _canvasGroup;


        private readonly Dictionary<int, IPopup> popups = new Dictionary<int, IPopup>();
        private readonly Stack<IPopup> activePopups = new Stack<IPopup>();
        private readonly Stack<TemporaryPopupState> temporaryPopupHistory = new Stack<TemporaryPopupState>();

        private IEventService _eventService;
        private IObjectResolver _resolver;
        private IPopupLoader _popupLoader;
        private bool _isBackground;

        public CanvasGroup CanvasGroup => _canvasGroup;

        [Inject]
        private void Contructor(IEventService eventService, IObjectResolver resolver)
        {
            _eventService = eventService;
            _resolver = resolver;
        }

        public bool IsBackground
        {
            get => _isBackground;
            set
            {
                _isBackground = value;
                if (_parrentUp != null) _parrentUp.gameObject.SetActive(_isBackground);
            }
        }

        public bool IsInitialized { get; set; }

        public UniTask<bool> InitializeAsync(CancellationToken cancellationToken)
        {

            _popupLoader = new HybridPopupLoader(_popupRegistryComponents, _resourcesPath, _addressablePrefix,
                _addressableSuffix);

            SetActive(false);

            _eventService.Observe<IPopup, PopupEventType>(PopupEventType.OnPopUpClose)
                .Subscribe(OnPopupClosed).RegisterTo(this.destroyCancellationToken);

            _eventService.Observe<IPopup, PopupEventType>(PopupEventType.OnPopupRequestClose)
                .Subscribe(requestedPopup =>
                {
                    if (activePopups.Count > 0 && activePopups.Peek() == requestedPopup)
                    {
                        CloseLastPopup();
                    }
                }).RegisterTo(this.destroyCancellationToken);

            SceneManager.sceneUnloaded += OnSceneUnloaded;

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
            if (temporaryPopupHistory.Count > 0)
            {
                var nextRestoreState = temporaryPopupHistory.Peek();
                int closedPopupHash = GetPopupHash(closedPopup.PopupNameId);

                if (closedPopupHash == nextRestoreState.TriggerPopupHash)
                {
                    temporaryPopupHistory.Pop();
                    int restoreHash = GetPopupHash(nextRestoreState.TemporarilyClosedPopup.PopupNameId);
                    if (popups.TryGetValue(restoreHash, out var restorePopup))
                    {
                        ShowPopupByHashInternal(restorePopup, restoreHash).Forget();
                    }
                }
            }
        }

        public async UniTask<bool> ShowPopup(string popupNameId, string message = "", bool closeAndRestore = false)
        {
            if (activePopups.Count > 0 && activePopups.Peek().PopupNameId == popupNameId)
            {
                return false;
            }

            int hashKey = GetPopupHash(popupNameId);

            if (!popups.TryGetValue(hashKey, out IPopup targetPopup) || targetPopup == null)
            {
                //LoadingIndicator.Show();
                GameObject prefab = await _popupLoader.LoadPrefabAsync(popupNameId, this.destroyCancellationToken);
                // LoadingIndicator.Hide();
                if (prefab == null) return false;

                var prefabMono = prefab.GetComponent<MonoBehaviour>();
                if (prefabMono is IPopup)
                {
                    MonoBehaviour spawnedMono = _resolver.Instantiate(prefabMono, _parrentDown);
                    if (spawnedMono == null)
                    {
                        return false;
                    }

                    spawnedMono.gameObject.SetActive(false);
                    targetPopup = spawnedMono as IPopup;
                    popups.Add(hashKey, targetPopup);
                    targetPopup.Initialize(_eventService);
                }
                else
                {
                    return false;
                }
            }

            return await ShowPopupByHashInternal(targetPopup, hashKey, message, closeAndRestore);
        }

        public T GetPopup<T>(string popupNameId) where T : class, IPopup
        {
            int hashKey = GetPopupHash(popupNameId);
            if (popups.TryGetValue(hashKey, out IPopup popup))
            {
                return popup as T;
            }

            return null;
        }

        private async UniTask<bool> ShowPopupByHashInternal(IPopup targetPopup, int hashKey, string message = "",
            bool closeAndRestore = false)
        {
            if (activePopups.Count > 0 && activePopups.Peek() == targetPopup)
            {
                return false;
            }

            if (!targetPopup.CanShow())
                return false;
          
            bool isFirstPopup = activePopups.Count == 0;

            if (isFirstPopup)
            {
                SetActive(true);
                if (_canvasTween != null && _canvasGroup != null)
                {
                   _canvasTween.PlayShowAsync(this.destroyCancellationToken).Forget();
                }
            }
        
            if (closeAndRestore && activePopups.Count > 0)
            {
                IPopup currentTopPopup = activePopups.Peek();
                temporaryPopupHistory.Push(new TemporaryPopupState(currentTopPopup, hashKey));
                await CloseLastPopupInternal(isTemporary: true);
            }
        
            else if (activePopups.Count > 0)
            {
                var previousPopup = activePopups.Peek();
                previousPopup.transform.SetParent(_parrentDown);
                if (previousPopup is MonoBehaviour monoPrev) monoPrev.gameObject.SetActive(false);
            }

            targetPopup.transform.SetParent(_parrentUp);

            if (targetPopup is MonoBehaviour monoPopup)
            {
                monoPopup.gameObject.SetActive(true);
            }

            if (targetPopup is IPopupWithData popupWithData)
            {
                popupWithData.SetMessage(message);
            }
         
            activePopups.Push(targetPopup);

            SetActive(true);

            await targetPopup.Show();

            _eventService.Publish<IPopup, PopupEventType>(PopupEventType.OnPopupChange, targetPopup);
            return true;
        }

        public void CloseLastPopup() => CloseLastPopupInternal(false).Forget();

        private async UniTask CloseLastPopupInternal(bool isTemporary)
        {
            if (activePopups.Count <= 0) return;

            var popup = activePopups.Pop();
            await popup.Hide();

            if (popup is MonoBehaviour monoPopup)
            {
                if (!popup.IsCacheable)
                {
                    int hashKey = GetPopupHash(popup.PopupNameId);
                    popups.Remove(hashKey);
                    Destroy(monoPopup.gameObject);
                    _popupLoader.ReleasePrefab(popup.PopupNameId);
                }
                else
                {
                    monoPopup.transform.SetParent(this.transform);
                    monoPopup.gameObject.SetActive(false);
                }
            }

            _eventService.Publish<IPopup, PopupEventType>(PopupEventType.OnPopUpClose, popup);

            if (activePopups.Count > 0)
            {
                var previousPopup = activePopups.Peek();
                previousPopup.transform.SetParent(_parrentUp);
                if (previousPopup is MonoBehaviour monoPrev)
                {
                    monoPrev.gameObject.SetActive(true);
                }
            }

            _eventService.Publish<IPopup, PopupEventType>(PopupEventType.OnPopupChange,
                activePopups.Count > 0 ? activePopups.Peek() : null);
         
            if (activePopups.Count == 0 && !isTemporary)
            {
                await HandleFadeOutAsync(noFade: false);
            }
        }

        public void CloseAllPopups(bool noFade = false)
        {
            temporaryPopupHistory.Clear();

            while (activePopups.Count > 0)
            {
                var popup = activePopups.Pop();
                popup.Hide().Forget();

                if (popup is MonoBehaviour monoPopup)
                {
                    if (monoPopup == null) continue;
                    if (!popup.IsCacheable)
                    {
                        int hashKey = GetPopupHash(popup.PopupNameId);
                        popups.Remove(hashKey);
                        Destroy(monoPopup.gameObject);
                        _popupLoader.ReleasePrefab(popup.PopupNameId);
                    }
                    else
                    {
                        if (this != null && this.gameObject != null)
                        {
                            monoPopup.transform.SetParent(this.transform);
                        }
                        monoPopup.gameObject.SetActive(false);
                    }
                }

                _eventService.Publish<IPopup, PopupEventType>(PopupEventType.OnPopUpClose, popup);
            }

            _eventService.Publish<IPopup, PopupEventType>(PopupEventType.OnPopupChange, null);

            HandleFadeOutAsync(noFade).Forget();
        }

        public void BindSceneCanvas(Canvas newCanvas, Transform up, Transform down, CanvasGroup group)
        {
            CloseAllPopups(noFade: true);
            _canvas = newCanvas;
            _parrentUp = up;
            _parrentDown = down;
            _canvasGroup = group;
            if (_canvasGroup != null && _canvasTween != null)
            {
                _canvasTween.SetupCanvasGroup(_canvasGroup);
            }

            SetActive(false);
        }

        public void UnbindSceneCanvas()
        {
            if(this == null) return;
            CloseAllPopups(noFade: true);
            _canvas = null;
            _parrentUp = null;
            _parrentDown = null;
            _canvasGroup = null;
        }

        private async UniTask HandleFadeOutAsync(bool noFade)
        {
            if (!noFade && _canvasTween != null && _canvasGroup != null)
            {
                await _canvasTween.PlayHideAsync(this.destroyCancellationToken);
            }

            SetActive(false);
        }

        public bool HasActivePopups() => activePopups.Count > 0;
        public void SetActive(bool active) => IsBackground = active;

        private void OnDestroy()
        {
            if (_popupLoader != null)
            {
                _popupLoader.ReleaseAll();
            }

            popups.Clear();
        }
    }


    public enum PopupEventType
    {
        OnPopUpClose,
        OnPopupChange,
        OnPopupRequestClose
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