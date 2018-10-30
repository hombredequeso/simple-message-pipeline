using Xunit;

namespace SimpleMessagePipelineTests
{
    public static class AssertionExtentions
    {
        public static void AssertFail(this string msg)
        {
            Assert.True(false, msg);
        }
    }
}