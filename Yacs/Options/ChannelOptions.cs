using System.Text;

namespace Yacs.Options
{
    /// <summary>
    /// Represents the options to create a <see cref="Channel"/>.
    /// </summary>
    public class ChannelOptions
    {
        /// <summary>
        /// Gets or sets the encoding used for communication. Default: UTF-8
        /// </summary>
        public Encoding Encoder { get; set; }

        /// <summary>
        /// Gets or sets the size of the reception buffer in bytes. A lower number will need more iterations to get a big message, a big number will consume more memory. Default: 32767
        /// </summary>
        public int ReceptionBufferSize { get; set; }

        /// <summary>
        /// Creates new <see cref="ChannelOptions"/> with the default values.
        /// </summary>
        public ChannelOptions()
        {
            Encoder = Encoding.UTF8;
            ReceptionBufferSize = short.MaxValue;
        }
    }
}
