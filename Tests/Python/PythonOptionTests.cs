/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class PythonOptionTests
    {
        [Test]
        public void PythonFilterFunctionReturnsList()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spyOption = algorithm.AddOption("SPY");

            using (Py.GIL())
            {
                //Filter function that returns a list of symbols
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "def filter(universe):\n" +
                    "   universe = universe.WeeklysOnly().Expiration(0, 10)\n" +
                    "   return [symbol for symbol in universe\n"+
                    "           if symbol.ID.OptionRight != OptionRight.Put\n" +
                    "           and universe.Underlying.Price - symbol.ID.StrikePrice < 10]\n"
                );

                var filterFunction = module.GetAttr("filter");
                Assert.DoesNotThrow(() => spyOption.SetFilter(filterFunction));
            }


        }

        [Test]
        public void PythonFilterFunctionReturnsUniverse()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spyOption = algorithm.AddOption("SPY");

            using (Py.GIL())
            {
                //Filter function that returns a OptionFilterUniverse
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "def filter(universe):\n" +
                    "   universe = universe.WeeklysOnly().Expiration(0, 5)\n" +
                    "   return universe"
                );

                var filterFunction = module.GetAttr("filter");
                Assert.DoesNotThrow(() => spyOption.SetFilter(filterFunction));
            }
        }

        [Test]
        public void FilterReturnsUniverseRegression()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("FilterUniverseRegressionAlgorithm",
                new Dictionary<string, string> {
                    {"Total Trades", "2"},
                    {"Average Win", "0%"},
                    {"Average Loss", "-0.02%"},
                    {"Compounding Annual Return", "-1.629%"},
                    {"Drawdown", "0.000%"},
                    {"Expectancy", "-1"},
                    {"Net Profit", "-0.021%"},
                    {"Sharpe Ratio", "0"},
                    {"Probabilistic Sharpe Ratio", "0%"},
                    {"Loss Rate", "100%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0"},
                    {"Annual Standard Deviation", "0"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "0"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$1.00"},
                    {"OrderListHash", "15d59ea72c0f1becb767a61a82f2241f"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }
    }
}
