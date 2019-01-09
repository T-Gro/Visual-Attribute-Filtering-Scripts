using System;
using ProtoBuf;

namespace KnnResults.Domain
{
  [ProtoContract]
  public struct Patch : IEquatable<Patch>, IComparable<Patch>
  {
    [ProtoMember(1)]
    public int ImageId { get; set; }
    [ProtoMember(2)]
    public int PatchId { get; set; }



    public bool Equals(Patch other)
    {
      return ImageId == other.ImageId && PatchId == other.PatchId;
    }

    public int CompareTo(Patch other)
    {
      if (this.ImageId == other.ImageId)
        return this.PatchId.CompareTo(other.PatchId);
      else
        return this.ImageId.CompareTo(other.ImageId);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      return obj is Patch && Equals((Patch) obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return (ImageId*397) ^ PatchId;
      }
    }
  }
}