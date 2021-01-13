using System.Text;

namespace Yacs.Options
{
    /// <summary>
    /// Represents the options to create a <see cref="Channel"/>.
    /// </summary>
    public class ChannelOptions : BaseOptions
    {
        /// <summary>
        /// Gets or sets the active monitoring option. When this option is enabled, the channel will periodically check if the communication is correct, triggering a <see cref="Events.ChannelDisconnectedEventArgs"/> when it is not successful.
        /// </summary>
        public bool ActiveMonitoring { get; set; }

        /// <summary>
        /// Creates new <see cref="ChannelOptions"/> with the default values.
        /// </summary>
        public ChannelOptions() : base()
        {
            ActiveMonitoring = false;
        }
    }
}
