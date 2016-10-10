namespace Switch.Tests
{
    using System;

    using Identity;

    using NUnit.Framework;

    [TestFixture]
    public class MacAddressTests
    {
        [Test]
        [TestCase("01:80:C2:00:00:00")]
        [TestCase("01:23:45:67:89:01")]
        [TestCase("AB:CD:EF:12:34:56")]
        public static void CorrectMacPassesValidation(string mac)
        {
            Assert.DoesNotThrow(() => MacAddress.Create(mac));
        }

        [Test]
        [TestCase("")]
        [TestCase("1")]
        [TestCase("12:34:56:78:90:")]
        [TestCase("12:34:56:78:90:12:")]
        [TestCase("ZZ:ZZ:ZZ:ZZ:ZZ:ZZ")]
        public static void IncorrectMacFailsValidation(string mac)
        {
            Assert.Throws<ArgumentException>(() => MacAddress.Create(mac));
        }
    }
}
