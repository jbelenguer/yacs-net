using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Yacs
{
    /// <summary>
    /// Identifies a <see cref="Channel"/>
    /// </summary>
    public class ChannelIdentifier : IEquatable<ChannelIdentifier>
    {
        private string _endPointString;

        /// <summary>
        /// Creates a new <see cref="ChannelIdentifier"/> based on an <see cref="EndPoint"/>
        /// </summary>
        /// <param name="endPoint">EndPoint to identify a channel.</param>
        public ChannelIdentifier(EndPoint endPoint)
        {
            _endPointString = endPoint.ToString();
        }

        /// <summary>
        /// Determines wheter this instance and another one have the same value.
        /// </summary>
        /// <param name="other">Other instance to compare.</param>
        /// <returns></returns>
        public bool Equals(ChannelIdentifier other)
        {
            return ToString() == other.ToString();
        }

        /// <summary>
        /// Determines wheter an this instance and an object, that has to be a <see cref="ChannelIdentifier"/> have the same value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is ChannelIdentifier other && this.Equals(other);
        }

        /// <summary>
        /// Returns the hashcode for this <see cref="ChannelIdentifier"/>
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _endPointString.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the <see cref="ChannelIdentifier"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _endPointString;
        }
    }
}
