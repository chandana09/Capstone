INTRODUCTION
------------------
This is a C# implementation of the circuit breaker design pattern. 

Circuit breaker are used to provide stability and prevent cascading failures in distributed systems. They should be used in conjunction with judicious timeouts at the interfaces between remote services or systems to prevent the failure of a single service from affecting other services.

As an example, imagine a web application interacting with a remote third-party service. Let's say the third-party has oversold their capacity and their database melts down under load. Assume that the database fails in such a way that it takes a long time to hand back an error to the third-party service. This in turn makes calls fail after a long period of time. Back to our web application, the users have noticed that their form submissions take much longer, indeed appearing to hang. Well, the users will likely start hammering the refresh button, so adding more requests. This eventually causes the failure of the web application due to resource exhaustion. This will affect all users, even those who are not using functionality dependent on this third-party service.

Introducing a circuit breaker on the third-party service call lets the requests fail-fast, thereby letting the users know that something is wrong and that they don't need to refresh their requests. This also confines the failure behavior to only those users that are using functionality dependent on the third party - other users are no longer affected as there is no resource exhaustion. Circuit breakers can also allow developers to mark as unavailable those services that use the broken functionality, or perhaps show some cached content as appropriate while the circuit breaker is open.

As another example, imagine a distributed system where one of its services has experienced a failure and a subsequent restart.  While it's restarting, the system will tend to bombard it with requests, thus potentially causing another failure. A circuit breaker can give the service sufficient time to initialise itself properly before having to deal with new  requests.
 
This C# library provides an implementation of the circuit breaker pattern which has the behavior described below.

PROJECT GOALS
------------------

PROJECT MATURITY
----------------
This is an immature project that has only one production implementation.

SUPPORTED FEATURES
------------------
During normal operation, a circuit breaker is in the Closed state:
    A call that fails with an exception or exceeds the configured timeout increments a failure count
    A successful call resets the failure count to zero
    When the failure count reaches the configured maximum, the breaker is tripped into the Open state

Whilst a circuit breaker is in the Open state:
    All calls fail-fast with a CircuitBreakerOpenException
    After the configured reset interval, the circuit breaker enters a Half-Open state

Whilst a circuit breaker is in the Half-Open state:
    The first call is allowed through without failing fast
    If the first call succeeds, the breaker is reset back to Closed state
    If the first call fails, the breaker is tripped again into the Open state
 
Call types:
    Synchronous action (command) without a result
    Synchronous function (query) with a result
    Asynchronous action (command) without a result
    Asynchronous function (query) with a result

If a command fails instead of returning a result, you can now supply a fall-back command to provide a default value for the result. Note that this fall-back command should be local, as that's much less likely to fail than another remote command.

SUPPORTED VERSIONS 
------------------
Language: C# 5 and upwards. 
Framework: Version 4.5 and upwards. 
IDE: Visual Studio 2013 and upwards.

GETTING STARTED
------------------
Compile the circuit breaker library and its associated unit test library.
Execute all of the unit tests and make sure they all pass.
Add a reference to the circuit breaker library  in your project.
Look at the supplied examples that show the features of this library and how to use them.
                
ROADMAP
------------------
Enable monitoring and logging by raising OnClose, OnOpen, and OnHalfOpen events.
Support for the non-default task scheduler, to avoid blocking the default task scheduler.
