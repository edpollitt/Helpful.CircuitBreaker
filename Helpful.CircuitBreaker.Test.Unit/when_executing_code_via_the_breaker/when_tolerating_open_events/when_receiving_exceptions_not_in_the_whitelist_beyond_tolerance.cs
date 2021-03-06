﻿using System;
using System.Collections.Generic;
using Helpful.BDD;
using Helpful.CircuitBreaker;
using Helpful.CircuitBreaker.Events;
using Helpful.CircuitBreaker.Exceptions;
using Helpful.CircuitBreaker.Schedulers;
using Helpful.CircuitBreaker.Test.Unit;
using Moq;
using NUnit.Framework;

namespace when_executing_code_via_the_breaker.when_tollerating_open_events
{
    class when_receiving_exceptions_not_in_the_whitelist_beyond_tollerance : using_a_mocked_event_factory
    {
        private CircuitBreakerConfig _config;
        private CircuitBreaker _circuitBreaker;
        private IRetryScheduler _scheduler;
        private List<Exception> _caughtExceptions;
        private NullReferenceException _thrownException;

        protected override void Given()
        {
            base.Given();
            _caughtExceptions = new List<Exception>();
            _config = new CircuitBreakerConfig
            {
                ExpectedExceptionListType = ExceptionListType.WhiteList,
                OpenEventTolerance = 2,
            };
            _config.ExpectedExceptionList.Add(typeof(ArgumentNullException));
            _scheduler = new FixedRetryScheduler(10);
            _circuitBreaker = Factory.GetBreaker(_config, _scheduler);
            _thrownException = new NullReferenceException();
        }

        protected override void When()
        {
            CallExecute();
            CallExecute();
            CallExecute();
            CallExecute();
        }

        private void CallExecute()
        {
            try
            {
                _circuitBreaker.Execute(() => { throw _thrownException; });
            }
            catch (Exception e)
            {
                _caughtExceptions.Add(e);
            }
        }

        [Then]
        public void an_exception_should_be_thrown_for_each_call()
        {
            Assert.That(_caughtExceptions.Count, Is.EqualTo(4));
        }

        [Then]
        public void the_first_three_exceptions_should_be_execution_errors()
        {
            Assert.That(_caughtExceptions[0], Is.TypeOf<CircuitBreakerExecutionErrorException>());
            Assert.That(_caughtExceptions[1], Is.TypeOf<CircuitBreakerExecutionErrorException>());
            Assert.That(_caughtExceptions[2], Is.TypeOf<CircuitBreakerExecutionErrorException>());
        }

        [Then]
        public void the_fourth_exception_should_be_breaker_open()
        {
            Assert.That(_caughtExceptions[3], Is.TypeOf<CircuitBreakerOpenException>());
        }

        [Then]
        public void an_open_event_should_be_raised()
        {
            OpenedEvent.Verify(e => e.RaiseEvent(_config, BreakerOpenReason.Exception, It.IsAny<Exception>()));
        }

        [Then]
        public void two_tollerated_open_events_should_be_raised()
        {
            TolleratedOpenEvent.Verify(e => e.RaiseEvent(It.IsAny<short>(), _config, BreakerOpenReason.Exception, It.IsAny<Exception>()), Times.Exactly(2));
        }
    }
}
