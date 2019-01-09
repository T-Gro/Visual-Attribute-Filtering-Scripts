using System;
using System.Threading.Tasks;
using ProtoBuf;

namespace KnnResults.Domain
{
  [ProtoContract]
  public struct SearchHit
  {
    [ProtoMember(1)]
    public Patch Hit { get; set; }
    [ProtoMember(2)]
    public float Distance { get; set; }
  }
}
