using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Matcher
{
    public class Matcher<T>
    {
        private static Regex IDENTIFIER_REGEXP = new Regex("[\\w\\.:]+");

        private List<MatcherWithPriority<T>> _results;
        private Tokenizer _tokenizer;
        private IMatchesName<T> _matchesName;
        private string _token;

        public static ICollection<MatcherWithPriority<List<string>>> CreateMatchers(string expression)
        {
            return new Matcher<List<string>>(expression, new NameMatcher())._results;
        }

        public Matcher(string expression, IMatchesName<T> matchesName)
        {
            this._results = new List<MatcherWithPriority<T>>();
            this._tokenizer = new Tokenizer(expression);
            this._matchesName = matchesName;

            this._token = _tokenizer.Next();
            while (_token != null)
            {
                int priority = 0;
                if (_token.Length == 2 && _token[1] == ':')
                {
                    switch (_token[0])
                    {
                        case 'R':
                            priority = 1;
                            break;
                        case 'L':
                            priority = -1;
                            break;
                    }
                    _token = _tokenizer.Next();
                }
                Predicate<T> matcher = ParseConjunction();
                if (matcher != null)
                {
                    _results.Add(new MatcherWithPriority<T>(matcher, priority));
                }
                if (!",".Equals(_token))
                {
                    break;
                }
                _token = _tokenizer.Next();
            }
        }

        private Predicate<T> parseInnerExpression()
        {
            List<Predicate<T>> matchers = new List<Predicate<T>>();
            Predicate<T> matcher = ParseConjunction();
            while (matcher != null)
            {
                matchers.Add(matcher);
                if ("|".Equals(_token) || ",".Equals(_token))
                {
                    do
                    {
                        _token = _tokenizer.Next();
                    } while ("|".Equals(_token) || ",".Equals(_token)); // ignore subsequent
                    // commas
                }
                else
                {
                    break;
                }
                matcher = ParseConjunction();
            }
            // some (or)
            return matcherInput =>
            {
                foreach (Predicate<T> matcher1 in matchers)
                {
                    if (matcher1.Invoke(matcherInput))
                    {
                        return true;
                    }
                }
                return false;
            };
        }

        private Predicate<T> ParseConjunction()
        {
            List<Predicate<T>> matchers = new List<Predicate<T>>();
            Predicate<T> matcher = ParseOperand();
            while (matcher != null)
            {
                matchers.Add(matcher);
                matcher = ParseOperand();
            }
            // every (and)
            return matcherInput =>
            {
                foreach (Predicate<T> matcher1 in matchers)
                {
                    if (!matcher1.Invoke(matcherInput))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        private Predicate<T> ParseOperand()
        {
            if ("-".Equals(_token))
            {
                _token = _tokenizer.Next();
                Predicate<T> expressionToNegate = ParseOperand();
                return matcherInput =>
                {
                    if (expressionToNegate == null)
                    {
                        return false;
                    }
                    return !expressionToNegate.Invoke(matcherInput);
                };
            }
            if ("(".Equals(_token))
            {
                _token = _tokenizer.Next();
                Predicate<T> expressionInParents = parseInnerExpression();
                if (")".Equals(_token))
                {
                    _token = _tokenizer.Next();
                }
                return expressionInParents;
            }
            if (IsIdentifier(_token))
            {
                ICollection<string> identifiers = new List<string>();
                do
                {
                    identifiers.Add(_token);
                    _token = _tokenizer.Next();
                } while (IsIdentifier(_token));
                return matcherInput => this._matchesName.Match(identifiers, matcherInput);
            }
            return null;
        }

        private bool IsIdentifier(string token)
        {
            return token != null && IDENTIFIER_REGEXP.Match(token).Success;
        }

        class Tokenizer
        {

            private static Regex REGEXP = new Regex("([LR]:|[\\w\\.:]+|[\\,\\|\\-\\(\\)])");

            Match match;

            public Tokenizer(string input)
            {
                this.match = REGEXP.Match(input);
            }

            public string Next()
            {
                if (match == null)
                    return null;

                match = match.NextMatch();

                if (match != null)
                    return match.Value;

                return null;
            }
        }
    }
}