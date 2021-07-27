using System.Collections.Generic;

namespace CommandTerminal
{
    public class CaseInsensitiveDictionary<T> : Dictionary<string, T>
    {
        public string GetKey(string variableName) => variableName.ToLower();
        public new T this[string name]
        {
            get => base[GetKey(name)];
            set => base[GetKey(name)] = value;
        }
        public new bool TryGetValue(string name, out T value) => base.TryGetValue(GetKey(name), out value);
        public new bool Remove(string name) => base.Remove(GetKey(name));
        public new void Add(string name, T value) => base.Add(GetKey(name), value);
        public new bool ContainsKey(string name) => base.ContainsKey(GetKey(name));
        public Dictionary<string, T> Raw => this;
    }
}
