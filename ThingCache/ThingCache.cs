using System.Collections.Generic;
using NUnit.Framework;
using FakeItEasy;
using FluentAssertions;

namespace MockFramework
{
    public class ThingCache
    {
        private readonly IDictionary<string, Thing> dictionary
            = new Dictionary<string, Thing>();
        private readonly IThingService thingService;

        public ThingCache(IThingService thingService)
        {
            this.thingService = thingService;
        }

        public Thing Get(string thingId)
        {
            Thing thing;
            if (dictionary.TryGetValue(thingId, out thing))
                return thing;
            if (thingService.TryRead(thingId, out thing))
            {
                dictionary[thingId] = thing;
                return thing;
            }
            return null;
        }
    }

    [TestFixture]
    public class ThingCache_Should
    {
        private IThingService thingService;
        private ThingCache thingCache;

        private const string thingId1 = "TheDress";
        private Thing thing1 = new Thing(thingId1);

        private const string thingId2 = "CoolBoots";
        private Thing thing2 = new Thing(thingId2);

        [SetUp]
        public void SetUp()
        {
            thingService = A.Fake<IThingService>();
            
        }

        [Test]
        public void GetMethod_TryReadCallsOnce_WhenTryReadReturnsTrue()
        {
            var outValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(true);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).MustHaveHappened();
        }

        [Test]
        public void GetMethod_ReadMethod_When()
        {
            var outValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(true);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out outValue))
                .MustHaveHappened(Repeated.Like((c) => c == 1));
        }
        
        [Test]
        public void GetMethod_ReadMetd_When()
        {
            var outValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(false);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out outValue))
                .MustHaveHappened(Repeated.Like((c) => c == 2));
        }
        
        [Test]
        public void GetMethod_Reaetd_When()
        {
            var outValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(false);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf").Should().BeNull();
        }
        
        [Test]
        public void GetMethod_ReadddsMetd_When()
        {
            var outValue = A.Fake<Thing>();
            var secondOutValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(true);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out secondOutValue)).Returns(true);
            thingCache.Get("hgf").Should().Be(outValue);
        }
        
        [Test]
        public void GetMethod_ReadddasMetd_When()
        {
            var outValue = A.Fake<Thing>();
            var secondOutValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(true);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out secondOutValue)).Returns(true);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out outValue))
                .MustHaveHappened(Repeated.Like((c) => c == 1));
        }
        
        [Test]
        public void GetMethod_ReaasdddasMetd_When()
        {
            var outValue = A.Fake<Thing>();
            var secondOutValue = A.Fake<Thing>();
            A.CallTo(() => thingService.TryRead("hgf", out outValue)).Returns(false);
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out secondOutValue)).Returns(true);
            thingCache.Get("hgf").Should().Be(secondOutValue);
        }
        
        [Test]
        public void GetMethod_ReadddasqaasMetd_When()
        {
            var outValue = A.Fake<Thing>();
            var secondOutValue = A.Fake<Thing>();
            thingCache = new ThingCache(thingService);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out secondOutValue)).Returns(true);
            thingCache.Get("hgf");
            A.CallTo(() => thingService.TryRead("hgf", out secondOutValue))
                .MustHaveHappened(Repeated.Like((c) => c == 2));
        }
       
    }
}