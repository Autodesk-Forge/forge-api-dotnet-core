using System.Text;
using Xunit;

namespace Autodesk.Forge.Core.Test
{
    public class TestMarshalling
    {
        [Fact]
        public async Task TestDeserializeThrowsOnNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Marshalling.DeserializeAsync<string>(null));
        }

        [Fact]
        public async Task TestDeserializeNonJsonThrows()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Marshalling.DeserializeAsync<string>(new ByteArrayContent(new byte[] { 0, 2, 3 })));
        }

        [Fact]
        public async Task TestDeserializeValidString()
        {
            var ret = await Marshalling.DeserializeAsync<string>(new StringContent("\"bla\"", Encoding.UTF8, "application/json"));
            Assert.Equal("bla", ret);
        }

        [Fact]
        public async Task TestDeserializeNull()
        {
            var ret = await Marshalling.DeserializeAsync<string>(new StringContent("null", Encoding.UTF8, "application/json"));
            Assert.Null(ret);
        }

        [Fact]
        public async Task TestDeserializeNullInvalid()
        {
            await Assert.ThrowsAsync <Newtonsoft.Json.JsonSerializationException>(() => Marshalling.DeserializeAsync<int>(new StringContent("null", Encoding.UTF8, "application/json")));
        }

        [Fact]
        public void TestSerializeValidString()
        {
            var content = Marshalling.Serialize("bla");
        }

        [Fact]
        public void TestBuildRequestUriUnmatchedPathTemplateThrows()
        {
            Assert.Throws<KeyNotFoundException>(() => Marshalling.BuildRequestUri("/test/{foo}/{some}",
                new Dictionary<string, object>
                {
                    { "foo", "bar"},
                },
                new Dictionary<string, object>()
                ));
        }
        [Fact]
        public void TestBuildRequestUriValid()
        {
            var uri = Marshalling.BuildRequestUri("/test/{foo}/{some}",
                new Dictionary<string, object>
                {
                    { "foo", "bar"},
                    { "some", "stuff"},
                },
                new Dictionary<string, object>
                {
                    { "page", "blabla" },
                    { "count", "3" }
                }
                );
            Assert.Equal("test/bar/stuff?page=blabla&count=3&", uri.OriginalString);
        }
    }
}
