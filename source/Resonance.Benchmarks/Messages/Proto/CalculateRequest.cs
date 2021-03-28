// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: CalculateRequest.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Resonance.Benchmarks.Messages.Proto {

  /// <summary>Holder for reflection information generated from CalculateRequest.proto</summary>
  public static partial class CalculateRequestReflection {

    #region Descriptor
    /// <summary>File descriptor for CalculateRequest.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static CalculateRequestReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChZDYWxjdWxhdGVSZXF1ZXN0LnByb3RvEg9UYW5nby5QTVIuU3R1YnMiKAoQ",
            "Q2FsY3VsYXRlUmVxdWVzdBIJCgFBGAEgASgBEgkKAUIYAiABKAFCGwoZY29t",
            "LnR3aW5lLnRhbmdvLnBtci5zdHVic2IGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Resonance.Benchmarks.Messages.Proto.CalculateRequest), global::Resonance.Benchmarks.Messages.Proto.CalculateRequest.Parser, new[]{ "A", "B" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class CalculateRequest : pb::IMessage<CalculateRequest> {
    private static readonly pb::MessageParser<CalculateRequest> _parser = new pb::MessageParser<CalculateRequest>(() => new CalculateRequest());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<CalculateRequest> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Resonance.Benchmarks.Messages.Proto.CalculateRequestReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public CalculateRequest() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public CalculateRequest(CalculateRequest other) : this() {
      a_ = other.a_;
      b_ = other.b_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public CalculateRequest Clone() {
      return new CalculateRequest(this);
    }

    /// <summary>Field number for the "A" field.</summary>
    public const int AFieldNumber = 1;
    private double a_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public double A {
      get { return a_; }
      set {
        a_ = value;
      }
    }

    /// <summary>Field number for the "B" field.</summary>
    public const int BFieldNumber = 2;
    private double b_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public double B {
      get { return b_; }
      set {
        b_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as CalculateRequest);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(CalculateRequest other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (A != other.A) return false;
      if (B != other.B) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (A != 0D) hash ^= A.GetHashCode();
      if (B != 0D) hash ^= B.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (A != 0D) {
        output.WriteRawTag(9);
        output.WriteDouble(A);
      }
      if (B != 0D) {
        output.WriteRawTag(17);
        output.WriteDouble(B);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (A != 0D) {
        size += 1 + 8;
      }
      if (B != 0D) {
        size += 1 + 8;
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(CalculateRequest other) {
      if (other == null) {
        return;
      }
      if (other.A != 0D) {
        A = other.A;
      }
      if (other.B != 0D) {
        B = other.B;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 9: {
            A = input.ReadDouble();
            break;
          }
          case 17: {
            B = input.ReadDouble();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
