using System.Collections.Generic;

namespace SomeProject.CommandTerminal
{
    public class CaseInsensitiveDictionary<T> : Dictionary<string, T>
    {
        public string GetKey(string variableName) => variableName.ToLower();
        public static string _GetKey(string variableName) => variableName.ToLower();
        public new T this[string name]
        {
            get => base[_GetKey(name)];
            set => base[_GetKey(name)] = value;
        }
        public new bool TryGetValue(string name, out T value) => base.TryGetValue(_GetKey(name), out value);
        public new bool Remove(string name) => base.Remove(_GetKey(name));
        public new void Add(string name, T value) => base.Add(_GetKey(name), value);
        public new bool ContainsKey(string name) => base.ContainsKey(_GetKey(name));
        public Dictionary<string, T> Raw => this;
    }
}
