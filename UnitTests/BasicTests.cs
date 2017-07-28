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
    public class BasicTests : UnitTestBase
    {

        [Test]
        public void TestDataread()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp01, "120","4");
            Assert.AreEqual(30,data.Courses.Count);
            Assert.AreEqual(6,data.Rooms.Count);
            Assert.AreEqual(6, data.Periods);
            Assert.AreEqual(5, data.Days);
            Assert.AreEqual(6*5, data.TimeSlots.Count);
            Assert.AreEqual(14, data.Curricula.Count);
        }
        [Test]
        public void TestSolutionread()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp01, "120","4");
            var formulation = ProblemFormulation.UD2;
            var sol = new Solution(data,formulation);
            sol.Read(dataPath + @"ITC2007\sol\comp01-UD2.sol");
            sol.AnalyzeSolution();
            Assert.AreEqual(0,sol.UnscheduledLectures);
            var model = new CCTModel(data, ProblemFormulation.UD2);
            model.SetAndFixSolution(sol._assignments.ToList());
            model.Optimize();
            Assert.AreEqual(5,model.Objective);
        }

        

        [Test]
        public void TestSolveComp01ToOptimalityUD2()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp01, "120","4");
            var formulation = ProblemFormulation.UD2;



            var model = new CCTModel(data,formulation);
            Console.WriteLine("Solving");
            model.WriteModel(@"c:\temp\model.lp");

            model.Optimize();
            model.WriteModel(@"c:\temp\solution.sol");
            var sol = new Solution(data,formulation);
            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());
            Assert.IsTrue(sol.IsFeasible,"solution should be feasible");
            Assert.AreEqual(5,sol.Objective);
        }

        [Test]
        public void TestSolveComp05ToOptimalityUD2()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp05, "120","4");
            var formulation = ProblemFormulation.UD2;


            var sol = new Solution(data, formulation);
            sol.Read(dataPath + @"ITC2007\comp05-UD2.sol");

            var model = new CCTModel(data, formulation);
            model.ModelParameters.SaveBoundInformation = true;
            model.MipStart(sol._assignments.ToList());

            //model.WriteModel(@"c:\temp\model.lp");
            Console.WriteLine("Solving");
            model.Optimize(300);

            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());
            Assert.IsTrue(sol.IsFeasible,"solution should be feasible");
          //  Assert.AreEqual(0,sol.Objective);

            Console.WriteLine("MWD;CC;obj;bnd");
            Console.WriteLine(String.Join("\n", model.bounds.Select(t => $"{t.Item1};{t.Item2};{t.Item3};{t.Item4}")));
        }

        [Test]
        public void TestSolve()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp04, "120","4");
            var formulation = ProblemFormulation.UD2;
            formulation.StudentMinMaxLoadWeight = 1;
            formulation.RoomStabilityWeight = 0;
            formulation.OverbookingAllowed = 0.5;
            var model = new CCTModel(data,formulation);
            //model.WriteModel(@"c:\temp\model.lp");
            Console.WriteLine("Solving");
            model.Optimize(300,0.40);
           // model.DisplaySoftConstraintViolations();
            var sol = new Solution(data,formulation);
            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());
            sol.Save(@"c:\temp\solution.ctt");
        }



        [Test]
        public void TestRoomUnsuitabilityConstraint()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp05, "120", "4");
            //var formulation = ProblemFormulation.UD2;
            var formulation = ProblemFormulation.UD2;
            formulation.UnsuitableRoomsWeight = 1;
            formulation.BadTimeslotsWeight = 1;
          //  formulation.StudentMinMaxLoadWeight = 1;
          //  formulation.OverbookingAllowed = 0.15;

            var model = new CCTModel(data, formulation);
            Console.WriteLine("Solving");
            model.WriteModel(@"c:\temp\model.lp");

            model.Optimize(900);
            model.WriteModel(@"c:\temp\solution.sol");
            var sol = new Solution(data, formulation);
      
            sol.SetAssignments(model.GetAssignments());
            Console.WriteLine(sol.AnalyzeSolution());
            Assert.IsTrue(sol.IsFeasible, "solution should be feasible");
           // Assert.AreEqual(5, sol.Objective);
        }
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
        public void TestStageI(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120","4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var mippar = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                TuneGurobi = true,
                SaveBoundInformation = true,
            };
            var model = new CCTModel(data,formulation,mippar);
            //model.WriteModel(@"c:\temp\model.lp");
            Console.WriteLine("Solving");
            model.Optimize(20);
            model.DisplayObjectives();
            var sol = new Solution(data, formulation);
            sol.SetAssignments(model.GetAssignments());
            // model.DisplaySoftConstraintViolations();
            //var sol = new Solution(data,Formulation);
            //sol.SetAssignments(model.GetAssignments());
            //Console.WriteLine(sol.AnalyzeSolution());
            //sol.Save(@"c:\temp\solution.ctt");
            //            File.AppendAllText(@"c:\temp\results.txt", $"30s;{filename};{model.Objective}\n");

            Console.WriteLine("MWD;CC;obj;bnd");
            Console.WriteLine(String.Join("\n",model.bounds.Select(t => $"{t.Item1};{t.Item2};{t.Item3};{t.Item4}")));
            
            Console.WriteLine(sol.AnalyzeSolution());
        }

        [Test]
        [TestCase(ITC_Comp05,1000,11)]
        [TestCase(ITC_Comp05,1000,21)]
        [TestCase(ITC_Comp06,1300,11)]
        [TestCase(ITC_Comp06,1300,21)]
        [TestCase(ITC_Comp08,1000,11)]
        [TestCase(ITC_Comp08,1000,21)]
        [TestCase(ITC_Comp12,500,11)]
        [TestCase(ITC_Comp12,500,21)]
        [TestCase(ITC_Comp18,400,11)]
        [TestCase(ITC_Comp18,400,21)]
        public void TestBudgetConstraint(string filename, int budget,int seed)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;
            var newrooms = CreateRoomsFixedSize(data, 25, 1);
            data.SetRoomList(newrooms);


            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
                TuneGurobi = true,
                Seed = seed,
            });
            //model.WriteModel(@"c:\temp\model.lp");
            model.SetBudgetConstraint(budget);
            model.Optimize(900);
            Console.WriteLine($"Room Cost: {model.GetUsedRooms().Sum(kv => kv.Key.Cost * kv.Value)}");
            File.AppendAllText(@"c:\temp\enumtest.txt", $"{nameof(TestBudgetConstraint)};{filename};{seed};{model.Objective}\n");

        }
        [Test]
        [TestCase(ITC_Comp05, 1000, 11)]
        [TestCase(ITC_Comp05, 1000, 21)]
        [TestCase(ITC_Comp06, 1300, 11)]
        [TestCase(ITC_Comp06, 1300, 21)]
        [TestCase(ITC_Comp08, 1000, 11)]
        [TestCase(ITC_Comp08, 1000, 21)]
        [TestCase(ITC_Comp12, 500, 11)]
        [TestCase(ITC_Comp12, 500, 21)]
        [TestCase(ITC_Comp18, 400, 11)]
        [TestCase(ITC_Comp18, 400, 21)]
        public void TestBudgetConstraintWithEnumeration(string filename, int budget,int seed)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            var formulation = ProblemFormulation.UD2NoOverbook;

            var comb = new RoomCombinations(data, 25);
            Console.WriteLine($"Minimum room is: {comb._minCost}");
            var newrooms = CreateRoomsFixedSize(data, 25,1);
            data.SetRoomList(newrooms);


            var model = new CCTModel(data, formulation, new CCTModel.MIPModelParameters()
            {
                UseRoomsAsTypes = true,
                UseStageIandII = false,
                TuneGurobi = true,
                Seed = seed,
            }); 
            //model.WriteModel(@"c:\temp\model.lp");
            //var budget = comb._minCost+0;
          

            Console.WriteLine($"Budget: {budget}");

            model.SetPossibleRoomCombinations(budget);
            model.Optimize(900);


            var sol = new Solution(data, formulation);
          //  sol.SetAssignments(model.GetAssignments());
          //  Console.WriteLine(sol.AnalyzeSolution());

            Console.WriteLine($"Room Cost: {model.GetUsedRooms().Sum(kv => kv.Key.Cost*kv.Value)}");
            File.AppendAllText(@"c:\temp\enumtest.txt", $"{nameof(TestBudgetConstraintWithEnumeration)};{filename};{seed};{model.Objective}\n");

        }


        [Test]
        public void TestQualConstraint()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp08, "120", "4");
            var formulation = ProblemFormulation.UD2;
            var model = new CCTModel(data, formulation);
            //model.WriteModel(@"c:\temp\model.lp");
            model.SetQualConstraint(5);
            model.Optimize();

        }

        [Test]
        public void TestObjective()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp01, "120", "4");
            var formulation = ProblemFormulation.UD2;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var model = new CCTModel(data, formulation);
            model.Optimize(900);
            model.SetObjective(0.95,0.5);
            model.Optimize(60);
            model.SetObjective(0.5,0.5);
            model.Optimize(60);
            model.SetObjective(0.2,0.8);
        }

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
        public void TestCombinations(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
             
            var comb = new RoomCombinations(data, 25);

            for (var i = 0; i < comb._minCost; i+=25)
            {
                var combinations = comb.GetCombinations(comb._minCost + i);
                var rooms = CreateRoomsFromIntList(data, combinations);

                if (combinations.Count == 0) continue;
                Console.WriteLine($"{i}: {combinations.Count}");
                Console.WriteLine(rooms.Count);
                Console.WriteLine(string.Join(",", combinations.First().Keys));
                foreach (var combination in combinations)
                {
                 Console.WriteLine(string.Join(",",combination.Values));   
                }
                
                if (combinations.Count > 1000) break;
            }
        }
    }
}