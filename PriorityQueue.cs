using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OpenSkillBot
{
    public class PriorityQueue<T> : ICollection<T> where T : IComparable
    {
        #region properties
        [JsonProperty]
        public bool IsDescending { get; private set; } = false;

        [JsonProperty]
        private List<T> Items { get; set; } = new List<T>();

        private int descendingConst => IsDescending ? -1 : 1;

        public int Count => this.Items.Count;

        public bool Empty => Count <= 1;

        public bool IsReadOnly => ((ICollection<T>)Items).IsReadOnly;
        #endregion

        public PriorityQueue(bool isDescending = false) {
            // fill first, but ignore if length is not zero (from serialization)
            if (this.Items.Count == 0)
                this.Items.Add(default);
            this.IsDescending = isDescending;
        }

        public PriorityQueue<T> Copy() {
            var q = new PriorityQueue<T>(this.IsDescending);
            foreach (var i in this.Items) {
                q.Items.Add(i);
            }
            return q;
        }

        public T Top() {
            return this[1];
        }

        public List<T> GetSorted() {
            var copy = Copy();
            var sorted = new List<T>();

            while (!copy.Empty) {
                sorted.Add(copy.Pop());
            }

            return sorted;
        }

        void swap(int i, int j) {
            if (i == j) return;

            T temp = this[j];
            this[j] = this[i];
            this[i] = temp;
        }

        public bool Insert(Func<T, bool> predicate, T v) {
            foreach (var i in this) {
                if (i == null) continue;
                if (predicate(i)) return false;
            }

            this.Items.Add(v);
            swim(this.Count - 1);

            return true;
        }

        public void Insert(T v) {
            this.Items.Add(v);
            swim(this.Count - 1);
        }

        void swim(int i) {
            while (i > 1 && descendingConst * this[i].CompareTo(this[i/2]) < 0) {
                swap(i, i/2);
                i /= 2;
            }
        }

        void sink(int i) {
            while (i*2 < this.Count) {
                int r = i*2;
                if (r+1 < this.Count && descendingConst * this[r+1].CompareTo(this[r]) < 0) ++r;
                if (descendingConst * this[i].CompareTo(this[r]) <= 0) break;
                swap(i, r);
                i = r;
            }
        }

        public T Pop() {
            T v = this[1];
            swap(1, this.Count - 1);
            var item = this[this.Count - 1];
            this.Items.RemoveAt(this.Count - 1);
            sink(1);
            return v;
        }

        public void Delete(int index) {
            // ref: http://www.mathcs.emory.edu/~cheung/Courses/171/Syllabus/9-BinTree/heap-delete.html
            swap(this.Count - 1, index);
            this.Items.RemoveAt(this.Count - 1);

            if (index == this.Count) return;
            swim(index);
            sink(index);
        }

        public bool Delete(Func<T, bool> predicate) {
            bool flag = false;
            for (int i = 1; i < this.Items.Count; ++i) {
                var item = this[i];
                if (predicate(item)) {
                    flag = true;
                    Delete(i);
                } 
            }
            return flag;
        }

        public bool Delete(T item) {
            var index = this.Items.IndexOf(item);
            if (index == -1) return false;

            Delete(index);

            return true;
        }

        public T this[int key] {
            get => this.Items[key];
            private set => this.Items[key] = value;
        }

        public static PriorityQueue<Q> FromList<Q>(List<Q> list, bool descending = false) where Q : IComparable {
            var q = new PriorityQueue<Q>(descending);
            foreach (var val in list) {
                q.Insert(val);
            }
            return q;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }

        public void Add(T item)
        {
            if (Items.Count == 1 && item == null) return;
            ((ICollection<T>)Items).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)Items).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)Items).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)Items).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)Items).Remove(item);
        }
    }
}