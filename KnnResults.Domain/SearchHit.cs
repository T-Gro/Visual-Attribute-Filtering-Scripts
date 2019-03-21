using System;
using System.Threading.Tasks;
using ProtoBuf;

namespace KnnResults.Domain
{
    [ProtoContract]
    public struct SearchHit : IEquatable<SearchHit>
    {
        [ProtoMember(1)]
        public Patch Hit { get; set; }
        [ProtoMember(2)]
        public float Distance { get; set; }

        public bool Equals(SearchHit other)
        {
            return Hit.Equals(other.Hit) && Distance.Equals(other.Distance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SearchHit other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Hit.GetHashCode() * 397) ^ Distance.GetHashCode();
            }
        }
    }
}
