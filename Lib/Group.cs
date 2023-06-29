using System.Collections.Immutable;
using System.Text.Json;

namespace ENS
{
    public class Group
    {
        public readonly int Index;
        public readonly string Name;
        public readonly string Kind;
        public readonly bool Restricted;
        public readonly int[] Primary;
        public readonly int[] Secondary;
        public readonly int[] Valid;
        public readonly bool CMWhitelisted;
        public Group(int index, JsonElement json)
        {
            Index = index;
            Name = json.GetProperty("name").GetString()!;
            Restricted = json.TryGetProperty("restricted", out JsonElement _0) && _0.GetBoolean();
            Kind = Restricted ? $"[Restricted[{Name}]" : Name;
            Primary = json.GetProperty("primary").ToIntList().ToArray();
            Secondary = json.GetProperty("secondary").ToIntList().ToArray();
            HashSet<int> union = new(Primary);
            union.UnionWith(Secondary);
            Valid = union.Order().ToArray();
            CMWhitelisted = json.TryGetProperty("cm", out JsonElement _1);
            if (CMWhitelisted && _1.GetArrayLength() > 0)
            {
                throw new InvalidOperationException("expected empty");
            }
        }
    }
}
