using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct CoroutineHandle
{
    readonly string _key;
    readonly Coroutine _routine;

    internal CoroutineHandle(string key, Coroutine routine)
    {
        _key = key;
        _routine = routine;
    }

    public bool IsValid => _routine != null;

    public void Stop()
    {
        CoroutineManager.Instance?.StopCoroutineByKey(_key);
    }
}

public class CoroutineManager : SingletonMono<CoroutineManager>
{
    readonly Dictionary<string, Coroutine> _coroutines = new();

    public CoroutineHandle StartUniqueCoroutine(string key, IEnumerator routine)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("Coroutine key is null or empty!");
            key = "unnamed_" + Time.time;
        }

        StopCoroutineByKey(key);
        var coroutine = StartCoroutine(routine);
        _coroutines[key] = coroutine;
        return new CoroutineHandle(key, coroutine);
    }

    public CoroutineHandle StartManagedCoroutine(IEnumerator routine)
    {
        var key = "auto_" + GetHashCode() + "_" + Time.time;
        var coroutine = StartCoroutine(routine);
        _coroutines[key] = coroutine;
        return new CoroutineHandle(key, coroutine);
    }

    public void StopCoroutineByKey(string key)
    {
        if (_coroutines.TryGetValue(key, out Coroutine routine))
        {
            StopCoroutine(routine);
            _coroutines.Remove(key);
        }
    }

    public void StopAllManagedCoroutines()
    {
        foreach (var routine in _coroutines.Values)
        {
            StopCoroutine(routine);
        }
        _coroutines.Clear();
    }

    public int CoroutineCount => _coroutines.Count;
}