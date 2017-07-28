using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace UniversityTimetabling.MIPModels
{
    class Helper
    {
    }

    public static class GRBExtensions
    {
       
   
        public static GRBLinExpr Sumx(this IEnumerable<GRBLinExpr> linExprs)
        {
            var expr = new GRBLinExpr();
            foreach (var lin in linExprs)
            {
                expr.Add(lin);
            }
            return expr;
        }
        public static GRBLinExpr Sumx(this IEnumerable<GRBVar> vars)
        {
            var expr = new GRBLinExpr();
            foreach (var v in vars)
            {
                expr.Add(v);
            }
            return expr;
        }

        public static GRBLinExpr Sumx<T>(this IEnumerable<T> source, Func<T, GRBVar> selector)
        {
            return source.Select(selector).Sumx();
        }
        public static GRBLinExpr Sumx<T>(this IEnumerable<T> source, Func<T, GRBLinExpr> selector) 
        {
            return source.Select(selector).Sumx();
        }
        

    }
}
