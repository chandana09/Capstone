using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sleeksoft.CB.Exceptions;

namespace Sleeksoft.CB.Tests
{
    [TestClass]
    public sealed class TestSyncWithResult : IDisposable
    {
        private const int MAX_FAILURES_BEFORE_TRIP = 3;
        private readonly TimeSpan CIRCUIT_RESET_TIMEOUT = TimeSpan.FromMilliseconds(150);
        private readonly TimeSpan CALL_TIMEOUT = TimeSpan.FromMilliseconds(100);

        private readonly Func<object> COMMAND_EMPTY = () => { return new object(); };
        private readonly Func<object> COMMAND_TIMEOUT = () => { Thread.Sleep(200); return new object(); };
        private readonly Func<object> COMMAND_EXCEPTION = () => { throw new ArithmeticException(); };
        private readonly Func<object> COMMAND_FALLBACK = () => { return true; };

        private readonly Circuit m_Circuit;

        public TestSyncWithResult()
        {
            m_Circuit = new Circuit(MAX_FAILURES_BEFORE_TRIP, CALL_TIMEOUT, CIRCUIT_RESET_TIMEOUT);
        }

        [TestMethod]
        public void SyncResult_MultipleCallsShouldSucceed()
        {
            m_Circuit.Close();

            for ( int i = 0; i < 20; i++ )
            {
                m_Circuit.ExecuteSync(COMMAND_EMPTY);
            }
            Assert.IsTrue(m_Circuit.IsClosed);
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerTimeoutException))]
        public void SyncResult_TimeoutShouldThrow()
        {
            m_Circuit.Close();

            m_Circuit.ExecuteSync(COMMAND_TIMEOUT);
        }

        [TestMethod]
        [ExpectedException(typeof(ArithmeticException))]
        public void SyncResult_ExceptionShouldThrow()
        {
            m_Circuit.Close();

            m_Circuit.ExecuteSync(COMMAND_EXCEPTION);
        }

        [TestMethod]
        public void SyncResult_LessThanMaxFailuresShouldNotOpenCircuit()
        {
            m_Circuit.Close();

            for ( int i = 1; i < MAX_FAILURES_BEFORE_TRIP; i++ )
            {
                this.SyncResult_ExecuteAndSuppressException();
            }
            Assert.IsTrue(m_Circuit.IsClosed);
        }

        [TestMethod]
        public void SyncResult_MaxFailuresShouldOpenCircuit()
        {
            m_Circuit.Close();

            for ( int i = 1; i <= MAX_FAILURES_BEFORE_TRIP; i++ )
            {
                this.SyncResult_ExecuteAndSuppressException();
            }
            Assert.IsTrue(m_Circuit.IsOpen);
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public void SyncResult_OpenCircuitShouldThrow()
        {
            m_Circuit.Open();

            m_Circuit.ExecuteSync(COMMAND_EMPTY);
        }

        [TestMethod]
        public void SyncResult_OpenCircuitShouldReset()
        {
            m_Circuit.Open();

            Thread.Sleep(CIRCUIT_RESET_TIMEOUT);
            Thread.Sleep(50);
            Assert.IsTrue(m_Circuit.IsHalfOpen);
        }

        [TestMethod]
        public void SyncResult_HalfOpenCircuitShouldClose()
        {
            m_Circuit.Open();

            Thread.Sleep(CIRCUIT_RESET_TIMEOUT);
            Thread.Sleep(50);
            Assert.IsTrue(m_Circuit.IsHalfOpen);

            m_Circuit.ExecuteSync(COMMAND_EMPTY);
            Assert.IsTrue(m_Circuit.IsClosed);
        }

        [TestMethod]
        public void SyncResult_HalfOpenCircuitShouldOpen()
        {
            m_Circuit.Open();

            Thread.Sleep(CIRCUIT_RESET_TIMEOUT);
            Thread.Sleep(100);
            Assert.IsTrue(m_Circuit.IsHalfOpen);

            this.SyncResult_ExecuteAndSuppressException();
            Assert.IsTrue(m_Circuit.IsOpen);
        }

        [TestMethod]
        public void SyncResult_FallbackCommandShouldWork()
        {
            m_Circuit.Close();

            object result = m_Circuit.ExecuteSync(COMMAND_EXCEPTION, COMMAND_FALLBACK);
            Assert.IsTrue(result.GetType() == typeof(bool));
        }

        [TestMethod]
        public void SyncResult_MaxFallbacksShouldOpenCircuit()
        {
            m_Circuit.Close();

            for ( int i = 1; i <= MAX_FAILURES_BEFORE_TRIP; i++ )
            {
                m_Circuit.ExecuteSync(COMMAND_EXCEPTION, COMMAND_FALLBACK);
            }
            Assert.IsTrue(m_Circuit.IsOpen);
        }

        private void SyncResult_ExecuteAndSuppressException()
        {
            try
            {
                m_Circuit.ExecuteSync(COMMAND_EXCEPTION);
            }
            catch ( ArithmeticException )
            {
            }
        }

        /// <summary>Cleans up state related to this type.</summary>
        /// <remarks>
        /// Don't make this method virtual. A derived type should 
        /// not be able to override this method.
        /// Because this type only disposes managed resources, it 
        /// don't need a finaliser. A finaliser isn't allowed to 
        /// dispose managed resources.
        /// Without a finaliser, this type doesn't need an internal 
        /// implementation of Dispose() and doesn't need to suppress 
        /// finalisation to avoid race conditions. So the full 
        /// IDisposable code pattern isn't required.
        /// </remarks>
        public void Dispose()
        {
            if (m_Circuit != null)
            {
                m_Circuit.Dispose();
            }
        }
    }
}