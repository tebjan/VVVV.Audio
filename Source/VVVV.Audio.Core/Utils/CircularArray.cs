/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 25.11.2014
 * Time: 01:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;

namespace VVVV.Audio
{
    class CircularArray<T> : IEnumerator<T>
    {
        private readonly T[] FArray;
        private int index = -1;
        public T Current { get; private set; }

        public CircularArray(T[] array)
        {
            Current = default(T);
            this.FArray = array;
        }
        
        object IEnumerator.Current
        {
            get { return Current; }
        }

        T IEnumerator<T>.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (++index >= FArray.Length)
                index = 0;
            Current = FArray[index];
            return true;
        }
        
        public T Next()
        {
            if (++index >= FArray.Length)
                index = 0;
            return FArray[index];
        }
        
        /// <summary>
        /// Cirular indexer using zmod
        /// </summary>
        public T this[int index]
        {
            get
            {
                return FArray[AudioUtils.Zmod(index, FArray.Length)];
            }
            set
            {
                FArray[AudioUtils.Zmod(index, FArray.Length)] = value;
            }
        }

        public void Reset()
        {
            index = -1;
        }

        public void Dispose()
        {
            //nothing to do
        }
    }
}
