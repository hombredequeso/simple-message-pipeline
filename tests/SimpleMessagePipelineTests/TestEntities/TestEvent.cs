using System;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TestEvent : IEquatable<TestEvent>
    {
        public TestEvent(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }

        public static string MessageType =
            "simplemessagepipelinetests.testentities.testevent:1.0";
        public bool Equals(TestEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestEvent) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(TestEvent left, TestEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TestEvent left, TestEvent right)
        {
            return !Equals(left, right);
        }

    }
}