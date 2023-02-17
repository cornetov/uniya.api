using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace Uniya.Core;

/// <summary>
/// Observable not null able collection with map by key (first property).
/// </summary>
public class XCollection<T, K> : ObservableCollection<T> where T : class
{
    // ------------------------------------------------------------------------------------
    #region ** primary object model

    private Dictionary<K, int> _cache = new Dictionary<K, int>();
    private List<T> _deleting = new List<T>();

    static PropertyInfo _keyProperty;

    static XCollection()
    {
        foreach (PropertyInfo pi in typeof(T).GetPublicProperties())
        {
            if (pi.PropertyType == typeof(K))
            {
                _keyProperty = pi;
                return;
            }
        }
        throw new KeyNotFoundException();
    }

    /// <summary>
    /// Gets object by key value.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <returns>Really object if found, otherwise <b>null</b>.</returns>
    public T GetBy(K key)
    {
        if (_cache.ContainsKey(key))
        {
            int idx = _cache[key];
            if (idx >= 0 && idx < Count)
                return this[idx];
        }
        return default;
    }

    /// <summary>Gets deleted collection.</summary>
    public ICollection<T> Deleting
    {
        get { return _deleting; }
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** override object model

    /// <summary>Removes all items from the collection.</summary>
    protected override void ClearItems()
    {
        foreach (var key in _cache.Keys)
        {
            int idx = _cache[key];
            if (idx >= 0 && idx < Count)
                _deleting.Add(this[idx]);
        }
        base.ClearItems();
        _cache.Clear();
    }
    /// <summary>Inserts an item into the collection at the specified index.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    protected override void InsertItem(int index, T item)
    {
        var key = (K)XProxy.GetValue(_keyProperty, item);
        _cache.Add(key, index);
        base.InsertItem(index, item);
    }
    /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
    protected override void MoveItem(int oldIndex, int newIndex)
    {
        var item = this[oldIndex];
        var key = (K)XProxy.GetValue(_keyProperty, item);
        base.MoveItem(oldIndex, newIndex);
        _cache[key] = newIndex;
    }

    /// <summary>Removes the item at the specified index of the collection.</summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    protected override void RemoveItem(int index)
    {
        var item = this[index];
        var key = (K)XProxy.GetValue(_keyProperty, item);
        _cache.Remove(key);
        base.RemoveItem(index);
        _deleting.Add(item);
    }
    /// <summary>Replaces the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="item">The new value for the element at the specified index.</param>
    protected override void SetItem(int index, T item)
    {
        // sanity old item
        var oldItem = this[index];
        if (item.Equals(oldItem)) return;

        // change cache
        var oldKey = (K)XProxy.GetValue(_keyProperty, oldItem);
        var key = (K)XProxy.GetValue(_keyProperty, item);
        if (key.Equals(oldKey))
        {
            // if equal key
            _cache[key] = index;
        }
        else
        {
            _cache.Remove(oldKey);
            _cache.Add(key, index);
        }
        _deleting.Add(oldItem);

        // immediate set
        base.SetItem(index, item);
    }

    #endregion
}
