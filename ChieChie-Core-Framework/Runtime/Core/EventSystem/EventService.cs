using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using R3;
using UnityEngine;

namespace ChieChie.Core
{
    public class EventService : IEventService
    {
        private readonly Dictionary<EventKey, Subject<object>> _subjects = new();
        private readonly Dictionary<EventKey, Subject<Unit>> _unitSubjects = new();
        private readonly Dictionary<EventKey, Subject<Vector3>> _vector3Subjects = new();
        private readonly Dictionary<EventKey, IDisposable> _reactiveProperties = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEnumHash<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            return EqualityComparer<TEnum>.Default.GetHashCode(value);
        }
        private Subject<Unit> GetUnitSubject<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
            var key = new EventKey(GetEnumHash(eventType), source);
            if (!_unitSubjects.TryGetValue(key, out var subject))
            {
                subject = new Subject<Unit>();
                _unitSubjects[key] = subject;
            }
            return subject;
        }

        private Subject<object> GetSubject<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
            var key = new EventKey(GetEnumHash(eventType), source);
            if (!_subjects.TryGetValue(key, out var subject))
            {
                subject = new Subject<object>();
                _subjects[key] = subject;
            }
            
            return subject;
        }
        private Subject<Vector3> GetVector3Subject<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
            var key = new EventKey(GetEnumHash(eventType), source);
            if (!_vector3Subjects.TryGetValue(key, out var subject))
            {
                subject = new Subject<Vector3>();
                _vector3Subjects[key] = subject;
            }
            return subject;
        }

        #region Global Scope Forwarders
        public void Publish<T, TEnum>(TEnum eventType, T value) where TEnum : struct, Enum => Publish(eventType, null, value);
        public void PublishEvent<TEnum>(TEnum eventType) where TEnum : struct, Enum => PublishEvent(eventType, null);
        public Observable<T> Observe<T, TEnum>(TEnum eventType) where TEnum : struct, Enum => Observe<T, TEnum>(eventType, null);
        public Observable<Unit> ObserveEvent<TEnum>(TEnum eventType) where TEnum : struct, Enum => ObserveEvent(eventType, null);
        public ReactiveProperty<T> GetReactiveProperty<T, TEnum>(TEnum propertyType, T initialValue = default) where TEnum : struct, Enum => GetReactiveProperty<T, TEnum>(propertyType, null, initialValue);
        #endregion

        #region Core Implementation
        public void Publish<T, TEnum>(TEnum eventType, object source, T value) where TEnum : struct, Enum
        {
            var key = new EventKey(GetEnumHash(eventType), source);
    
            if (_subjects.TryGetValue(key, out var subject))
            {
                subject.OnNext(value); 
            }
    
         
        }

        public void PublishEvent<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
            var key = new EventKey(GetEnumHash(eventType), source);
            if (_unitSubjects.TryGetValue(key, out var subject))
            {
                subject.OnNext(Unit.Default); 
            }
        }
        public void PublishVector3<TEnum>(TEnum eventType, Vector3 value) where TEnum : struct, Enum 
            => PublishVector3(eventType, null, value);

        public void PublishVector3<TEnum>(TEnum eventType, object source, Vector3 value) where TEnum : struct, Enum
        {
            var key = new EventKey(GetEnumHash(eventType), source);
            if (_vector3Subjects.TryGetValue(key, out var subject))
            {
                subject.OnNext(value);
            }
        }
        
        public Observable<Vector3> ObserveVector3<TEnum>(TEnum eventType) where TEnum : struct, Enum 
            => ObserveVector3(eventType, null);

        public Observable<Vector3> ObserveVector3<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
           
            return GetVector3Subject(eventType, source); 
        }

        public Observable<T> Observe<T, TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
           
            if (typeof(T) == typeof(UnityEngine.Vector3))
            {
                Debug.LogWarning($"[RxEventManager] Hãy dùng 'ObserveVector3' thay vì 'Observe<Vector3>' cho sự kiện {eventType} để tránh Boxing gây rác bộ nhớ!");
            }
            else if (typeof(T) == typeof(R3.Unit))
            {
                Debug.LogWarning($"[RxEventManager] Hãy dùng 'ObserveEvent' thay vì 'Observe<Unit>' cho sự kiện {eventType} để đạt hiệu năng tối đa!");
            }
#endif

            return GetSubject(eventType, source)
                .Where(x => x is T)
                .Select(x => (T)x);
        }

        public Observable<Unit> ObserveEvent<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum
        {
            return GetUnitSubject(eventType, source);
        }

        public ReactiveProperty<T> GetReactiveProperty<T, TEnum>(TEnum propertyType, object source, T initialValue = default) where TEnum : struct, Enum
        {
           
            var key = new EventKey(GetEnumHash(propertyType), source, typeof(T));

            if (_reactiveProperties.TryGetValue(key, out var existing))
            {
                return (ReactiveProperty<T>)existing;
            }

            var newProp = new ReactiveProperty<T>(initialValue);
            _reactiveProperties[key] = newProp;
    
            return newProp;
        }
        #endregion

        #region Cleanup Logic

        public void ClearDeadReferences()
        {
            // Tối ưu hóa: Dùng một list static tạm thời hoặc list tái sử dụng để tránh tạo mới List liên tục gây rác
            // Ở đây viết dạng pooling/clear nhanh:
            _deadKeysCache.Clear();

            foreach (var kvp in _subjects)
            {
                if (kvp.Key.IsDead) _deadKeysCache.Add(kvp.Key);
            }
            for (int i = 0; i < _deadKeysCache.Count; i++)
            {
                var key = _deadKeysCache[i];
                _subjects[key].Dispose();
                _subjects.Remove(key);
            }

            _deadKeysCache.Clear();

            foreach (var kvp in _reactiveProperties)
            {
                if (kvp.Key.IsDead) _deadKeysCache.Add(kvp.Key);
            }
            for (int i = 0; i < _deadKeysCache.Count; i++)
            {
                var key = _deadKeysCache[i];
                _reactiveProperties[key].Dispose();
                _reactiveProperties.Remove(key);
            }
            
            _deadKeysCache.Clear();
        }
     
        private readonly List<EventKey> _deadKeysCache = new();
        #endregion
    }
}