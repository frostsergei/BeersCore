using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeersCore
{
    class LinearRegressionCalculation : IComputeFunc<LinearModel>
    {
        private IIgnite localIgnite { get; set; }
        private int partialCorrelation { get; set; }

        public LinearRegressionCalculation(IIgnite ignite, int part)
        {
            localIgnite = ignite;
            partialCorrelation = part;
        }

        private (DenseMatrix, DenseVector) InitData(string CACHE_NAME)
        {
            var xOne = new List<double>();
            var xAroma = new List<double>();
            var xAppearance = new List<double>();
            var xTaste = new List<double>();
            var xPalate = new List<double>();
            var yOverall = new List<double>();

            foreach (var item in localIgnite.GetCache<int, BeerReview>(Program.VECTORS))
            {
                xOne.Add(1.0);
                xAroma.Add(Convert.ToDouble(item.Value.review_aroma.Replace(".", ",")));
                xAppearance.Add(Convert.ToDouble(item.Value.review_appearance.Replace(".", ",")));
                xTaste.Add(Convert.ToDouble(item.Value.review_taste.Replace(".", ",")));
                xPalate.Add(Convert.ToDouble(item.Value.review_palate.Replace(".", ",")));

                yOverall.Add(Convert.ToDouble(item.Value.review_overall.Replace(".", ",")));
            }

            var X = DenseMatrix.OfColumns(new[] { new DenseVector(xOne.ToArray()), new DenseVector(xAroma.ToArray()), new DenseVector(xAppearance.ToArray()), new DenseVector(xPalate.ToArray()), new DenseVector(xTaste.ToArray()) });
            var Y = new DenseVector(yOverall.ToArray());

            return (X, Y);
        }



        LinearModel IComputeFunc<LinearModel>.Invoke()
        {
            var (X, Y) = InitData(Program.VECTORS);

            var R = X.QR().Solve(Y);
            LinearModel solution = new LinearModel();

            solution.bias = R[0];
            solution.aroma = R[1];
            solution.appearance = R[2];
            solution.palate = R[3];
            solution.taste = R[4];

            // Calculate correlation between Y and estimates, estimates: E=X*R
            if (partialCorrelation != 0)
                solution.correlation = Correlation.Pearson(Y, X * R);
            else
            {
                var (aX, aY) = InitData(Program.ALL);
                solution.correlation = Correlation.Pearson(aY, aX * R);
            }

            return solution;
        }
    }
}
