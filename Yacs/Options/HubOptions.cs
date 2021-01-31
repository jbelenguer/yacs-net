namespace Yacs.Options
{
    /// <summary>
    /// Represents the options to create a <see cref="Hub"/>.
    /// </summary>
    public class HubOptions : BaseOptions
    {
        /// <summary>
        /// Gets if a <see cref="Hub"/> is discoverable. Default: yes 
        /// </summary>
        public bool IsDiscoverable => DiscoveryPort > 0;

        /// <summary>
        /// Gets or sets the port used for the discovery. Default: 11000
        /// </summary>
        public int DiscoveryPort { get; set; }

        /// <summary>
        /// Gets or sets if the channels in the <see cref="Hub"/> should be monitored for disconnections. Default: false
        /// </summary>
        public bool ActiveChannelMonitoring { get; set; }

        /// <summary>
        /// Creates a new <see cref="HubOptions"/> object with the default values.
        /// </summary>
        public HubOptions() : base()
        {
            DiscoveryPort = 11000;
            ActiveChannelMonitoring = false;
        }
    }
}
