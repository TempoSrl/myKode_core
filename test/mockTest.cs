using System;

using mdl;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using Moq;
using NUnit;
using Moq.Language.Flow;
using Moq.Language;
using Moq.Linq;
using Moq.Protected;
using q = mdl.MetaExpression;
using NUnit.Framework;

namespace test {
    [TestFixture]
    public class MockTest {
        [Test]
        public void dataAccessMock() {
            var dMock = new Mock<DataAccess>(MockBehavior.Strict,
                "dsn","dummyServer","dummyDataBase","user","password",DateTime.Now.Year,DateTime.Now.Date);
            var t = new DataTable("config");
            // Invalid setup on a non-virtual (overridable in VB) member
            dMock.Setup(x => x.Select("config","*",null,null,null,null,-1))
	            .Returns( async () => { return t;});

            dMock.Setup(x => x.Reset());
            dMock.Protected().Setup("CreateDataAccess",ItExpr.IsAny<bool>(),
                ItExpr.IsAny<string>(),
                ItExpr.IsAny<string>(),
                ItExpr.IsAny<string>(),
                ItExpr.IsAny<string>(),
                ItExpr.IsAny<string>(),
                ItExpr.IsAny<int>(),
                ItExpr.IsAny<DateTime>());
               
            //dMock.Setup(x => x.CreateDataAccess(true,"dsn", "dummyServer", "dummyDataBase", 
            //        DateTime.Now.Year, DateTime.Now.Date));

            DataAccess conn = dMock.Object;
            var tFound = conn.Select("config");
            Assert.AreEqual(t, tFound, "RUN_SELECT mock called");

            Assert.That(() => {
                var tFound2 = conn.Select("registry");
                return false;
            },Throws.Exception);

            
        }
    }
}
