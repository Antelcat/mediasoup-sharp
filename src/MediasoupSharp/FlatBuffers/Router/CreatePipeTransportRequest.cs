// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using FlatBuffers.PipeTransport;
using MediasoupSharp.FlatBuffers.PipeTransport.T;

namespace FlatBuffers.Router
{

  using System;
  using System.Collections.Generic;
  using Google.FlatBuffers;
  using System.Text.Json.Serialization;

  public struct CreatePipeTransportRequest : IFlatbufferObject
  {
    private Table __p;
    public ByteBuffer ByteBuffer { get { return __p.bb; } }
    public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
    public static CreatePipeTransportRequest GetRootAsCreatePipeTransportRequest( ByteBuffer _bb ) { return GetRootAsCreatePipeTransportRequest( _bb, new CreatePipeTransportRequest() ); }
    public static CreatePipeTransportRequest GetRootAsCreatePipeTransportRequest( ByteBuffer _bb, CreatePipeTransportRequest obj ) { return (obj.__assign( _bb.GetInt( _bb.Position ) + _bb.Position, _bb )); }
    public void __init( int _i, ByteBuffer _bb ) { __p = new Table( _i, _bb ); }
    public CreatePipeTransportRequest __assign( int _i, ByteBuffer _bb ) { __init( _i, _bb ); return this; }

    public string TransportId { get { int o = __p.__offset( 4 ); return o != 0 ? __p.__string( o + __p.bb_pos ) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetTransportIdBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
    public ArraySegment<byte>? GetTransportIdBytes() { return __p.__vector_as_arraysegment( 4 ); }
#endif
    public byte[] GetTransportIdArray() { return __p.__vector_as_array<byte>( 4 ); }
    public PipeTransportOptions? Options { get { int o = __p.__offset( 6 ); return o != 0 ? ( PipeTransportOptions? ) (new PipeTransportOptions()).__assign( __p.__indirect( o + __p.bb_pos ), __p.bb ) : null; } }

    public static Offset<CreatePipeTransportRequest> CreateCreatePipeTransportRequest( FlatBufferBuilder builder,
                                                                                       StringOffset transport_idOffset = default( StringOffset ),
                                                                                       Offset<PipeTransportOptions> optionsOffset = default( Offset<PipeTransportOptions> ) )
    {
      builder.StartTable( 2 );
      CreatePipeTransportRequest.AddOptions( builder, optionsOffset );
      CreatePipeTransportRequest.AddTransportId( builder, transport_idOffset );
      return CreatePipeTransportRequest.EndCreatePipeTransportRequest( builder );
    }

    public static void StartCreatePipeTransportRequest( FlatBufferBuilder builder ) { builder.StartTable( 2 ); }
    public static void AddTransportId( FlatBufferBuilder builder, StringOffset transportIdOffset ) { builder.AddOffset( 0, transportIdOffset.Value, 0 ); }
    public static void AddOptions( FlatBufferBuilder builder, Offset<PipeTransportOptions> optionsOffset ) { builder.AddOffset( 1, optionsOffset.Value, 0 ); }
    public static Offset<CreatePipeTransportRequest> EndCreatePipeTransportRequest( FlatBufferBuilder builder )
    {
      int o = builder.EndTable();
      builder.Required( o, 4 );  // transport_id
      builder.Required( o, 6 );  // options
      return new Offset<CreatePipeTransportRequest>( o );
    }
    public CreatePipeTransportRequestT UnPack()
    {
      var _o = new CreatePipeTransportRequestT();
      this.UnPackTo( _o );
      return _o;
    }
    public void UnPackTo( CreatePipeTransportRequestT _o )
    {
      _o.TransportId = this.TransportId;
      _o.Options = this.Options.HasValue ? this.Options.Value.UnPack() : null;
    }
    public static Offset<CreatePipeTransportRequest> Pack( FlatBufferBuilder builder, CreatePipeTransportRequestT _o )
    {
      if ( _o == null )
        return default( Offset<CreatePipeTransportRequest> );
      var _transport_id = _o.TransportId == null ? default( StringOffset ) : builder.CreateString( _o.TransportId );
      var _options = _o.Options == null ? default( Offset<PipeTransportOptions> ) : PipeTransportOptions.Pack( builder, _o.Options );
      return CreateCreatePipeTransportRequest(
        builder,
        _transport_id,
        _options );
    }
  }

  public class CreatePipeTransportRequestT
  {
    [JsonPropertyName( "transport_id" )]
    public string TransportId { get; set; }
    [JsonPropertyName( "options" )]
    public PipeTransportOptionsT Options { get; set; }

    public CreatePipeTransportRequestT()
    {
      this.TransportId = null;
      this.Options = null;
    }
  }


}
