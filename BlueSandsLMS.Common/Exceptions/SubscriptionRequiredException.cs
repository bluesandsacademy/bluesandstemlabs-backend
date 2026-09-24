using System;

namespace BlueSandsLMS.Common.Exceptions
{
    public class SubscriptionRequiredException : Exception
    {
        public SubscriptionRequiredException() { }
        public SubscriptionRequiredException(string message) : base(message) { }
        public SubscriptionRequiredException(string message, Exception inner) : base(message, inner) { }
    }
}
