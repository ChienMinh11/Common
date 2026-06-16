using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace ChieChie.Core
{
    public class ServiceOrderedInitialiser : IAsyncStartable
    {
        private readonly List<List<IServiceInitialisable>> _layers;
        private readonly bool _showLog; 

        public ServiceOrderedInitialiser(List<List<IServiceInitialisable>> layers, bool showLog = true) 
        {
            _layers = layers;
            _showLog = showLog;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken, Action<float> onProgressUpdate = null)
        {
            ShowLog("<color=teal>[OrderedInitialiser]</color> Bắt đầu chuỗi khởi tạo...");
            int totalLayers = _layers.Count;

            for (int i = 0; i < totalLayers; i++)
            {
                var layer = _layers[i];
                if (layer == null || layer.Count == 0) continue;
                ShowLog($"[OrderedInitialiser] ⏳ Đang khởi tạo Layer {i + 1}/{totalLayers}...");

                var tasks = layer.Select(async service =>
                {
                    string serviceName = service.GetType().Name;
                    try
                    {
                        bool success = await service.InitializeAsync(cancellationToken);
                        if (!success) throw new Exception($"Service {serviceName} báo khởi tạo thất bại.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[OrderedInitialiser] ❌ LỖI tại {serviceName}: {ex.Message}");
                        throw;
                    }
                });
            
                await UniTask.WhenAll(tasks);
              
                if (onProgressUpdate != null)
                {
                    float startProgress = 0.25f; 
                    float remainingProgress = 1.0f - startProgress;
                    float currentLayerProgress = startProgress + (remainingProgress * ((float)(i + 1) / totalLayers));
                    currentLayerProgress = Mathf.Clamp(currentLayerProgress, startProgress, 0.95f); 
                    
                    onProgressUpdate.Invoke(currentLayerProgress);
                }
            }
            ShowLog("<color=green>[OrderedInitialiser]</color> Toàn bộ các Layer khởi tạo thành công!");
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            await StartAsync(cancellationToken, null);
        }

        private void ShowLog(string message)
        {
            if (_showLog) Debug.Log(message);
        }
    }
}