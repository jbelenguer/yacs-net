namespace Yacs.Options
{
    /// <summary>
    /// Represents the options to create a <see cref="Server"/>.
    /// </summary>
    public class ServerOptions : ChannelOptions
    {
        /// <summary>
        /// Gets if a server is discoverable. Default: yes 
        /// </summary>
        public bool IsDiscoverable => DiscoveryPort > 0;

        /// <summary>
        /// Gets or sets the port used for the discovery. Default: 11000
        /// </summary>
        public int DiscoveryPort { get; set; }

        /// <summary>
        /// Creates a new <see cref="ServerOptions" object with the default values./>
        /// </summary>
        public ServerOptions() : base()
        {
            DiscoveryPort = 11000;
        }
    }
}
