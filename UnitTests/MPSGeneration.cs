using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UniversityTimetabling;
using UniversityTimetabling.MIPModels;
using NUnit.Framework;
using UniversityTimetabling.StrategicOpt;

namespace UnitTests
{


    [TestFixture]
    public class MPSGeneration : UnitTestBase
    {




        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void WriteMPSForThreeIndex(string filename)
        {
            var data = Data.ReadXml(dataPath +  filename, "120","4");
            var dataset = System.IO.Path.GetFileNameWithoutExtension(filename);
            var formulation = ProblemFormulation.UD2;
       

            var model = new CCTModel(data,formulation,new CCTModel.MIPModelParameters()
            {
                UseStageIandII = true,
            });

            model.WriteModel(@"c:\temp\MPS\" + $"{dataset}_3idx.mps");
            /*
            model.Optimize();
            model.WriteModel(@"c:\temp\solution.sol");
            var sol = new Solution(data,formulation);
            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());
            Assert.IsTrue(sol.IsFeasible,"solution should be feasible");
            Assert.AreEqual(5,sol.Objective);
            */
        }        [Test]
        [TestCase(ITC_Comp01)]
        [TestCase(ITC_Comp02)]
        [TestCase(ITC_Comp03)]
        [TestCase(ITC_Comp04)]
        [TestCase(ITC_Comp05)]
        [TestCase(ITC_Comp06)]
        [TestCase(ITC_Comp07)]
        [TestCase(ITC_Comp08)]
        [TestCase(ITC_Comp09)]
        [TestCase(ITC_Comp10)]
        [TestCase(ITC_Comp11)]
        [TestCase(ITC_Comp12)]
        [TestCase(ITC_Comp13)]
        [TestCase(ITC_Comp14)]
        [TestCase(ITC_Comp15)]
        [TestCase(ITC_Comp16)]
        [TestCase(ITC_Comp17)]
        [TestCase(ITC_Comp18)]
        [TestCase(ITC_Comp19)]
        [TestCase(ITC_Comp20)]
        [TestCase(ITC_Comp21)]
        public void WriteMPSForTwoIndex(string filename)
        {
            var data = Data.ReadXml(dataPath +  filename, "120","4");
            var dataset = System.IO.Path.GetFileNameWithoutExtension(filename);
            var formulation = ProblemFormulation.UD2;
       

            var model = new CCTModel(data,formulation,new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
            });

            model.WriteModel(@"c:\temp\MPS\" + $"{dataset}_2idx.mps");
            /*
            model.Optimize();
            model.WriteModel(@"c:\temp\solution.sol");
            var sol = new Solution(data,formulation);
            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());
            Assert.IsTrue(sol.IsFeasible,"solution should be feasible");
            Assert.AreEqual(5,sol.Objective);
            */
        }
        
    }
}