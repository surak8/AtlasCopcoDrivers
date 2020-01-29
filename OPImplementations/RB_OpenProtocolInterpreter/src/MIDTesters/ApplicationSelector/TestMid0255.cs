﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenProtocolInterpreter.ApplicationSelector;

namespace MIDTesters.ApplicationSelector
{
    [TestClass]
    public class TestMid0255 : MidTester
    {
        [TestMethod]
        public void Mid0255Revision1()
        {
            string package = "00340255            01510221112022";
            var mid = _midInterpreter.Parse<Mid0255>(package);

            Assert.AreEqual(typeof(Mid0255), mid.GetType());
            Assert.IsNotNull(mid.DeviceId);
            Assert.IsNotNull(mid.RedLights);
            Assert.AreEqual(package, mid.Pack());
        }

        [TestMethod]
        public void Mid0255ByteRevision1()
        {
            string package = "00340255            01510221112022";
            byte[] bytes = GetAsciiBytes(package);
            var mid = _midInterpreter.Parse<Mid0255>(bytes);

            Assert.AreEqual(typeof(Mid0255), mid.GetType());
            Assert.IsNotNull(mid.DeviceId);
            Assert.IsNotNull(mid.RedLights);
            Assert.IsTrue(mid.PackBytes().SequenceEqual(bytes));
        }
    }
}
