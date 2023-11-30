using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sleeksoft.CB.Exceptions;

namespace Sleeksoft.CB.Tests
{
    [TestClass]
    public sealed class TestAsyncWithoutResult : IDisposable
    {
        private const int MAX_FAILURES_BEFORE_TRIP = 3;
        private readonly TimeSpan CIRCUIT_RESET_TIMEOUT = TimeSpan.FromMilliseconds(150);
        private readonly TimeSpan CALL_TIMEOUT = TimeSpan.FromMilliseconds(100);

        private readonly Func<Task> COMMAND_EMPTY = () => Task.FromResult(false);
        private readonly Func<Task> COMMAND_TIMEOUT = () => Task.Delay(200);
        private readonly Func<Task> COMMAND_EXCEPTION = () => { throw new ArithmeticException(); };

        private readonly Circuit m_Circuit;

        public TestAsyncWithoutResult()
        {
            m_Circuit = new Circuit(MAX_FAILURES_BEFORE_TRIP, CALL_TIMEOUT, CIRCUIT_RESET_TIMEOUT);
        }

        [TestMethod]
        public async Task Async_MultipleCallsShouldSucceed()
        {
            m_Circuit.Close();

            for ( int i = 0; i < 20; i++ )
            {
                await m_Circuit.ExecuteAsync(COMMAND_EMPTY);
            }
            Assert.IsTrue(m_Circuit.IsClosed);
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerTimeoutException))]
        public async Task Async_TimeoutShouldThrow()
        {
            m_Circuit.Close();

            await m_Circuit.ExecuteAsync(COMMAND_TIMEOUT);
        }

        [TestMethod]
        [ExpectedException(typeof(ArithmeticException))]
        public async Task Async_ExceptionShouldThrow()
        {
            m_Circuit.Close();

            await m_Circuit.ExecuteAsync(COMMAND_EXCEPTION);
        }

        [TestMethod]
        public async Task Async_LessThanMaxFailuresShouldNotOpenCircuit()
        {
            m_Circuit.Close();

            for ( int i = 1; i < MAX_FAILURES_BEFORE_TRIP; i++ )
            {
                await this.Async_ExecuteAndSuppressException();
            }
            Assert.IsTrue(m_Circuit.IsClosed);
        }

        [TestMethod]
        public async Task Async_MaxFailuresShouldOpenCircuit()
        {
            m_Circuit.Close();

            for ( int i = 1; i <= MAX_FAILURES_BEFORE_TRIP; i++ )
            {
                await this.Async_ExecuteAndSuppressException();
            }
            Assert.IsTrue(m_Circuit.IsOpen);
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task Async_OpenCircuitShouldThrow()
        {
            m_Circuit.Open();

            await m_Circuit.ExecuteAsync(COMMAND_EMPTY);
        }

        [TestMethod]
        public void Async_OpenCircuitShouldReset()
        {
            m_Circuit.Open();

            Thread.Sleep(CIRCUIT_RESET_TIMEOUT);
            Thread.Sleep(50);
            Assert.IsTrue(m_Circuit.IsHalfOpen);
        }

        [TestMethod]
        public async Task Async_HalfOpenCircuitShouldClose()
        {
            m_Circuit.Open();

            Thread.Sleep(CIRCUIT_RESET_TIMEOUT);
            Thread.Sleep(50);
            Assert.IsTrue(m_Circuit.IsHalfOpen);

            await m_Circuit.ExecuteAsync(COMMAND_EMPTY);
            Assert.IsTrue(m_Circuit.IsClosed);
        }

        [TestMethod]
        public async Task Async_HalfOpenCircuitShouldOpen()
        {
            m_Circuit.Open();

            Thread.Sleep(CIRCUIT_RESET_TIMEOUT);
            Thread.Sleep(100);
            Assert.IsTrue(m_Circuit.IsHalfOpen);

            await this.Async_ExecuteAndSuppressException();
            Assert.IsTrue(m_Circuit.IsOpen);
        }

        private async Task Async_ExecuteAndSuppressException()
        {
            try
            {
                await m_Circuit.ExecuteAsync(COMMAND_EXCEPTION);
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