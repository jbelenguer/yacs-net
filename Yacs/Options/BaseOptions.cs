using System.Text;

namespace Yacs.Options
{
    /// <summary>
    /// Represents common options for <see cref="Channel"/> and <see cref="Server"/> alike.
    /// </summary>
    public abstract class BaseOptions
    {
        /// <summary>
        /// Gets or sets the encoding used for communication. If this value is null, then messages will be byte arrays instead of strings. Default: UTF-8
        /// </summary>
        public Encoding Encoder { get; set; }

        /// <summary>
        /// Gets or sets the size of the reception buffer in bytes. A lower number will need more iterations to get a big message, a big number will consume more memory. Default: 32767
        /// </summary>
        public int ReceptionBufferSize { get; set; }

        /// <summary>
        /// Creates a new <see cref="BaseOptions"/> object with the default values.
        /// </summary>
        public BaseOptions()
        {
            Encoder = Encoding.UTF8;
            ReceptionBufferSize = short.MaxValue;
        }
    }
}
