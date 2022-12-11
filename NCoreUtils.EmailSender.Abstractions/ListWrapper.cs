using System;
using System.Collections.Generic;
using System.Linq;

namespace NCoreUtils;

public struct ListWrapper<T>
{
    private List<T>? _list;

    public T this[int index]
    {
        get => _list is null ? throw new IndexOutOfRangeException() : _list[index];
        set
        {
            if (_list is null)
            {
                throw new IndexOutOfRangeException();
            }
            _list[index] = value;
        }
    }

    public int Count => _list is null ? 0 : _list.Count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        _list ??= new List<T>();
        _list.Add(item);
    }

    public void Clear()
        => _list?.Clear();

    public bool Contains(T item)
        => _list is not null && _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _list?.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator()
        => _list?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();

    public int IndexOf(T item)
        => _list is null ? -1 : _list.IndexOf(item);

    public void Insert(int index, T item)
    {
        _list ??= new List<T>();
        _list.Insert(index, item);
    }

    public bool Remove(T item)
        => _list is not null && _list.Remove(item);

    public void RemoveAt(int index)
        => _list?.RemoveAt(index);

    public T[] ToArray()
        => _list is null ? Array.Empty<T>() : _list.ToArray();
}