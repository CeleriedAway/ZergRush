#if UNITY_5_3_OR_NEWER

using System;
using NUnit.Framework;

namespace ZergRush.ReactiveCore.Tests
{
    [TestFixture]
    public class EventStreamTest
    {
        [Test]
        public void Once_StreamUpdates_OnlyOnce()
        {
            const int updatesCount = 3;
            var callCounter = 0;
            var stream = new EventStream();

            var firstOnlyStream = stream.Once();
            firstOnlyStream.Listen(() => callCounter++);
            for (var i = 0; i < updatesCount; i++)
                stream.Send();

            Assert.AreEqual(1, callCounter);
        }

        [Test]
        public void Listen_WhenStreamWasNotUpdated_DontUpdate()
        {
            var isUpdated = false;
            var stream = new EventStream();

            stream.Listen(() => isUpdated = true);

            Assert.False(isUpdated);
        }

        [Test]
        public void Listen_WhenStreamUpdates_Update()
        {
            var isUpdated = false;
            var stream = new EventStream();

            stream.Listen(() =>
            {
                isUpdated = true;
            });
            stream.Send();

            Assert.True(isUpdated);
        }

        [Test]
        public void Listen_WhenStreamDispose_DontUpdate()
        {
            var isUpdated = false;
            var stream = new EventStream();

            var connection = stream.Listen(() =>
            {
                isUpdated = true;
            });
            connection.Dispose();
            stream.Send();

            Assert.False(isUpdated);
        }

        [Test]
        public void MergeWith_UpdateSourceStreams_UpdateAllStreams()
        {
            var firstUpdated = false;
            var secondUpdated = false;
            var mergedUpdated = false;
            var firstStream = new EventStream();
            var secondStream = new EventStream();
            firstStream.Listen(() => firstUpdated = true);
            secondStream.Listen(() => secondUpdated = true);

            var mergedStream = firstStream.MergeWith(secondStream);
            mergedStream.Listen(() => mergedUpdated = true);
            firstStream.Send();
            secondStream.Send();

            Assert.True(firstUpdated && secondUpdated && mergedUpdated);
        }

        [Test]
        public void MergeWith_Null_ThrowsException()
        {
            var stream = new EventStream();
            Assert.Throws<ArgumentException>(() => stream.MergeWith(null, null, null));
        }
    }
}

#endif
