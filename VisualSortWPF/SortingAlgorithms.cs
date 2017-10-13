using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VisualSortWPF
{
    public class ActiveRangeChangeArgs<T> : EventArgs
    {
        public ActiveRangeChangeArgs(T[] items)
        {
            Items = items;
        }
        public T[] Items { get; set; }
    }

    public class ItemActionArgs<T> : EventArgs
    {
        public ItemActionArgs(T item, int itemIndex)
        {
            Item = item;
            ItemIndex = itemIndex;
        }
        public int ItemIndex { get; set; }
        public T Item { get; set; }
    }

    public class SortActionArgs<T> : EventArgs
    {
        public SortActionArgs(T item1, int item1Index, T item2, int item2Index)
        {
            Item1 = item1;
            Item2 = item2;
            Item1Index = item1Index;
            Item2Index = item2Index;
        }
        public int Item1Index { get; set; }
        public T Item1 { get; set; }
        public int Item2Index { get; set; }
        public T Item2 { get; set; }
    }

    public abstract class SortingAlgorithm<T> where T : IComparable<T>
    {
        private static int _Delay = 50;
        private static int _DelaySpins;
        public static int Delay
        {
            get
            {
                return _Delay;
            }
            set
            {
                _Delay = value;
                int n = 100 - _Delay;
                _DelaySpins = 9200 * n * n + 4900 * n;
            }
        }

        protected bool _Cancel = false;

        protected T[] _Array;

        public event EventHandler<EventArgs> Complete;
        public event EventHandler<ActiveRangeChangeArgs<int>> ActiveRangeChange;
        public event EventHandler<SortActionArgs<T>> ItemsSwapped;
        public event EventHandler<SortActionArgs<T>> ItemsCompared;
        public event EventHandler<ItemActionArgs<T>> ItemUpdated;

        public abstract string Name { get; }

        protected virtual void OnComplete()
        {
            if (_Cancel) return;
            EventHandler<EventArgs> handler = Complete;
            if (handler != null)
                handler(this, new EventArgs());
        }

        protected virtual void OnActiveRangeChange(params int[] indexes)
        {
            if (_Cancel) return;
            EventHandler<ActiveRangeChangeArgs<int>> handler = ActiveRangeChange;
            if (handler != null)
                handler(this, new ActiveRangeChangeArgs<int>(indexes));
        }

        protected virtual void OnSwapped(T item1, int item1Index, T item2, int item2Index)
        {
            if (_Cancel) return;
            EventHandler<SortActionArgs<T>> handler = ItemsSwapped;
            if (handler != null)
                handler(this, new SortActionArgs<T>(item1, item1Index, item2, item2Index));
        }

        protected virtual void OnCompared(T item1, int item1Index, T item2, int item2Index)
        {
            if (_Cancel) return;
            EventHandler<SortActionArgs<T>> handler = ItemsCompared;
            if (handler != null)
                handler(this, new SortActionArgs<T>(item1, item1Index, item2, item2Index));
        }

        protected virtual void OnUpdated(T item, int itemIndex)
        {
            if (_Cancel) return;
            EventHandler<ItemActionArgs<T>> handler = ItemUpdated;
            if (handler != null)
                handler(this, new ItemActionArgs<T>(item, itemIndex));
        }

        public void Cancel()
        {
            _Cancel = true;
        }

        public bool CancelCheck()
        {
            if (_Cancel)
            {
                _Cancel = false;
                return true;
            }
            return false;
        }

        protected int[] CreateArray(int first, int last)
        {
            int[] a = new int[last - first + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = first + i;
            }
            return a;
        }

        private void SpinWait()
        {
            Thread.SpinWait(_DelaySpins);
        }

        protected void Swap(int left, int right)
        {
            SpinWait();

            T temp = _Array[left];
            _Array[left] = _Array[right];
            _Array[right] = temp;

            OnSwapped(_Array[left], left, _Array[right], right);
        }

        protected int Compare(int left, int right)
        {
            SpinWait();

            OnCompared(_Array[left], left, _Array[right], right);

            return _Array[left].CompareTo(_Array[right]);
        }

        protected int Compare(T[] leftArray, int leftIndex, T[] rightArray, int rightIndex, int offset)
        {
            SpinWait();

            OnCompared(leftArray[leftIndex], leftIndex + offset, rightArray[rightIndex], leftArray.Length + offset + rightIndex);

            return leftArray[leftIndex].CompareTo(rightArray[rightIndex]);
        }

        protected void Update(int index, T value)
        {
            SpinWait();

            _Array[index] = value;

            OnUpdated(value, index);
        }

        protected void Update(T[] array, int index, T value, int offset)
        {
            SpinWait();

            array[index] = value;

            OnUpdated(value, index + offset);
        }

        public void Sort(T[] array)
        {
            _Array = array;
            ThreadPool.QueueUserWorkItem((object s) =>
            { InternalSort(array); });
        }

        protected abstract void InternalSort(T[] array);

        public static SortingAlgorithm<T> FromName(string name)
        {
            switch (name.ToUpperInvariant())
            {
                case "BUBBLE SORT": return new BubbleSort<T>();
                case "COCKTAIL SORT": return new CocktailSort<T>();
                case "ODD-EVEN SORT": return new OddEvenSort<T>();
                case "COMB SORT": return new CombSort<T>();
                case "GNOME SORT": return new GnomeSort<T>();
                case "QUICK SORT": return new QuickSort<T>();
                //case "3-WAY QUICK SORT": return new QuickSort3<T>();
                case "SELECTION SORT": return new SelectionSort<T>();
                case "SELECTION COCKTAIL SORT": return new SelectionCocktailSort<T>();
                case "HEAP SORT": return new HeapSort<T>();
                case "INSERTION SORT": return new InsertionSort<T>();
                case "SHELL SORT": return new ShellSort<T>();
                case "MERGE SORT": return new MergeSort<T>();
                case "STRAND SORT": return new StrandSort<T>();
                case "STOOGE SORT": return new StoogeSort<T>();
            }
            return new BubbleSort<T>();
        }
    }

    public class BubbleSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Bubble Sort"; } }

        protected override void InternalSort(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                bool swapped = false;
                for (int j = array.Length - 1; j > i; j--)
                {
                    if (CancelCheck()) return;
                    if (Compare(j, j - 1) < 0)
                    {
                        Swap(j, j - 1);
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }
            OnComplete();
        }
    }
    
    public class CocktailSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Cocktail Sort"; } }

        protected override void InternalSort(T[] array)
        {
            int begin = -1;
            int end = array.Length - 2;
            bool swapped = true;
            while (swapped)
            {
                if (CancelCheck()) return;
                swapped = false;
                begin++;
                for (int i = begin; i <= end; i++)
                {
                    if (CancelCheck()) return;
                    if (Compare(i, i + 1) > 0)
                    {
                        Swap(i, i + 1);
                        swapped = true;
                    }
                }
                if (!swapped)
                    break;
                swapped = false;
                end--;
                for (int i = end; i >= begin; i--)
                {
                    if (CancelCheck()) return;
                    if (Compare(i, i + 1) > 0)
                    {
                        Swap(i, i + 1);
                        swapped = true;
                    }
                }
            }
            OnComplete();
        }
    }
    
    public class OddEvenSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Odd-Even Sort"; } }

        protected override void InternalSort(T[] array)
        {
            bool sorted = false;
            while (!sorted)
            {
                if (CancelCheck()) return;
                sorted = true;
                for (int i = 1; i < array.Length - 1; i += 2)
                {
                    if (CancelCheck()) return;
                    if (Compare(i, i + 1) > 0)
                    {
                        Swap(i, i + 1);
                        sorted = false;
                    }
                }
                for (int i = 0; i < array.Length - 1; i += 2)
                {
                    if (CancelCheck()) return;
                    if (Compare(i, i + 1) > 0)
                    {
                        Swap(i, i + 1);
                        sorted = false;
                    }
                }
            }
            OnComplete();
        }
    }
    
    public class CombSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Comb Sort"; } }

        protected override void InternalSort(T[] array)
        {
            double shrinkFactor = 1.247330950103979;
            int gap = array.Length;
            bool swapped = false;
            while (gap > 1 || !swapped)
            {
                if (CancelCheck()) return;
                if (gap > 1)
                    gap = (int)(gap / shrinkFactor);
                int i = 0;
                swapped = false;
                while (gap + i < array.Length)
                {
                    if (CancelCheck()) return;
                    if (Compare(i, i + gap) > 0)
                    {
                        Swap(i, i + gap);
                        swapped = true;
                    }
                    i++;
                }
            }
            OnComplete();
        }
    }
    
    public class GnomeSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Gnome Sort"; } }

        protected override void InternalSort(T[] array)
        {
            int p = 0;
            while (p < array.Length)
            {
                if (CancelCheck()) return;
                if (p == 0 || Compare(p, p - 1) >= 0)
                {
                    p++;
                }
                else
                {
                    if (CancelCheck()) return;
                    Swap(p, p - 1);
                    p--;
                }
            }
            OnComplete();
        }
    }

    public class QuickSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Quick Sort"; } }

        protected override void InternalSort(T[] array)
        {
            InternalSort(0, array.Length - 1);
            if (CancelCheck()) return;
            OnComplete();
        }

        private void InternalSort(int left, int right)
        {
            if (_Cancel) return;
            OnActiveRangeChange(CreateArray(left, right));
            int i = left;
            int j = right;
            int c = (left + right) / 2;
            T pivot = _Array[c];
            while (i <= j)
            {
                if (_Cancel) return;
                while (Compare(i, c) < 0)
                {
                    if (_Cancel) return;
                    i++;
                }
                while (Compare(j, c) > 0)
                {
                    if (_Cancel) return;
                    j--;
                }
                if (i <= j)
                {
                    if (_Cancel) return;
                    Swap(i, j);
                    if (c == i) c = j;
                    else if (c == j) c = i;
                    i++;
                    j--;
                }
            }
            if (left < j)
                InternalSort(left, j);
            if (i < right)
                InternalSort(i, right);
        }
    }
    /*
    public class QuickSort3<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "3-Way Quick Sort"; } }

        protected override void InternalSort(T[] array)
        {
            InternalSort(0, array.Length - 1);
            if (CancelCheck()) return;
            OnComplete();
        }

        private void InternalSort(int left, int right)
        {
            if (_Cancel) return;
            OnActiveRangeChange(CreateArray(left, right));
            if (right <= left) return;
            int i = left - 1;
            int j = right;
            int p = left - 1;
            int q = right;
            T pivot = _Array[right];
            for (; ; )
            {
                if (_Cancel) return;
                while (_Array[++i].CompareTo(pivot) < 0) ;
                while (pivot.CompareTo(_Array[--j]) < 0)
                    if (j == left)
                        break;
                if (i >= j)
                    break;
                if (_Cancel) return;
                Swap(_Array[i], _Array[j]);
                if (_Array[i].CompareTo(pivot) == 0)
                {
                    p++;
                    Swap(_Array[p], _Array[i]);
                }
                if (pivot.CompareTo(_Array[j]) == 0)
                {
                    q--;
                    Swap(_Array[j], _Array[q]);
                }
            }
            if (_Cancel) return;
            Swap(_Array[i], _Array[right]);
            j = i - 1;
            i = i + 1;
            for (int k = left; k <= p; k++, j--)
                Swap(_Array[k], _Array[j]);
            for (int k = right - 1; k >= q; k--, i++)
                Swap(_Array[i], _Array[k]);
            InternalSort(left, j);
            InternalSort(i, right);
        }
        
        /*
        public void InternalSort(int l, int r)
        {
            if (r <= l) return;
            T v = _Array[r];
            int i = l - 1, j = r, p = l - 1, q = r, k;
            for (; ; )
            {
                while (_Array[++i].CompareTo(v) < 0) ;
                while (v.CompareTo(_Array[--j]) < 0) if (j == l) break;
                if (i >= j) break;
                Swap(_Array[i], _Array[j]);
                if (_Array[i].CompareTo(v) == 0) { p++; Swap(_Array[p], _Array[i]); }
                if (v.CompareTo(_Array[j]) == 0) { q--; Swap(_Array[q], _Array[j]); }
            }
            Swap(_Array[i], _Array[r]); j = i - 1; i = i + 1;
            for (k = l; k <= p; k++, j--) Swap(_Array[k], _Array[j]);
            for (k = r - 1; k >= q; k--, i++) Swap(_Array[k], _Array[i]);
            InternalSort(l, j);
            InternalSort(i, r);
        }
        */ /*

    }
    */

    public class SelectionSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Selection Sort"; } }

        protected override void InternalSort(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (CancelCheck()) return;
                int min = i;
                for (int j = i + 1; j < array.Length; j++)
                {
                    if (CancelCheck()) return;
                    if (Compare(j, min) < 0)
                    {
                        min = j;
                    }
                }
                Swap(i, min);
            }
            OnComplete();
        }
    }

    public class SelectionCocktailSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Selection Cocktail Sort"; } }

        protected override void InternalSort(T[] array)
        {
            for (int i = 0, j = array.Length - 1; i < j; i++, j--)
            {
                if (CancelCheck()) return;
                int min = i;
                int max = j;
                for (int k = i; k <= j; k++)
                {
                    if (CancelCheck()) return;
                    if (Compare(k, min) < 0)
                        min = k;
                    if (Compare(k, max) > 0)
                        max = k;
                }
                Swap(i, min);
                if (i == max) max = min;
                Swap(j, max);
            }
            OnComplete();
        }
    }

    public class HeapSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Heap Sort"; } }

        protected override void InternalSort(T[] array)
        {
            Heapify();
            for (int end = _Array.Length - 1; end > 0; end--)
            {
                if (CancelCheck()) return;
                Swap(end, 0);
                SiftDown(0, end - 1);
            }
            OnComplete();
        }

        private void Heapify()
        {
            for (int start = (_Array.Length - 2) / 2; start >= 0; start--)
            {
                if (_Cancel) return;
                SiftDown(start, _Array.Length - 1);
            }
        }

        private void SiftDown(int start, int end)
        {
            int root = start;
            while (root * 2 + 1 <= end)
            {
                int child = root * 2 + 1;
                if (_Cancel) return;
                if (child < end)
                {
                    if (Compare(child, child + 1) < 0)
                        child++;
                }
                if (_Cancel) return;
                if (Compare(root, child) < 0)
                {
                    Swap(root, child);
                    root = child;
                }
                else
                {
                    return;
                }
            }
        }
    }

    public class InsertionSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Insertion Sort"; } }

        protected override void InternalSort(T[] array)
        {
            for (int i = 1; i < array.Length; i++)
            {
                for (int j = i; j > 0 && Compare(j, j - 1) < 0; j--)
                {
                    if (CancelCheck()) return;
                    Swap(j, j - 1);
                }
            }
            OnComplete();
        }
    }
    
    public class ShellSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Shell Sort"; } }

        protected override void InternalSort(T[] array)
        {
            int[] cols = {1391376, 463792, 198768, 86961, 33936, 13776, 4592,
                            1968, 861, 336, 112, 48, 21, 7, 3, 1};
            foreach (int h in cols)
            {
                if (CancelCheck()) return;
                for (int i = h; i < array.Length; i++)
                {
                    if (CancelCheck()) return;
                    List<int> work = new List<int>();
                    for (int w = i; w >= 0; w -= h)
                        work.Add(w);
                    OnActiveRangeChange(work.ToArray());
                    int v = i;
                    int j = i;
                    while (j >= h && Compare(j-h, v) > 0)
                    {
                        if (CancelCheck()) return;
                        Swap(j, j - h);
                        v = j - h;
                        j = j - h;
                    }
                    Swap(j, v);
                    if (CancelCheck()) return;
                }
            }
            OnComplete();
        }
    }

    public class MergeSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Merge Sort"; } }

        protected override void InternalSort(T[] array)
        {
            T[] temp = new T[array.Length];
            array.CopyTo(temp, 0);
            InternalSort(temp, 0);
            temp.CopyTo(array, 0);
            if (CancelCheck()) return;
            OnComplete();
        }

        private void InternalSort(T[] array, int offset)
        {
            if (_Cancel) return;

            if (array.Length <= 1)
                return;


            T[] arr1 = new T[array.Length / 2];
            T[] arr2 = new T[array.Length - arr1.Length];
            Array.Copy(array, 0, arr1, 0, arr1.Length);
            Array.Copy(array, arr1.Length, arr2, 0, arr2.Length);

            if (_Cancel) return;

            InternalSort(arr1, offset);
            InternalSort(arr2, offset + arr1.Length);

            if (_Cancel) return;

            OnActiveRangeChange(CreateArray(offset, offset + array.Length - 1));

            int i = 0;
            int j = 0;
            int k = 0;
            while (j != arr1.Length && k != arr2.Length)
            {
                if (_Cancel) return;
                if (Compare(arr1, j, arr2, k, offset) <= 0)
                {
                    Update(array, i, arr1[j], offset);
                    i++;
                    j++;
                }
                else
                {
                    Update(array, i, arr2[k], offset);
                    i++;
                    k++;
                }
            }
            while (j != arr1.Length)
            {
                if (_Cancel) return;
                Update(array, i, arr1[j], offset);
                i++;
                j++;
            }
            while (k != arr2.Length)
            {
                if (_Cancel) return;
                array[i] = arr2[k];
                Update(array, i, arr2[k], offset);
                i++;
                k++;
            }
        }
    }
    /*
    public class StrandSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Strand Sort"; } }

        protected override void InternalSort(T[] array)
        {
            List<T> result = new List<T>();
            T[] tempArray = new T[array.Length];
            array.CopyTo(tempArray, 0);
            List<T> list = new List<T>(tempArray);
            List<T> sublist = new List<T>();
            while (list.Count > 0)
            {
                if (CancelCheck()) return;
                sublist.Clear();
                sublist.Add(list[0]);
                list.RemoveAt(0);
                ResetArray(result.ToArray(), sublist.ToArray(), list.ToArray());
                OnActiveRangeChange(CreateArray(result.Count, result.Count + sublist.Count - 1));
                for (int i = 0; i < list.Count; i++)
                {
                    if (CancelCheck()) return;
                    if (_Array[i + result.Count + sublist.Count].CompareTo(_Array[result.Count + sublist.Count - 1]) > 0)
                    {
                        sublist.Add(list[i]);
                        list.RemoveAt(i);
                        i--;
                        if (CancelCheck()) return;
                        //ResetArray(result.ToArray(), sublist.ToArray(), list.ToArray());
                        ResetArraySublist(sublist.ToArray(), result.Count);
                        OnActiveRangeChange(CreateArray(result.Count, result.Count + sublist.Count - 1));
                    }
                }
                //OnActiveRangeChange(CreateArray(result.Count, result.Count + sublist.Count - 1));
                OnActiveRangeChange(CreateArray(0, result.Count + sublist.Count - 1));
                for (int i = 0, j = 0; i < sublist.Count; i++)
                {
                    if (CancelCheck()) return;
                    bool append = true;
                    for (; j < result.Count; j++)
                    {
                        if (CancelCheck()) return;
                        //if (_Array[i + result.Count].CompareTo(_Array[j]) < 0)
                        //if (sublist[i].CompareTo(result[j]) < 0)
                        if (_Array[result.Count + i].CompareTo(_Array[j]) < 0)
                        {
                            result.Insert(j, sublist[i]);
                            append = false;
                            break;
                        }
                    }
                    if (append)
                    {
                        result.Add(sublist[i]);
                    }
                    //sublist.RemoveAt(i);
                    //i--;
                }
                sublist.Clear();
                ResetArray(result.ToArray(), sublist.ToArray(), list.ToArray());
                //OnActiveRangeChange(CreateArray(result.Count, result.Count + sublist.Count - 1));
            }
            CopyArray(result.ToArray(), 0, _Array, 0, _Array.Length);
            OnComplete();
        }

        private void ResetArray(T[] result, T[] sublist, T[] list)
        {
            CopyArray(result, 0, _Array, 0, result.Length);
            CopyArray(sublist, 0, _Array, result.Length, sublist.Length);
            CopyArray(list, 0, _Array, result.Length + sublist.Length, list.Length);
        }

        private void ResetArraySublist(T[] sublist, int offset)
        {
            CopyArray(sublist, 0, _Array, offset, sublist.Length);
        }
    }
     * */
    
    public class StrandSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Strand Sort"; } }

        protected override void InternalSort(T[] array)
        {
            T[] result = new T[array.Length];
            int resultIndex = 0;
            T[] list = new T[array.Length];
            array.CopyTo(list, 0);
            int listIndex = list.Length - 1;
            T[] sublist = new T[array.Length];
            int sublistIndex = 0;
            while (listIndex > 0)
            {
                if (CancelCheck()) return;
                sublistIndex = 1;
                sublist[0] = list[0];
                sublistIndex++;
                Remove(list, 0, listIndex--, 0);
                for (int i = 0; i <= listIndex; i++)
                {
                    if (CancelCheck()) return;
                    //if (_Array[i + result.Count + sublist.Count].CompareTo(_Array[result.Count + sublist.Count - 1]) > 0)
                    if (Compare(sublist, sublistIndex, list, i, resultIndex) > 0)
                    {
                        //sublist.Add(list[i]);
                        Insert(sublist, list[i], sublistIndex, sublistIndex++, resultIndex);
                        //list.RemoveAt(i);
                        Remove(list, i, listIndex--, resultIndex + sublistIndex);
                        i--;
                        if (CancelCheck()) return;
                    }
                }
                for (int i = 0, j = 0; i < sublistIndex; i++)
                {
                    if (CancelCheck()) return;
                    bool append = true;
                    for (; j < resultIndex; j++)
                    {
                        if (CancelCheck()) return;
                        //if (_Array[result.Count + i].CompareTo(_Array[j]) < 0)
                        if (Compare(result, j, sublist, i, 0) < 0)
                        {
                            //result.Insert(j, sublist[i]);
                            Insert(result, sublist[i], j, resultIndex++, 0);
                            append = false;
                            break;
                        }
                    }
                    if (append)
                    {
                        //result.Add(sublist[i]);
                        Insert(result, sublist[i], resultIndex, resultIndex++, 0);
                    }
                }
            }
            result.CopyTo(_Array, 0);
            OnComplete();
        }

        private void Remove(T[] array, int index, int lastIndex, int offset)
        {
            for (int i = index; i < lastIndex - 1; i++)
            {
                Update(array, i, array[i + 1], offset);
            }
        }

        private void Insert(T[] array, T value, int index, int lastIndex, int offset)
        {
            for (int i = lastIndex; i > index; i--)
            {
                Update(array, i, array[i - 1], offset);
            }
            Update(array, index, value, offset);
        }
    }
    
    public class StoogeSort<T> : SortingAlgorithm<T> where T : IComparable<T>
    {
        public override string Name { get { return "Stooge Sort"; } }

        protected override void InternalSort(T[] array)
        {
            InternalSort(0, array.Length - 1);
            if (CancelCheck()) return;
            OnComplete();
        }

        private void InternalSort(int i, int j)
        {
            if (_Cancel) return;
            //OnActiveRangeChange(CreateArray(i, j));
            if (Compare(j, i) < 0)
            {
                if (_Cancel) return;
                Swap(i, j);
            }
            if (j - i > 1)
            {
                if (_Cancel) return;
                int t = (j - i + 1) / 3;
                InternalSort(i, j - t);
                InternalSort(i + t, j);
                InternalSort(i, j - t);
            }
        }
    }
     
}
