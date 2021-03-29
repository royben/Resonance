using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a transcoding decorator.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class)]
    public class ResonanceTranscodingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the transcoding name.
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceTranscodingAttribute"/> class.
        /// </summary>
        /// <param name="name">The transcoding name.</param>
        public ResonanceTranscodingAttribute(String name)
        {
            Name = name;
        }
    }
}
