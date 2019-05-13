using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Mochi
{
    public readonly struct Form
    {
        private readonly Dictionary<string, List<string>> valuesDict;
        private readonly Dictionary<string, List<Part>> partsDict;

        public Form(Dictionary<string, List<string>> valuesDict, Dictionary<string, List<Part>> partsDict)
        {
            this.valuesDict = valuesDict;
            this.partsDict = partsDict;
        }

        public IEnumerable<(string name, IReadOnlyList<string> values)> EnumerateAllValues()
        {
            if (this.valuesDict == null) yield break;

            foreach (var pair in this.valuesDict)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        public string GetValue(string name)
        {
            var values = GetValues(name);
            return values.Count > 0 ? values[0] : string.Empty;
        }

        public IReadOnlyList<string> GetValues(string name)
        {
            if (this.valuesDict == null) return Array.Empty<string>();

            return this.valuesDict.TryGetValue(name, out var values) ? (IReadOnlyList<string>)values : Array.Empty<string>();
        }

        public Stream GetFile(string name)
        {
            if (this.partsDict == null) return null;

            return this.partsDict.TryGetValue(name, out var parts) ? parts[0].GetFile() : null;
        }

        public IReadOnlyList<Stream> GetFiles(string name)
        {
            if (this.partsDict == null) return Array.Empty<Stream>();

            return this.partsDict.TryGetValue(name, out var parts) ?
                parts.Select(p => p.GetFile()).ToArray() :
                Array.Empty<Stream>();
        }
    }
}