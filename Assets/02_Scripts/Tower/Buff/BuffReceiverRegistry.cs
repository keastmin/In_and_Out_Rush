using System.Collections.Generic;
using UnityEngine;

public static class BuffReceiverRegistry
{
    public readonly struct Entry
    {
        public readonly IBuffReceiver Receiver;
        public readonly Transform Transform;
        public readonly BuffTargetType TargetType;

        public Entry(IBuffReceiver receiver, Transform transform, BuffTargetType targetType)
        {
            Receiver = receiver;
            Transform = transform;
            TargetType = targetType;
        }
    }

    private static readonly Dictionary<IBuffReceiver, Entry> _entries = new();
    private static readonly List<Entry> _snapshot = new();

    public static IReadOnlyList<Entry> Snapshot => _snapshot;

    public static void Register(IBuffReceiver receiver, Transform transform, BuffTargetType targetType)
    {
        if (receiver == null || transform == null || targetType == BuffTargetType.None) return;

        _entries[receiver] = new Entry(receiver, transform, targetType);
        RebuildSnapshot();
    }

    public static void Unregister(IBuffReceiver receiver)
    {
        if (receiver == null) return;
        if (_entries.Remove(receiver))
        {
            RebuildSnapshot();
        }
    }

    private static void RebuildSnapshot()
    {
        _snapshot.Clear();
        foreach (var pair in _entries)
        {
            if (pair.Value.Receiver == null || pair.Value.Transform == null) continue;
            _snapshot.Add(pair.Value);
        }
    }
}

