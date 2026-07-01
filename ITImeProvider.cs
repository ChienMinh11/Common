using System;
using UnityEngine;

namespace ChieChie.Constracts
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}
