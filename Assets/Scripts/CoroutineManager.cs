using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public readonly struct CoroutineHandle
{
    readonly string _key;
    readonly CoroutineManager _manager;

    internal CoroutineHandle(string key, CoroutineManager manager)
    {
        _key = key;
        _manager = manager;
    }

    public void Stop() => _manager?.StopByKey(_key);
}

public class CoroutineManager : SingletonMono<CoroutineManager>
{
    readonly Dictionary<string, Coroutine> _coroutines = new();
    static int _autoId = 0;

    public CoroutineHandle StartWithKey(string key, IEnumerator routine)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("Coroutine key is null or empty! Using auto ID.");
            key = "auto_" + _autoId++;
        }

        StopByKey(key);
        var coroutine = StartCoroutine(routine);
        _coroutines[key] = coroutine;
        return new CoroutineHandle(key, this);
    }

    public void StopByKey(string key)
    {
        if (_coroutines.TryGetValue(key, out var routine))
        {
            if (routine != null)
                StopCoroutine(routine);
            _coroutines.Remove(key);
        }
    }

    public void StopAllManaged()
    {
        foreach (var routine in _coroutines.Values)
        {
            if (routine != null)
                StopCoroutine(routine);
        }
        _coroutines.Clear();
    }
}