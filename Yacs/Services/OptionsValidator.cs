using Yacs.Exceptions;
using Yacs.Options;

namespace Yacs.Services
{
    internal static class OptionsValidator
    {
        private const int DISCOVERY_PORT_LOWER_LIMIT = 0;
        private const int DISCOVERY_PORT_UPPER_LIMIT = ushort.MaxValue;

        private const int MAXIMUM_CHANNELS_LOWER_LIMIT = 0;

        public static void Validate(HubOptions serverOptions)
        {
            if (serverOptions.DiscoveryPort < DISCOVERY_PORT_LOWER_LIMIT)
                throw new OptionsException($"{nameof(HubOptions)}.{nameof(HubOptions.DiscoveryPort)} must be equal to or greater than {DISCOVERY_PORT_LOWER_LIMIT}.");

            if (serverOptions.DiscoveryPort > DISCOVERY_PORT_UPPER_LIMIT)
                throw new OptionsException($"{nameof(HubOptions)}.{nameof(HubOptions.DiscoveryPort)} must be less than or equal to {DISCOVERY_PORT_UPPER_LIMIT}.");

            if (serverOptions.MaximumChannels < MAXIMUM_CHANNELS_LOWER_LIMIT)
                throw new OptionsException($"{nameof(HubOptions)}.{nameof(HubOptions.MaximumChannels)} must be equal to or greater than {MAXIMUM_CHANNELS_LOWER_LIMIT}.");

            Validate(serverOptions as BaseOptions);
        }

        public static void Validate(ChannelOptions channelOptions)
        {
            Validate(channelOptions as BaseOptions);
        }

        private static void Validate(BaseOptions baseOptions)
        {
            if (baseOptions.Encoding != null)
            {
                var maximumBytesForOneEncodedCharacter = baseOptions.Encoding.GetMaxByteCount(1);
                if (baseOptions.ReceptionBufferSize < maximumBytesForOneEncodedCharacter)
                    throw new OptionsException($"{nameof(HubOptions)}.{nameof(HubOptions.ReceptionBufferSize)} must be equal to or greater than the maximum number of bytes that can be produced by encoding a single {baseOptions.Encoding.HeaderName} character ({maximumBytesForOneEncodedCharacter}).");
            }
        }
    }
}
