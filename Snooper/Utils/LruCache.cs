using System;
using System.Collections.Generic;

namespace Snooper.Utils;

internal class LruCache<TKey, TValue> where TKey: notnull {
	private readonly int capacity;
	private readonly Dictionary<TKey, TValue> data = new();
    private readonly LinkedList<TKey> lruList = new();

	internal LruCache(int capacity)
	{
		this.capacity = capacity;
	}

	internal TValue? this[TKey key]
	{
		get
		{
			return data.GetValueOrDefault(key);
		}
	}

	internal TValue GetOrLoad(TKey key, Func<TKey, TValue> valueLoader)
	{
		data.TryGetValue(key, out TValue? value);

        if (value == null)
        {
            // Evict earliest sender if necessary
            if (data.Count == capacity)
            {
                data.Remove(lruList.First!.Value);
                lruList.RemoveFirst();
            }

            value = valueLoader(key);
            data[key] = value;
        }
        else
        {
            lruList.Remove(key);
        }

		lruList.AddLast(key);
		return value;
	}

	internal void Set(TKey key, TValue value)
	{
		if (data.ContainsKey(key))
		{
            data[key] = value;
		}
		else
		{
			GetOrLoad(key, _ => value);
		}
	}

	internal void Clear()
	{
		data.Clear();
		lruList.Clear();
	}

	internal void Clear(Action<TValue> disposer)
	{
		foreach (var value in data.Values)
		{
			disposer(value);
		}

		Clear();
	}
}
