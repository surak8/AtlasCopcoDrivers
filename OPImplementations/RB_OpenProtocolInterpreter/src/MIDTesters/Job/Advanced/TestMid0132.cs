﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenProtocolInterpreter.Job.Advanced;

namespace MIDTesters.Job.Advanced
{
    [TestClass]
    public class TestMid0132 : MidTester
    {
        [TestMethod]
        public void Mid0132Revision1()
        {
            string package = "00200132            ";
            var mid = _midInterpreter.Parse(package);

            Assert.AreEqual(typeof(Mid0132), mid.GetType());
            Assert.AreEqual(package, mid.Pack());
        }

        [TestMethod]
        public void Mid0132ByteRevision1()
        {
            string package = "00200132            ";
            byte[] bytes = GetAsciiBytes(package);
            var mid = _midInterpreter.Parse(bytes);

            Assert.AreEqual(typeof(Mid0132), mid.GetType());
            Assert.IsTrue(mid.PackBytes().SequenceEqual(bytes));
        }
    }
}
