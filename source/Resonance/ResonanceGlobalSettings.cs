using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents the Resonance library global settings.
    /// </summary>
    public class ResonanceGlobalSettings
    {
        private static readonly Lazy<ResonanceGlobalSettings> _default = new Lazy<ResonanceGlobalSettings>(() => new ResonanceGlobalSettings());

        /// <summary>
        /// Gets the default global settings instance.
        /// </summary>
        public static ResonanceGlobalSettings Default
        {
            get 
            {
                return _default.Value; 
            }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="ResonanceGlobalSettings"/> class from being created.
        /// </summary>
        private ResonanceGlobalSettings()
        {

        }

        /// <summary>
        /// Gets or sets the default header transcoder for all <see cref="IResonanceEncoder"/> and <see cref="IResonanceDecoder"/>.
        /// </summary>
        public IResonanceHeaderTranscoder DefaultHeaderTranscoder { get; } = new ResonanceDefaultHeaderTranscoder();

        /// <summary>
        /// Gets or sets the default request timeout for all <see cref="IResonanceTransporter"/>.
        /// </summary>
        public TimeSpan DefaultRequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the default keep alive configuration.
        /// </summary>
        public ResonanceKeepAliveConfiguration DefaultKeepAliveConfiguration { get; } = new ResonanceKeepAliveConfiguration();

        /// <summary>
        /// Gets or sets the default compression configuration.
        /// </summary>
        public ResonanceCompressionConfiguration DefaultCompressionConfiguration { get; } = new ResonanceCompressionConfiguration();

        /// <summary>
        /// Gets or sets the encryption configuration.
        /// </summary>
        public ResonanceEncryptionConfiguration DefaultEncryptionConfiguration { get; } = new ResonanceEncryptionConfiguration();
    }
}
