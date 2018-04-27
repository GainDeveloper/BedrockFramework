/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Map Definition: BiDirectional Dictionary
Source: https://stackoverflow.com/questions/10966331/two-way-bidirectional-dictionary-in-c
********************************************************/
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class Map<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>
{
    [ReadOnly, ShowInInspector]
    private readonly Dictionary<T1, T2> _forward = new Dictionary<T1, T2>();
    private readonly Dictionary<T2, T1> _reverse = new Dictionary<T2, T1>();

    public Map()
    {
        Forward = new Indexer<T1, T2>(_forward);
        Reverse = new Indexer<T2, T1>(_reverse);
    }

    public Indexer<T1, T2> Forward { get; private set; }
    public Indexer<T2, T1> Reverse { get; private set; }
	
    public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
    {
        return _forward.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<T1, T2>>)_forward).GetEnumerator();
    }

    public void Add(T1 t1, T2 t2)
    {
        _forward.Add(t1, t2);
        _reverse.Add(t2, t1);
    }

    public void Remove(T1 t1)
    {
        _reverse.Remove(_forward[t1]);
        _forward.Remove(t1);
    }

    public class Indexer<T3, T4>
    {
        private readonly Dictionary<T3, T4> _dictionary;

        public Indexer(Dictionary<T3, T4> dictionary)
        {
            _dictionary = dictionary;
        }

        public T4 this[T3 index]
        {
            get { return _dictionary[index]; }
            set { _dictionary[index] = value; }
        }

        public bool Contains(T3 key)
        {
            return _dictionary.ContainsKey(key);
        }
    }
}