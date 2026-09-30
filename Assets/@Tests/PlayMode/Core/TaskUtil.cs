using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace F1.Tests
{
    /// <summary>Bridges Task-based APIs into coroutine-based PlayMode tests.</summary>
    internal static class TaskUtil
    {
        public const float DefaultTimeoutSeconds = 20f;

        /// <summary>Waits for the task and fails the test if it faulted.</summary>
        public static IEnumerator Await(Task task, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            yield return AwaitIgnoringFailure(task, timeoutSeconds);
            if (task.IsFaulted)
            {
                Assert.Fail("Task failed: " + FailureOf(task));
            }
        }

        /// <summary>Waits until the task has completed, whether it succeeded or not.</summary>
        public static IEnumerator AwaitIgnoringFailure(Task task, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Task did not complete within the timeout.");
                }

                yield return null;
            }
        }

        /// <summary>The exception the task failed with, unwrapped; null if it did not fail.</summary>
        public static Exception FailureOf(Task task)
        {
            if (!task.IsFaulted)
            {
                return null;
            }

            Exception exception = task.Exception;
            while (exception is AggregateException aggregate && aggregate.InnerException != null)
            {
                exception = aggregate.InnerException;
            }

            return exception;
        }
    }
}
