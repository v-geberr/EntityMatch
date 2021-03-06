﻿using Autofac;
using EntityMatch;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TestMatcher
{
    class Program
    {
        static void ReadPhrases(string path, IMatcher matcher)
        {
            var timer = Stopwatch.StartNew();
            int count = 0;
            using (var stream = new StreamReader(path))
            {
                var splitter = new Regex("(?:^|,)(?:(?:\"((?:[^\"]|\"\")*)\")|([^,\"]*))", RegexOptions.Compiled);
                Func<string, string[]> split = (input) => (from Match match in splitter.Matches(input) select match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).ToArray();
                var columns = split(stream.ReadLine());
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    var values = split(line);
                    matcher.AddEntities("DVD", values[0]);
                    ++count;
                }
            }
            matcher.Compute();
            matcher.Interpretations("dkjkjh", 1, 0.0).ToList();
            Console.WriteLine($"Reading {count} phrases from {path} took {timer.Elapsed.TotalSeconds}s");
        }

        static void TestLoop(IMatcher matcher)
        {
            int spansPerPosition = 1;
            double threshold = 0.25;
            string input;
            Console.WriteLine($"-threshold {threshold} will set the threshold for dropping matches.");
            Console.WriteLine($"-spans {spansPerPosition} will control how many matches per range.");
            Console.Write("\nInput: ");
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            {
                if (input.StartsWith("-threshold"))
                {
                    if (double.TryParse(input.Substring("-threshold".Length), out threshold))
                    {
                        Console.WriteLine($"Set threshold to {threshold}");
                    }
                    else
                    {
                        Console.WriteLine("Could not parse threshold");
                    }
                }
                else if (input.StartsWith("-spans"))
                {
                    if (int.TryParse(input.Substring("-spans".Length), out spansPerPosition))
                    {
                        Console.WriteLine($"Set spans to {spansPerPosition}");
                    }
                    else
                    {
                        Console.WriteLine("Could not parse spans");
                    }
                }
                else
                {
                    var timer = Stopwatch.StartNew();
                    var interpretations = matcher.Interpretations(input, spansPerPosition, threshold).ToList();
                    Console.WriteLine($"Interpretation took {timer.Elapsed.TotalSeconds}s");
                    foreach (var interpretation in interpretations)
                    {
                        Console.WriteLine($"{interpretation}");
                    }
                }
                Console.Write("\nInput: ");
            }
        }

        private static IContainer Container { get; set; }

        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleTokenizer>().As<ITokenizer>().SingleInstance();
            builder.RegisterType<EntitiesDictionary>().As<IEntities>().SingleInstance();
            builder.Register((c) => new SynonymAlternatives(
                // new BaseAlternatives()))
                new SpellingAlternatives(new BaseAlternatives())))
                .As<IAlternatives>()
                .As<SynonymAlternatives>()
                .SingleInstance();
            builder.RegisterType<Recognizer>().As<IEntityRecognizer>().SingleInstance();
            builder.RegisterType<Matcher>().As<IMatcher>(); ;
            Container = builder.Build();
            using (var scope = Container.BeginLifetimeScope())
            {
                var matcher = scope.Resolve<IMatcher>();
                var synonyms = scope.Resolve<SynonymAlternatives>();
                synonyms.AddAlternatives("mouse", new Alternative("mouse", 1.0), new Alternative("mice", 0.9));
                ReadPhrases(@"c:\tmp\DVD.txt", matcher);
                TestLoop(matcher);
            }
        }
    }
}
