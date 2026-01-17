using System;
using System.Threading.Tasks;

namespace AsyncBreakfast
{
    // These classes are intentionally empty for the purpose of this example. They are simply marker classes for the purpose of demonstration, contain no properties, and serve no other purpose.
    internal class HashBrown { }
    internal class Coffee { }
    internal class Egg { }
    internal class Juice { }
    internal class Toast { }

    class Program
    {
        static async Task Main(string[] args)
        {
            Coffee cup = PourCoffee();
            Console.WriteLine("coffee is ready " + DateTimeOffset.Now.ToString("mm:ss") + Environment.NewLine);

            
            Task<Egg> eggTask = FryEggsAsync(2);
            Task<HashBrown> hashBrownTask = FryHashBrownsAsync(3);
            Task<Toast> toastTask = ToastBreadAsync(2);

            Toast toast = await toastTask;
            ApplyButter(toast);
            ApplyJam(toast);
            Console.WriteLine("toast is ready at: "+ DateTimeOffset.Now.ToString("mm:ss")+ Environment.NewLine);

            Juice oj = PourOJ();
            Console.WriteLine("oj is ready");

            Egg egg = await eggTask;
            Console.WriteLine("eggs are ready " + DateTimeOffset.Now.ToString("mm:ss") + Environment.NewLine);

            HashBrown hashBrown = await hashBrownTask;
            Console.WriteLine("hash browns are ready" + DateTimeOffset.Now.ToString("mm:ss") + Environment.NewLine);

            Console.WriteLine("Breakfast is ready!");
        }

        private static Juice PourOJ()
        {
            Console.WriteLine("Pouring orange juice" + DateTimeOffset.Now.ToString("mm:ss"));
            return new Juice();
        }

        private static void ApplyJam(Toast toast) =>
            Console.WriteLine("Putting jam on the toast" + DateTimeOffset.Now.ToString("mm:ss"));

        private static void ApplyButter(Toast toast) =>
            Console.WriteLine("Putting butter on the toast" + DateTimeOffset.Now.ToString("mm:ss"));

        private static async Task<Toast> ToastBreadAsync(int slices)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Putting a slice of bread in the toaster" + DateTimeOffset.Now.ToString("mm:ss"));
            }
            Console.WriteLine("Start toasting... " + DateTimeOffset.Now.ToString("mm:ss"));
            await Task.Delay(3000);
            Console.WriteLine("Remove toast from toaster " + DateTimeOffset.Now.ToString("mm:ss"));

            return new Toast();
        }

        private static async Task<HashBrown> FryHashBrownsAsync(int patties)
        {
            Console.WriteLine($"putting {patties} hash brown patties in the pan " + DateTimeOffset.Now.ToString("mm:ss"));
            Console.WriteLine("cooking first side of hash browns... " + DateTimeOffset.Now.ToString("mm:ss"));
            await Task.Delay(3000);
            for (int patty = 0; patty < patties; patty++)
            {
                Console.WriteLine("flipping a hash brown patty " + DateTimeOffset.Now.ToString("mm:ss"));
            }
            Console.WriteLine("cooking the second side of hash browns..." + DateTimeOffset.Now.ToString("mm:ss"));
            await Task.Delay(3000);
            Console.WriteLine("Put hash browns on plate" + DateTimeOffset.Now.ToString("mm:ss"));

            return (new HashBrown());
        }

        private static async Task<Egg> FryEggsAsync(int howMany)
        {
            Console.WriteLine("Warming the egg pan... " + DateTimeOffset.Now.ToString("mm:ss"));
            await Task.Delay(5000);
            Console.WriteLine($"cracking {howMany} eggs at {DateTimeOffset.Now.ToString("mm:ss")}" );
            Console.WriteLine("cooking the eggs ..." + DateTimeOffset.Now.ToString("mm:ss"));
            await Task.Delay(4000);
            Console.WriteLine("Put eggs on plate At: "+ DateTimeOffset.Now.ToString("mm:ss"));

            return new Egg();
        }

        private static Coffee PourCoffee()
        {
            Console.WriteLine("Pouring coffee At: " + DateTimeOffset.Now.ToString("mm:ss"));
            return (new Coffee());
        }
    }
}