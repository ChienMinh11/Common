using System;
using R3;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ChieChie.Core
{
    public interface IEventService
    {
        void PublishVector3<TEnum>(TEnum eventType, UnityEngine.Vector3 value) where TEnum : struct, Enum;
        void PublishVector3<TEnum>(TEnum eventType, object source, UnityEngine.Vector3 value) where TEnum : struct, Enum;

        Observable<UnityEngine.Vector3> ObserveVector3<TEnum>(TEnum eventType) where TEnum : struct, Enum;
        Observable<UnityEngine.Vector3> ObserveVector3<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum;
        void Publish<T, TEnum>(TEnum eventType, T value) where TEnum : struct, Enum;
        void PublishEvent<TEnum>(TEnum eventType) where TEnum : struct, Enum;
        Observable<T> Observe<T, TEnum>(TEnum eventType) where TEnum : struct, Enum;
        Observable<Unit> ObserveEvent<TEnum>(TEnum eventType) where TEnum : struct, Enum;

        ReactiveProperty<T> GetReactiveProperty<T, TEnum>(TEnum propertyType, T initialValue = default)
            where TEnum : struct, Enum;

        void Publish<T, TEnum>(TEnum eventType, object source, T value) where TEnum : struct, Enum;
        void PublishEvent<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum;
        Observable<T> Observe<T, TEnum>(TEnum eventType, object source) where TEnum : struct, Enum;
        Observable<Unit> ObserveEvent<TEnum>(TEnum eventType, object source) where TEnum : struct, Enum;

        ReactiveProperty<T> GetReactiveProperty<T, TEnum>(TEnum propertyType, object source, T initialValue = default)
            where TEnum : struct, Enum;

        void ClearDeadReferences();
    }

    public struct EventKey : IEquatable<EventKey>
    {
        private readonly int _enumHash;
        private readonly WeakReference _sourceRef;
        private readonly int _sourceHash;
        private readonly Type _dataType; // THÊM: Lưu kiểu dữ liệu để phân biệt

        public EventKey(int enumHash, object source, Type dataType = null)
        {
            _enumHash = enumHash;
            _dataType = dataType; // Kiểu dữ liệu (ví dụ: typeof(int))
            if (source != null)
            {
                _sourceRef = new WeakReference(source);
                _sourceHash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(source);
            }
            else
            {
                _sourceRef = null;
                _sourceHash = 0;
            }
        }

        public bool IsDead
        {
            get
            {
                if (_sourceRef == null) return false;
                var target = _sourceRef.Target;
                if (target == null) return true;
                if (target is UnityEngine.Object unityObj) return unityObj == null;
                return false;
            }
        }

        public bool Equals(EventKey other)
        {
            // Kiểm tra kiểu dữ liệu trước
            if (_dataType != other._dataType) return false;
            if (_enumHash != other._enumHash) return false;
            if (_sourceRef == null && other._sourceRef == null) return true;
            if (_sourceRef == null || other._sourceRef == null) return false;

            var targetSelf = _sourceRef.Target;
            var targetOther = other._sourceRef.Target;

            bool selfNull = targetSelf == null || (targetSelf is UnityEngine.Object uSelf && uSelf == null);
            bool otherNull = targetOther == null || (targetOther is UnityEngine.Object uOther && uOther == null);

            if (selfNull && otherNull) return true;
            if (selfNull || otherNull) return false;

            return ReferenceEquals(targetSelf, targetOther);
        }

        public override bool Equals(object obj) => obj is EventKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _enumHash;
                hash = hash * 23 + _sourceHash;
                if (_dataType != null) hash = hash * 23 + _dataType.GetHashCode(); // Thêm vào hash code
                return hash;
            }
        }
    }
}