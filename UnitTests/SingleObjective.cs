using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UniversityTimetabling;
using UniversityTimetabling.MIPModels;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class SingleObjective : UnitTestBase
    {

        //private int TimelimitStageI = 210;
        //private int TimelimitStageII = 30;
        private int TimelimitStageI = 155;
        private int TimelimitStageII = 25;
        private ProblemFormulation formulation = ProblemFormulation.UD2NoOverbook;
        private int ntests = 5;

        //https://www.random.org/integer-sets/?sets=1&num=10&min=1&max=999&commas=on&order=index&format=html&rnd=new
        readonly int[] seeds = { 63, 983, 91, 704, 28, 214, 480, 359, 235, 601 };

        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void TestSolveStageI(string filename)
        {
            var algname = nameof(TestSolveStageI);
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            for (int i = 0; i < ntests; i++)
            {
                //var formulation = ProblemFormulation.UD2NoOverbook;
                //Formulation.AvailabilityHardConstraint = false;
                //Formulation.OverbookingAllowed = 0.35;
                var ModelParameters = new CCTModel.MIPModelParameters()
                {
                    UseStageIandII = false,
                    TuneGurobi = true,
                    UseHallsConditions = true,
                    UseRoomHallConditions = true,
                    Seed = seeds[i],
                };
                var model = new CCTModel(data, formulation, ModelParameters);

                model.Optimize(TimelimitStageI);
                var objective = (int) Math.Round(model.Objective);

                File.AppendAllText(@"c:\temp\heuristic.txt",
          $"{algname};{filename};{DateTime.Now.ToString()};{i};{objective}\n");

            }
        }

        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void TestSolveStageIandII(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            //var formulation = ProblemFormulation.UD2NoOverbook;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,

            };
            var model = new CCTModel(data, formulation, ModelParameters);

            model.Optimize(TimelimitStageI);

            //todo make test. use fixing as in multi

            File.AppendAllText(@"c:\temp\heuristic.txt", $"{nameof(TestSolveStageI)};{filename};{model.Objective}\n");
        }

        [TestCase(true,20)]
        [TestCase(true,25)]
        [TestCase(true,30)]
        [TestCase(false,12)]
        public void TestLocalBranchingConstraint(bool usecurr,int N)
        {

            var data = Data.ReadXml(dataPath + ITC_Comp05, "120", "4");
            //var formulation = ProblemFormulation.UD2;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var modelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false
            };
            var model = new CCTModel(data, formulation, modelParameters);
            model.Optimize(30*3, 0.99);
            model.SetProxConstraint(N); 
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < TimelimitStageI)
            {
                model.SetProximityOrigin(usecurr);
                model.Optimize(15);
            }
        }
    

        [Test]
        public void TestProximityConstraint()
        {
            
        //    var data = Data.ReadXml(dataPath + Erlangen2012_1, "120", "4");
            var data = Data.ReadXml(dataPath + ITC_Comp05, "120", "4");
           // var formulation = ProblemFormulation.UD2;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                ConstraintPenalty = 50,
                AbortSolverOnZeroPenalty = true,
            };
            var model = new CCTModel(data, formulation, ModelParameters);
            var watch = Stopwatch.StartNew();
            model.Optimize(30, 0.99);
            var obj = model.ObjSoftCons;
            model.SetObjective(0, 0, 1);

            while (watch.Elapsed.TotalSeconds < TimelimitStageI)
            {
                model.SetProximityOrigin(true);
                model.SetQualConstraint(obj - 10);
                model.Optimize(15);
                obj = model.ObjSoftCons;
                Console.WriteLine($"Objective: {obj}");

            }
        }
        [Test]
        public void TestCurriculaConstraint()
        {
            var data = Data.ReadXml(dataPath + ITC_Comp05, "120", "4");
           // var formulation = ProblemFormulation.UD2;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                ConstraintPenalty = 50,
                AbortSolverOnZeroPenalty = true,
            };
            var model = new CCTModel(data, formulation, ModelParameters);
            var watch = Stopwatch.StartNew();
            model.Optimize(30, 0.99);
            var obj = model.ObjSoftCons;

            while (watch.Elapsed.TotalSeconds < TimelimitStageI*2)
            {
                model.SetQualConstraint(obj - 5);
                model.Optimize(120);
                obj = model.ObjSoftCons;
                Console.WriteLine($"Objective: {obj}");

            }
        }



     //   [TestCaseSource(nameof(TestDatasetsITC2007))]
        [TestCase(Erlangen2012_1,5)]
        [TestCase(ITC_Comp09,1)]
    [TestCase(ITC_Comp09, 2)]
    [TestCase(ITC_Comp09, 3)]
    [TestCase(ITC_Comp09, 4)]
    [TestCase(ITC_Comp09, 5)]
    [TestCase(ITC_Comp09, 6)]
    [TestCase(ITC_Comp09, 7)]
    [TestCase(ITC_Comp09, 8)]
    [TestCase(ITC_Comp09, 9)]
    [TestCase(ITC_Comp09, 10)]
    [TestCase(ITC_Comp09, 11)]
    [TestCase(ITC_Comp09, 12)]
    [TestCase(ITC_Comp09, 13)]
    [TestCase(ITC_Comp05, 13)]
   //   [TestCase(ITC_Comp05,1)]
      /*
      [TestCase(ITC_Comp05, 2)]
      [TestCase(ITC_Comp05, 3)]
      [TestCase(ITC_Comp05, 4)]
      [TestCase(ITC_Comp05, 5)]
      [TestCase(ITC_Comp05, 6)]
      [TestCase(ITC_Comp05, 7)]
      [TestCase(ITC_Comp05, 8)]
      [TestCase(ITC_Comp05, 11)]
      [TestCase(ITC_Comp05, 12)]
      [TestCase(ITC_Comp05, 13)]
      [TestCase(ITC_Comp05, 14)]
      [TestCase(ITC_Comp05, 15)]
      [TestCase(ITC_Comp05, 16)]
      [TestCase(ITC_Comp05, 17)]
      */
        public void CCfirstHeuristic30s(string filename, int seed) // 
        {
          //  var seed = 0;
            for (int ia = 1; ia < 5; ia++)
            {
                seed = ia;
            
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            // var newrooms = CreateRooms(data);
            // data.SetRoomList(newrooms);

            var problemFormulation = ProblemFormulation.UD2NoOverbook;
            problemFormulation.MinimumWorkingDaysWeight = 0;
            //    problemFormulation.CurriculumCompactnessWeight = 0;
            var par = new CCTModel.MIPModelParameters()
            {
                TuneGurobi = true,
                UseStageIandII = false,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                UseRoomsAsTypes = false,
                Seed = seed,
            };
            var model = new CCTModel(data, problemFormulation, par);
            model.SetObjective(1, 0, 0);
            model.Optimize(TimelimitStageI);
            model.DisplayObjectives();
            var result = new Dictionary<double, double>();
            //maybe find better proximity that reflects currciulumcompactness better.
            //Check which curriculum that can be moved without introducting penalties.
            model.SetProximityOrigin(true);
            problemFormulation.MinimumWorkingDaysWeight = 5;
            for (var i = 15; i <= 15; i++)
            {
//                model.SetObjective(0, 0, i);
                model.SetProxConstraint(i);
                model.AddMinimumWorkingDaysCost();
                // model.FixCurricula(true);
                model.Optimize();
                model.DisplayObjectives();
                model.SetObjective(1, 0, 0);
                model.Fixsol(true);
                model.Optimize();
                model.Fixsol(false);
                model.DisplayObjectives();
                result[i] = model.Objective;
            }

            Console.WriteLine("Results:");
            Console.WriteLine("i;obj");
            foreach (var r in result)
            {
                Console.WriteLine($"{r.Key};{r.Value}");
            }

            var best = result.Min(r => r.Value);

            File.AppendAllText(@"c:\temp\heuristic.txt", $"{nameof(CCfirstHeuristic30s)};{filename};{best}\n");
                //  model.FixCurricula(false);

                // maybe also save multiple minimum curriculum solutions
            }

        }
    //    [TestCase(Erlangen2012_1)]
        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void TestSolveStageIRoomRelaxedAndFixed(string filename)
        {
            for (int i = 0; i < ntests; i++)
            {
                var algname = nameof(TestSolveStageIRoomRelaxedAndFixed) + "roomrelaxontimeslot";
                //  formulation.AvailabilityHardConstraint = false;
                var data = Data.ReadXml(dataPath + filename, "120", "4");

                //var formulation = ProblemFormulation.UD2NoOverbook;
                // formulation.AvailabilityHardConstraint = false;
                //   formulation.OverbookingAllowed = 0.35;
                var ModelParameters = new CCTModel.MIPModelParameters()
                {
                    UseStageIandII = false,
                    TuneGurobi = true,
                    UseHallsConditions = true,
                    UseRoomHallConditions = true,
                    // UseRoomsAsTypes = true,
                    UseRoomsAsTypes = false,
                    RoomBoundForEachTimeSlot = true,
                    Seed = seeds[i],

                };
                var heuristics = new Heuristics(TimelimitStageI, data, formulation, ModelParameters);
                var sol = heuristics.Run();
                Console.WriteLine(sol.AnalyzeSolution());

                //sol = heuristics.SolveStageII();
                //Console.WriteLine(sol.AnalyzeSolution());

                //var model = RoomRelaxAndTight(data, ModelParameters);


                var objective = (int) Math.Round(sol.Objective);
                File.AppendAllText(@"c:\temp\heuristic.txt",
                    $"{algname};{filename};{DateTime.Now.ToString()};{i};{objective}\n");
            }
        }
        

        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void PenaltiesScaledByCourseSizes(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            //var formulation = ProblemFormulation.UD2NoOverbook;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,
                ScalePenaltyWithCourseSize = true,

            };
            var model = new CCTModel(data, formulation, ModelParameters);

            model.Optimize(TimelimitStageI*2);
            File.AppendAllText(@"c:\temp\heuristic.txt", $"{nameof(PenaltiesScaledByCourseSizes)};{filename};{model.Objective}\n");
        }

        /*

        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void TestForceCurDownHeur(string filename)
        {
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            //var formulation = ProblemFormulation.UD2NoOverbook;
            //Formulation.AvailabilityHardConstraint = false;
            //Formulation.OverbookingAllowed = 0.35;
            var ModelParameters = new CCTModel.MIPModelParameters()
            {
                UseStageIandII = false,
                TuneGurobi = true,
                UseHallsConditions = true,
                UseRoomHallConditions = true,

            };
            var model = new CCTModel(data, formulation, ModelParameters);

            model.Optimize(TimelimitStageI);


            while (watch.Elapsed.TotalSeconds < TimelimitStageI)
            {
                model.SetProximityOrigin(true);
                model.SetQualConstraint(obj - 10);
                model.Optimize(15);
                obj = model.ObjSoftCons;
                Console.WriteLine($"Objective: {obj}");

            }
            File.AppendAllText(@"c:\temp\heuristic.txt", $"{nameof(TestForceCurDownHeur)};{filename};{model.Objective}\n");
        }
        */
    }
}
