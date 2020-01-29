﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenProtocolInterpreter.Job.Advanced;

namespace MIDTesters.Job.Advanced
{
    [TestClass]
    public class TestMid0120 : MidTester
    {
        [TestMethod]
        public void Mid0120Revision1()
        {
            string package = "00200120   1        ";
            var mid = _midInterpreter.Parse(package);

            Assert.AreEqual(typeof(Mid0120), mid.GetType());
            Assert.IsNotNull(mid.HeaderData.NoAckFlag);
            Assert.AreEqual(package, mid.Pack());
        }

        [TestMethod]
        public void Mid0120ByteRevision1()
        {
            string package = "00200120   1        ";
            byte[] bytes = GetAsciiBytes(package);
            var mid = _midInterpreter.Parse(bytes);

            Assert.AreEqual(typeof(Mid0120), mid.GetType());
            Assert.IsNotNull(mid.HeaderData.NoAckFlag);
            Assert.IsTrue(mid.PackBytes().SequenceEqual(bytes));
        }
    }
}
