﻿using System;
using System.Threading.Tasks;
using REstate;
using REstate.Engine;
using Serilog;

namespace Semaphore
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            Log.Logger =
                new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            var semaphoreSchematic = CreateSemaphoreSchematic();

            var semaphore = await REstateHost.Agent
                .GetStateEngine<int, int>()
                .CreateMachineAsync(semaphoreSchematic);

            var task1 = Task.Run(() => DoSomeProcessingAsync(60));
            var task2 = Task.Run(() => DoSomeProcessingAsync(40));
            var task3 = Task.Run(() => DoSomeProcessingAsync(20));
            var task4 = Task.Run(() => DoSomeProcessingAsync(50));
            var task5 = Task.Run(() => DoSomeProcessingAsync(10));

            await Task.WhenAll(task1, task2, task3, task4, task5);

            Console.WriteLine("Done!");
            Console.ReadLine();

            async Task DoSomeProcessingAsync(int workPeriodMs, int onFailedToGetSlotDelayMs = 5)
            {
                while (true)
                {
                    try
                    {
                        await semaphore.SendAsync(1);

                        await Task.Delay(workPeriodMs);

                        await semaphore.SendAsync(-1);

                        break;
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Logger.Verbose("Semapore is full, waiting {delay}ms and then retrying...",
                            onFailedToGetSlotDelayMs);

                        await Task.Delay(onFailedToGetSlotDelayMs);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a Schematic that represents a semaphore with 3 slots
        /// </summary>
        /// <remarks>
        /// The following is the Schematic in DOT Graph
        /// <![CDATA[ 
        /// digraph {
        ///     rankdir="LR"
        ///     "0" -> "1" [label= "  1  "];
        ///     "1" -> "0" [label="  -1  "];
        ///     "1" -> "2" [label= "  1  "];
        ///     "2" -> "1" [label="  -1  "];
        ///     "2" -> "3" [label= "  1  "];
        ///     "3" -> "2" [label="  -1  "];
        /// } 
        /// ]]>
        /// <image url="$(SolutionDir)\src\Examples\Semaphore\diagram_white.png" />
        /// </remarks>
        private static REstate.Schematics.Schematic<int, int> CreateSemaphoreSchematic() =>
            REstateHost.Agent
                .CreateSchematic<int, int>("3SlotSemaphore")
                .WithStateConflictRetries()
                .WithState(0, state => state
                    .AsInitialState()
                    .DescribedAs("No slots filled.")
                    .WithReentrance(-1))
                .WithState(1, state => state
                    .DescribedAs("One slot filled.")
                    .WithTransitionFrom(0, 1)
                    .WithTransitionTo(0, -1))
                .WithState(2, state => state
                    .DescribedAs("Two slots filled.")
                    .WithTransitionFrom(1, 1)
                    .WithTransitionTo(1, -1))
                .WithState(3, state => state
                    .DescribedAs("Three slots filled.")
                    .WithTransitionFrom(2, 1)
                    .WithTransitionTo(2, -1))
                .Build();
    }
}
