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
    class HeuristicTests : UnitTestBase
    {

        //private int TimelimitStageI = 210;
        //private int TimelimitStageII = 30;
        private int TimelimitStageI = 155;
        private int TimelimitStageII = 25;
        private ProblemFormulation formulation = ProblemFormulation.UD2NoOverbook;
        private int ntests = 10;

        //https://www.random.org/integer-sets/?sets=1&num=10&min=1&max=999&commas=on&order=index&format=html&rnd=new
        readonly int[] seeds = { 63, 983, 91, 704, 28, 214, 480, 359, 235, 601 };

        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void GurobiStageI(string filename)
        {
            var algname = nameof(GurobiStageI);
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

                var stopwatch = Stopwatch.StartNew();

                model.Optimize(TimelimitStageI);
                stopwatch.Stop();
                var objective = (int) Math.Round(model.Objective);
                var dataname = System.IO.Path.GetFileNameWithoutExtension(filename);

                File.AppendAllText(@"c:\temp\heuristic.txt",
          $"{algname};{dataname};{DateTime.Now};{i};{objective};{(int) stopwatch.Elapsed.TotalSeconds};\n");

            }
        }
                [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void Relaxunavailability(string filename)
        {
            var algname = nameof(Relaxunavailability);
            var data = Data.ReadXml(dataPath + filename, "120", "4");
            for (int i = 0; i < 1; i++)
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
                formulation.AvailabilityHardConstraint = false;
                var model = new CCTModel(data, formulation, ModelParameters);

                var stopwatch = Stopwatch.StartNew();

                model.Optimize(30);
                stopwatch.Stop();
                var objective = (int) Math.Round(model.Objective);
                var dataname = System.IO.Path.GetFileNameWithoutExtension(filename);

                File.AppendAllText(@"c:\temp\heuristic.txt",
          $"{algname};{dataname};{DateTime.Now};{i};{objective};{(int) stopwatch.Elapsed.TotalSeconds};\n");

            }
        }
        
  
        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void RRHeurStageI(string filename)
        {
            for (int i = 0; i < ntests; i++)
            {
                var algname = nameof(RRHeurStageI) + "20extra";
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
                var heuristics = new Heuristics(TimelimitStageI+20, data, formulation, ModelParameters);
                var sol = heuristics.Run();
                Console.WriteLine(sol.AnalyzeSolution());
                var objective = (int) Math.Round(sol.Objective);

                var dataname = System.IO.Path.GetFileNameWithoutExtension(filename);

                File.AppendAllText(@"c:\temp\heuristic.txt",
                    $"{algname};{dataname};{DateTime.Now};{i};{objective};{heuristics.totalseconds};{heuristics.roomtightseconds}\n");
            }
        }
        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void UnavailHeurStageI(string filename)
        {
            for (int i = 0; i < 1; i++)
            {
                var algname = nameof(UnavailHeurStageI);
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
                  //  RoomBoundForEachTimeSlot = true,
                    Seed = seeds[i],

                };
                formulation.AvailabilityHardConstraint = false;

                var heuristics = new Heuristics(TimelimitStageI, data, formulation, ModelParameters);
                var sol = heuristics.RunUnavailbility();
                Console.WriteLine(sol.AnalyzeSolution());
                var objective = (int) Math.Round(sol.Objective);

                var dataname = System.IO.Path.GetFileNameWithoutExtension(filename);

                File.AppendAllText(@"c:\temp\heuristic.txt",
                    $"{algname};{dataname};{DateTime.Now};{i};{objective};{heuristics.totalseconds};{heuristics.roomtightseconds}\n");
            }
        }
        [TestCaseSource(nameof(TestDatasetsITC2007))]
        public void LargeCoursesFirstHeuristic(string filename)
        {
            for (int i = 0; i < 1; i++)
            {
                var algname = nameof(UnavailHeurStageI);
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
                  //  RoomBoundForEachTimeSlot = true,
                    Seed = seeds[i],

                };
               
                var heuristics = new Heuristics(TimelimitStageI, data, formulation, ModelParameters);
                var sol = heuristics.RunLargeCoursesFirst();
                Console.WriteLine(sol.AnalyzeSolution());
                var objective = (int) Math.Round(sol.Objective);

                var dataname = System.IO.Path.GetFileNameWithoutExtension(filename);

                File.AppendAllText(@"c:\temp\heuristic.txt",
                    $"{algname};{dataname};{DateTime.Now};{i};{objective};{heuristics.totalseconds};{heuristics.roomtightseconds}\n");
            }
        }
     
    }
}
