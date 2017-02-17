using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RestRunner.Models
{
    public static class GeneratorProcessor
    {
        private static readonly Random _random = new Random();
        private static readonly List<GeneratorSequence> _sequences = new List<GeneratorSequence>();

        public static string Process(string textToProcess)
        {
            var groupIndexes = new Dictionary<int, int>();
            var regex = new Regex(@"@(.*?)@");
            foreach (Match match in regex.Matches(textToProcess))
            {
                string generatorVariable = match.Value;
                string generatedText = ExpandGenerator(match.Groups[1].Value, groupIndexes);
                if (generatedText != null)
                    textToProcess = textToProcess.Replace(generatorVariable, generatedText);
            }

            return textToProcess;
        }

        private static string ExpandGenerator(string generatorText, Dictionary<int, int> groupIndexes)
        {
            if (generatorText.Contains('-'))
            {
                int dashIndex = generatorText.IndexOf('-');

                //number range
                try
                {
                    bool isCurrency = generatorText[0] == '$';
                    int minInclusive = int.Parse(generatorText.Substring(isCurrency ? 1 : 0, dashIndex - (isCurrency ? 1 : 0)));
                    int maxInclusive = int.Parse(generatorText.Substring(dashIndex + 1));

                    var result = _random.Next(minInclusive, maxInclusive + 1).ToString();
                    if (isCurrency)
                        result += $".{_random.Next(0, 100):00}";

                    return result;
                }
                catch (Exception) { }

                //char range (if there is a single character on each end of the dash, just pick a random character between the two of them (inclusive)
                if ((dashIndex == 1) && (generatorText.Length == 3))
                {
                    int minInclusive = generatorText[0];
                    int maxInclusive = generatorText[2] + 1;

                    return ((char) _random.Next(minInclusive, maxInclusive)).ToString();
                }
            }

            if (generatorText.Contains(':'))
            {
                try
                {
                    var sequence = new GeneratorSequence(generatorText.Split(':'));
                    if (!_sequences.Contains(sequence))
                        _sequences.Add(sequence);

                    //get the existing generator that matches the new one, so that you get the proper current value
                    var curSequence = _sequences.First(s => s.Equals(sequence));
                    return curSequence.NextValue().ToString();
                }
                catch (Exception) { }
            }

            if ((generatorText.Contains(',')) && (generatorText.Contains("[")) && (generatorText.EndsWith("]")))
            {
                try
                {
                    //see if there is a group number before the list (ex. @2[Moe,Larry,Curly]@)
                    int groupNumber = -1;
                    int startBracketIndex = generatorText.IndexOf('[');
                    if (!generatorText.StartsWith("["))
                    {
                        groupNumber = int.Parse(generatorText.Substring(0, startBracketIndex));
                    }

                    var values = generatorText.Substring(startBracketIndex + 1, generatorText.Length - startBracketIndex - 2).Split(',');

                    //find out which value to select
                    int selectedValueIndex = _random.Next(0, values.Length);
                    if (groupNumber >= 0)
                    {
                        if (!groupIndexes.ContainsKey(groupNumber))
                            groupIndexes[groupNumber] = _random.Next(0, values.Length);

                        selectedValueIndex = groupIndexes[groupNumber];
                    }

                    return selectedValueIndex < values.Length ? values[selectedValueIndex] : "";
                }
                catch (Exception) { }
            }

            //if this was not a valid generator, then return null, so we know not to replace anything
            return null;
        }
    }

    class GeneratorSequence : IEquatable<GeneratorSequence>
    {
        private long _currentValue;
        private readonly object _lock = new object();

        public GeneratorSequence(string[] numbers)
        {
            Start = long.Parse(numbers[0]);
            IncrementBy = long.Parse(numbers[1]);
            MaxValue = numbers.Length == 3 ? long.Parse(numbers[2]) : long.MaxValue;
            _currentValue = Start - IncrementBy; //_currentValue is always incremented before returning, so this makes the first return value correct
        }

        private long Start { get; }
        private long IncrementBy { get; }
        private long MaxValue { get; }

        public long NextValue()
        {
            lock (_lock)
            {
                if ((MaxValue - IncrementBy) < _currentValue) //this form of checking protects against out-of-bounds errors if adding IncrementBy to _currentValue would have been higher than long.MaxValue
                    _currentValue = Start;
                else
                    _currentValue += IncrementBy;
                return _currentValue;
            }
        }

        public bool Equals(GeneratorSequence other)
        {
            if (ReferenceEquals(null, other)) return false;

            return
                Start == other.Start &&
                IncrementBy == other.IncrementBy &&
                MaxValue == other.MaxValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals(obj as GeneratorSequence);
        }
    }
}
