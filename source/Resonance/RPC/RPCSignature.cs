using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Resonance.RPC
{
    /// <summary>
    /// Represents a remote procedure call signature.
    /// </summary>
    public class RPCSignature
    {
        /// <summary>
        /// Gets or sets the interface member type.
        /// </summary>
        public RPCSignatureType Type { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public String Service { get; set; }

        /// <summary>
        /// Gets or sets the service member name.
        /// </summary>
        public String Member { get; set; }

        /// <summary>
        /// Creates a signature from the specified member.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns></returns>
        public static RPCSignature FromMemberInfo(MemberInfo info)
        {
            RPCSignature signature = new RPCSignature();

            if (info is MethodInfo)
            {
                signature.Type = RPCSignatureType.Method;
            }
            else if (info is PropertyInfo)
            {
                signature.Type = RPCSignatureType.Property;
            }
            else if (info is EventInfo)
            {
                signature.Type = RPCSignatureType.Event;
            }

            signature.Service = info.DeclaringType.Name;
            signature.Member = info.Name;

            return signature;
        }

        /// <summary>
        /// Parses the specified signature string to a new signature object.
        /// </summary>
        /// <param name="signatureString">The signature string.</param>
        /// <returns></returns>
        public static RPCSignature FromString(String signatureString)
        {
            RPCSignature signature = new RPCSignature();

            String[] arr1 = signatureString.Split(':');
            String[] arr2 = arr1[1].Split('.');

            String type = arr1[0];
            String service = arr2[0];
            String member = arr2[1];

            signature.Type = (RPCSignatureType)Enum.Parse(typeof(RPCSignatureType), type);
            signature.Service = service;
            signature.Member = member;

            return signature;
        }

        /// <summary>
        /// Returns the full signature string representation.
        /// </summary>
        public override string ToString()
        {
            return $"{Type}:{Service}.{Member}";
        }

        /// <summary>
        /// Returns a the descriptive representation of this signature.
        /// </summary>
        /// <returns></returns>
        public string ToDescription()
        {
            return $"{Service}.{Member}";
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="RPCSignature"/> to <see cref="String"/>.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator String(RPCSignature signature) => signature.ToString();

        /// <summary>
        /// Performs an explicit conversion from <see cref="String"/> to <see cref="RPCSignature"/>.
        /// </summary>
        /// <param name="signatureString">The signature string.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static explicit operator RPCSignature(String signatureString) => RPCSignature.FromString(signatureString);
    }
}
