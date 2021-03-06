using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Common.Utils
{
    // Prefer built in NullReferenceException || ArgumentNullException || NoNullAllowedException
    public class NonNullException : Exception
    {
        public NonNullException() : base("Parameter cannot be null")
        {
        }
    }

    public class NonNull<T> where T : class
    {
        public NonNull(T val)
        {
            // doesn't throw Exception?
            if (val == null)
                new NonNullException();

            Value = val;
        }

        public static implicit operator T(NonNull<T> n) => n.Value;

        private readonly T Value;
    }

    public static class GenericConversionExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this T obj) => new T[] { obj };

        public static NonNull<T> ToNonNull<T>(this T obj) where T : class => new NonNull<T>(obj);
    }

    public static class EnumerableExtension
    {
        public static string AsString(this IEnumerable<char> chars) => string.Concat(chars);

        public static T FirstIfSingleOrDefault<T>(this IEnumerable<T> enumerable, T replace = default)
        {
            //Avoid enumerating the whole array.
            //If enumerable.Count() < 2, takes whole array.
            var test = enumerable.Take(2).ToList();
            return test.Count == 1 ? test[0] : replace;
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> list) => list.SelectMany(x => x);
    }

    public static class CollectionExtension
    {
        // IEnumerable class above already does this. All ICollection are IEnumerable, so favor IEnumerable?
        public static bool IsEmpty<T>(this ICollection<T> obj) => obj.Count == 0;

        // obj == null || obj.IsEmpty() causes VS to suggest merging sequential checks
        // the result is obj?.IsEmpty() == true which returns false when null
        // Other options are obj?.IsEmpty() == true || obj == null Or (obj?.IsEmpty()).GetValueOrDefault(true)
        // All three options remove the suggestion and give the intended result of this function
        public static bool IsEmptyOrNull<T>(this ICollection<T> obj) => obj?.IsEmpty() ?? true;
    }

    public static class XElementExtension
    {
        public static XElement First(this XElement element, string name) => element.Descendants(name).First();

        public static string FirstValue(this XElement element, string name) => element.First(name).Value;
    }

    public static class KeyValuePairsExtension
    {
        public static IDictionary<Key, Value> ToDictionary<Key, Value>(this IEnumerable<KeyValuePair<Key, Value>> pairs) => pairs.ToDictionary(x => x.Key, x => x.Value);
    }

    public static class TaskExtensions
    {
        public static Task<IEnumerable<TResult>> Until<TResult>(this IEnumerable<Task<TResult>> tasks, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var aggregateTask = Task.WhenAll(tasks);
            var anyTask = Task.WhenAny(timeoutTask, aggregateTask);
            var continuation = anyTask.ContinueWith((_) =>
            {
                var completedTasks = tasks.Where(t => t.Status == TaskStatus.RanToCompletion);
                var results = completedTasks.Select(t => t.Result);
                return results;
            });

            return continuation;
        }
    }
}
