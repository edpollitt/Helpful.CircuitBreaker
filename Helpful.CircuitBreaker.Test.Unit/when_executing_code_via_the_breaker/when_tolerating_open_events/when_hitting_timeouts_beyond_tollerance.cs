﻿using System;
using System.Collections.Generic;
using System.Threading;
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
    class when_hitting_timeouts_beyond_tollerance : using_a_mocked_event_factory
    {
        private CircuitBreakerConfig _config;
        private TimeSpan _timeout;
        private CircuitBreaker _circuitBreaker;
        private IRetryScheduler _scheduler;
        private List<Exception> _caughtExceptions;

        protected override void Given()
        {
            base.Given();
            _caughtExceptions = new List<Exception>();
            _timeout = TimeSpan.FromMilliseconds(100);
            _config = new CircuitBreakerConfig
            {
                UseTimeout = true,
                Timeout = _timeout,
                OpenEventTolerance = 2,
            };
            _scheduler = new FixedRetryScheduler(10);
            _circuitBreaker = Factory.GetBreaker(_config, _scheduler);
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
                _circuitBreaker.Execute(() => Thread.Sleep(10000));
            }
            catch (Exception e)
            {
                _caughtExceptions.Add(e);
            }
        }

        [Then]
        public void each_call_should_throw_an_exception()
        {
            Assert.That(_caughtExceptions.Count, Is.EqualTo(4));
        }

        [Then]
        public void the_first_three_exceptions_should_be_timeouts()
        {
            Assert.That(_caughtExceptions[0], Is.TypeOf<CircuitBreakerTimedOutException>());
            Assert.That(_caughtExceptions[1], Is.TypeOf<CircuitBreakerTimedOutException>());
            Assert.That(_caughtExceptions[2], Is.TypeOf<CircuitBreakerTimedOutException>());
        }

        [Then]
        public void the_fourth_exception_should_be_breaker_open()
        {
            Assert.That(_caughtExceptions[3], Is.TypeOf<CircuitBreakerOpenException>());
        }

        [Then]
        public void the_breaker_should_be_open()
        {
            Assert.That(_circuitBreaker.State, Is.EqualTo(BreakerState.Open));
        }

        [Then]
        public void an_open_event_should_be_raised()
        {
            OpenedEvent.Verify(e => e.RaiseEvent(_config, BreakerOpenReason.Timeout, It.IsAny<Exception>()));
        }

        [Then]
        public void two_tollerated_open_events_should_be_raised()
        {
            TolleratedOpenEvent.Verify(e => e.RaiseEvent(It.IsAny<short>(), _config, BreakerOpenReason.Timeout, It.IsAny<Exception>()), Times.Exactly(2));
        }
    }
}
