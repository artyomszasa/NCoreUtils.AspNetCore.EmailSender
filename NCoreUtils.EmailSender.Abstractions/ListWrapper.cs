using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils;

public struct ListWrapper<T>
{
    private List<T>? _list;

    public readonly T this[int index]
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

    public readonly int Count => _list is null ? 0 : _list.Count;

    public readonly bool IsReadOnly => false;

    public void Add(T item)
        => (_list ??= []).Add(item);

    public readonly void Clear()
        => _list?.Clear();

    public readonly bool Contains(T item)
        => _list is not null && _list.Contains(item);

    public readonly void CopyTo(T[] array, int arrayIndex)
        => _list?.CopyTo(array, arrayIndex);

    public readonly IEnumerator<T> GetEnumerator()
        => _list?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();

    public readonly int IndexOf(T item)
        => _list is null ? -1 : _list.IndexOf(item);

    public void Insert(int index, T item)
        => (_list ??= []).Insert(index, item);

    public readonly bool Remove(T item)
        => _list is not null && _list.Remove(item);

    public readonly void RemoveAt(int index)
        => _list?.RemoveAt(index);

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "Array is used to allow potential optimizations.")]
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "Array is used to allow potential optimizations.")]
    public readonly T[] ToArray()
        => _list is null ? Array.Empty<T>() : _list.ToArray();
}