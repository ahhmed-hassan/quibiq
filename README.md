# Async/Await Introduction

Step-by-step refactoring of a breakfast simulation from fully synchronous to properly concurrent async code.

## Steps

| # | Step | Focus |
|---|---------|-------|
| 1 | Synchronous baseline | Sequential `Task.Delay().Wait()` blocking the thread at every step |
| 2 | Task-returning but still blocking | Methods return `Task<T>` while `.Wait()` still blocks internally |
| 3 | True concurrent execution | `await Task.Delay()`, fire-and-forget task creation, deferred awaiting |
| 4 | Async method composition | Encapsulating toast + butter + jam into one `async` method |

## Key Takeaways

Returning `Task<T>` from a method does not make it async if the body still calls `.Wait()`; the thread stays blocked until you replace every `.Wait()` with `await`. The wall-clock time only drops (from ~15 s to ~9 s) once tasks are started without awaiting them immediately, so the longest cooking step becomes the bottleneck instead of the sum of all steps.
