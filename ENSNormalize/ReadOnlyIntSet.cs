#define USE_SET
using System.Collections;
using System.Collections.Generic;

namespace ADRaffy.ENSNormalize
{
    public class ReadOnlyIntSet : IReadOnlyCollection<int>
    {
        static public readonly ReadOnlyIntSet EMPTY = new(new int[0]);

        private readonly HashSet<int> Set;
        public int Count { get => Set.Count; }
        public ReadOnlyIntSet(IEnumerable<int> v)
        {
            Set = new(v);
        }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => Set.GetEnumerator(); // ew
        IEnumerator IEnumerable.GetEnumerator() => Set.GetEnumerator();
        public bool Contains(int x) => Set.Contains(x);

        // note: uses less memory but 10% slower
        /*
        private readonly int[] Sorted;
        public int this[int index] { get => Sorted[index]; }
        public int Count {  get => Sorted.Length; }
        public ReadOnlyIntSet(IEnumerable<int> v) {
            Sorted = v.ToArray();
            Array.Sort(Sorted);
        }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)Sorted).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sorted.GetEnumerator();
        public bool Contains(int x) => Array.BinarySearch(Sorted, x) >= 0;
        */
    }
}
