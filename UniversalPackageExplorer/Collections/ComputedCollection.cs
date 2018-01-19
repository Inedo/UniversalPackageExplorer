using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace UniversalPackageExplorer.Collections
{
    public sealed class ComputedCollection<TOuter, TInput, TOutput> : IReadOnlyCollection<TOutput>, ICollection<TOutput>, INotifyCollectionChanged
        where TOuter : ICollection<TInput>, INotifyCollectionChanged
    {
        private readonly TOuter outer;
        private readonly Func<TInput, TOutput> compute;

        internal ComputedCollection(TOuter outer, Func<TInput, TOutput> compute)
        {
            this.outer = outer;
            this.compute = compute;
            this.outer.CollectionChanged += Outer_CollectionChanged;
        }

        private void Outer_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems.Cast<TInput>().Select(compute).ToArray(), e.NewStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems.Cast<TInput>().Select(compute).ToArray(), e.OldStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems.Cast<TInput>().Select(compute).ToArray(), e.OldItems.Cast<TInput>().Select(compute).ToArray(), e.NewStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Move:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, e.OldItems.Cast<TInput>().Select(compute).ToArray(), e.NewStartingIndex, e.OldStartingIndex));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, e.NewItems.Cast<TInput>().Select(compute).ToArray()));
                    break;
            }
        }

        public int Count => outer.Count;
        public bool IsReadOnly => true;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Add(TOutput item) => new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(TOutput item) => outer.Select(compute).Contains(item);
        public void CopyTo(TOutput[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (var v in outer)
            {
                array[i] = compute(v);
                i++;
            }
        }
        public IEnumerator<TOutput> GetEnumerator() => outer.Select(compute).GetEnumerator();
        public bool Remove(TOutput item) => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
