﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenProtocolInterpreter.IOInterface;

namespace MIDTesters.IOInterface
{
    [TestClass]
    public class TestMid0216 : MidTester
    {
        [TestMethod]
        public void Mid0216Revision1()
        {
            string package = "00230216   1        026";
            var mid = _midInterpreter.Parse<Mid0216>(package);

            Assert.AreEqual(typeof(Mid0216), mid.GetType());
            Assert.IsNotNull(mid.RelayNumber);
            Assert.AreEqual(package, mid.Pack());
        }

        [TestMethod]
        public void Mid0216ByteRevision1()
        {
            string package = "00230216   1        026";
            byte[] bytes = GetAsciiBytes(package);
            var mid = _midInterpreter.Parse<Mid0216>(bytes);

            Assert.AreEqual(typeof(Mid0216), mid.GetType());
            Assert.IsNotNull(mid.RelayNumber);

            Assert.IsTrue(mid.PackBytes().SequenceEqual(bytes));
        }
    }
}
