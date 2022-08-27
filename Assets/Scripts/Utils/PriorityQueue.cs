using System;
using System.Collections.Generic;
using UnityEngine;

// Code taken from a passage on Visual Studio Magazine, by James McCaffrey:
// https://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx
public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> data;

    public int Count
    {
        get {
            return data.Count;
        }
    }

    public PriorityQueue()
    {
        this.data = new List<T>();
    }

    public void Enqueue(T item)
    {
        data.Add(item);
        
        // Go up
        int ci = data.Count - 1; // child index; start at end
        while (ci > 0)
        {
            int pi = (ci - 1) / 2; // parent index
            if (data[ci].CompareTo(data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
            T tmp = data[ci]; data[ci] = data[pi]; data[pi] = tmp;
            ci = pi;
        }
    }

    public T Dequeue()
    {
        // assumes pq is not empty; up to calling code
        int li = data.Count - 1; // last index (before removal)
        T frontItem = data[0];   // fetch the front
        data[0] = data[li];
        data.RemoveAt(li);

        --li; // last index (after removal)

        // Go down
        int pi = 0; // parent index. start at front of pq
        while (true)
        {
            int ci = pi * 2 + 1; // left child index of parent
            if (ci > li) break;  // no children so done
            int rc = ci + 1;     // right child
            if (rc <= li && data[rc].CompareTo(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                ci = rc;
            if (data[pi].CompareTo(data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
            T tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp; // swap parent and child
            pi = ci;
        }
        return frontItem;
    }

    public bool Remove(T item)
    {
        if (data.Count == 0)
            return false;

        int ti = data.IndexOf(item); // target index
        if (ti < 0)
            return false; // Not found
        
        int li = data.Count - 1; // last index (before removal)

        data[ti] = data[li];
        data.RemoveAt(li);

        if (li == ti) // target happen to be the last item, return (goes out of range if access data[ti])
            return true;

        --li; // last index (after removal)

        int pio = (ti - 1) / 2; // target index's parent

        // short-circuit here
        if (ti == 0 || data[ti].CompareTo(data[pio]) > 0) // target happen to be the root, or target greater than parent. Go down
        {
            int pi = ti; // parent index. start at target index
            while (true)
            {
                int ci = pi * 2 + 1; // left child index of parent
                if (ci > li) break;  // no children so done
                int rc = ci + 1;     // right child
                if (rc <= li && data[rc].CompareTo(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                    ci = rc;
                if (data[pi].CompareTo(data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
                T tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp; // swap parent and child
                pi = ci;
            }
        }
        else if (data[ti].CompareTo(data[pio]) < 0) // target is not root, and target smaller than parent. Go up
        {
            int ci = ti; // child index. start at target index
            while (ci > 0)
            {
                int pi = (ci - 1) / 2; // parent index
                if (data[ci].CompareTo(data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
                T tmp = data[ci]; data[ci] = data[pi]; data[pi] = tmp;
                ci = pi;
            }
        }

        return true;
    }

    public T Peek()
    {
        T frontItem = data[0];
        return frontItem;
    }

    public bool Contains(T item)
    {
        return data.Contains(item);
    }

    // TODO Remove later
    public List<T> GetValues()
    {
        return data;
    }

    public void Clear()
    {
        data.Clear();
    }

    public override string ToString()
    {
        string s = "";
        for (int i = 0; i < data.Count; ++i)
            s += data[i].ToString() + " ";
        s += "count = " + data.Count;
        return s;
    }

    public bool IsConsistent()
    {
        // is the heap property true for all data?
        if (data.Count == 0) return true;
        int li = data.Count - 1; // last index
        for (int pi = 0; pi < data.Count; ++pi) // each parent index
        {
            int lci = 2 * pi + 1; // left child index
            int rci = 2 * pi + 2; // right child index

            if (lci <= li && data[pi].CompareTo(data[lci]) > 0) return false; // if lc exists and it's greater than parent then bad.
            if (rci <= li && data[pi].CompareTo(data[rci]) > 0) return false; // check the right child too.
        }
        return true; // passed all checks
    } // IsConsistent
}