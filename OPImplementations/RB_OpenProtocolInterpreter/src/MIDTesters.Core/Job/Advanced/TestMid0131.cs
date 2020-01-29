﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenProtocolInterpreter.Job.Advanced;

namespace MIDTesters.Job.Advanced
{
    [TestClass]
    public class TestMid0131 : MidTester
    {
        [TestMethod]
        public void Mid0131Revision1()
        {
            string package = "00200131            ";
            var mid = _midInterpreter.Parse(package);

            Assert.AreEqual(typeof(Mid0131), mid.GetType());
            Assert.AreEqual(package, mid.Pack());
        }

        [TestMethod]
        public void Mid0131ByteRevision1()
        {
            string package = "00200131            ";
            byte[] bytes = GetAsciiBytes(package);
            var mid = _midInterpreter.Parse(bytes);

            Assert.AreEqual(typeof(Mid0131), mid.GetType());
            Assert.IsTrue(mid.PackBytes().SequenceEqual(bytes));
        }
    }
}
