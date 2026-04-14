# Industrial Processing System
A thread-safe, async, event-driven industrial job processing system built in C# (.NET ).

## Overview
Simulates an industrial job queue using a producer-consumer pattern with priority scheduling, async execution, retry logic, and automatic reporting.

## Features

Priority queue — jobs with lower Priority value are processed first

Thread-safe — concurrent producers submit jobs safely via SemaphoreSlim / locks

Async processing — each job runs on a Task, result exposed via JobHandle

Idempotency — duplicate job IDs are silently rejected

## Two job types:

Prime — counts primes up to N using parallel threads (capped at 1–8)

IO — simulates I/O delay via Thread.Sleep, returns random 0–100

## 

Retry logic — failed jobs (> 2s) are retried up to 2 times; after 3rd failure, ABORT is logged

Event system — JobCompleted / JobFailed events write asynchronously to a log file

Periodic reports — LINQ-based XML report every minute (rolling window of 10 files)

XML config — worker count, max queue size, and initial jobs loaded from config
